using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using AngleSharp.Text;
using GlobalCMSUmbraco.ProjectsSection.Extensions;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using uSync.Expansions.Core;
using uSync.Expansions.Core.Physical;
using uSync.Expansions.Core.Services;
using uSync.Exporter;
using uSync.Exporter.Services;
using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using File = System.IO.File;
using ZipFile = System.IO.Compression.ZipFile;

namespace GlobalCMSUmbraco.ProjectsSection.Services
{
    public class CloneSyncPackService : ICloneSyncPackService
    {
        private readonly SyncExporterService exporterService;
        private readonly SyncFileService syncFileService;
        private readonly SyncHandlerService syncHandlerService;
        private readonly SyncHandlerFactory syncHandlerFactory;
        private readonly SyncPackService syncPackService;
        private readonly SyncPackFileService syncPackFileService;
        private readonly AppCaches caches;
        private readonly ILogger logger;

        private readonly string syncPackSourceFolderPath;
        private readonly uSyncConfig uSyncConfig;
        private readonly ISyncItemFactory syncItemFactory;

        public CloneSyncPackService(
            SyncExporterService exporterService,
            SyncPackService syncPackService,
            SyncPackFileService syncPackFileService,
            SyncFileService syncFileService,
            SyncHandlerService syncHandlerService, // check the composer these are customised
            SyncHandlerFactory syncHandlerFactory, // check the composer these are customised
            ISyncItemFactory syncItemFactory,
            uSyncConfig uSyncConfig,
            AppCaches caches, 
            ILogger logger)
        {
            this.exporterService = exporterService;
            this.syncFileService = syncFileService;
            this.syncHandlerService = syncHandlerService;
            this.syncHandlerFactory = syncHandlerFactory;
            this.syncItemFactory = syncItemFactory;
            this.caches = caches;
            this.syncPackService = syncPackService;
            this.syncPackFileService = syncPackFileService;
            this.uSyncConfig = uSyncConfig;
            this.logger = logger;

            syncPackSourceFolderPath = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/"), ProjectConstants.AppDataStarterKitsFolder);
        }

        public List<ExporterStep> CreateCloneSteps(ExporterRequest exporterRequest, string projectCode = null)
        {
            if (exporterRequest.Id == Guid.Empty)
                exporterRequest.Id = Guid.NewGuid();

            return GetSyncPackCustomizationSteps(exporterRequest, projectCode);
        }

        public ExporterResponse Process(ExporterRequest exporterRequest)
        {
            var steps = GetSyncPackCustomizationSteps(exporterRequest);
            return exporterRequest.ProcessSteps(steps, uSyncConfig, syncHandlerService);
        }

        public bool CloneAndProcess(string projectCode, string selectedSyncPackFile)
        {
            var exporterRequest = new ExporterRequest
            {
                Id = Guid.NewGuid(),
                StepIndex = 0,
                Name = selectedSyncPackFile,
                Request = new SyncPackRequest
                {
                    PageNumber = 0,
                    PageSize = 0
                }
            };


            // start with cloning
            var steps = GetSyncPackCustomizationSteps(exporterRequest, projectCode);
            logger.Info<CloneSyncPackService>("Start cloning project {code} from {selectedSyncPackFile}", projectCode, selectedSyncPackFile);

            var completed = false;
            var progressCount = 0;
            while (!completed)
            {
                progressCount++;
                logger.Info<CloneSyncPackService>("Cloning project {code} progress count {count}", projectCode, progressCount);
                var exporterResponse = exporterRequest.ProcessSteps(steps, uSyncConfig, syncHandlerService);
                exporterRequest.StepIndex = exporterResponse.StepIndex;
                exporterRequest.Request.PageNumber = exporterResponse.NextPage;
                exporterRequest.Request.HandlerFolder = exporterResponse.NextFolder;
                completed = exporterResponse.ExportComplete;
            }
            logger.Info<CloneSyncPackService>("Cloning project {code} completed", projectCode);

            // now do import
            steps = GetImportSyncPackSteps();
            logger.Info<CloneSyncPackService>("Start importing project {code}", projectCode);

            exporterRequest.StepIndex = 0;
            completed = false;
            progressCount = 0;
            while (!completed)
            {
                progressCount++;
                logger.Info<CloneSyncPackService>("Importing project {code} progress count {count}", projectCode, progressCount);
                var exporterResponse = exporterRequest.ProcessSteps(steps, uSyncConfig, syncHandlerService);
                exporterRequest.StepIndex = exporterResponse.StepIndex;
                exporterRequest.Request.PageNumber = exporterResponse.NextPage;
                exporterRequest.Request.HandlerFolder = exporterResponse.NextFolder;
                completed = exporterResponse.ExportComplete;
            }
            logger.Info<CloneSyncPackService>("Importing project {code} completed", projectCode);

            return true;
        }

        private List<ExporterStep> GetSyncPackCustomizationSteps(ExporterRequest exporterRequest, string projectCode = null)
        {
            exporterRequest.Request.HandlerSet = "starterkit";

            var cacheKey = GetCacheKey(exporterRequest);
            return caches.RuntimeCache.GetCacheItem(cacheKey, () => new List<ExporterStep>
            {
                new ExporterStep("Clone", "icon-split-alt", (guid, request) => CloneStep(guid, request, exporterRequest, projectCode)),
                new ExporterStep("Customise", "icon-fingerprint", (guid, request) => ProcessItems(guid, request, projectCode)),
                new ExporterStep("Prepare", "icon-shuffle", PrepareStep),
                new ExporterStep("Clean", "icon-brush-alt-2", CleanReport)
            });
        }

        private List<ExporterStep> GetImportSyncPackSteps()
        {
            return new List<ExporterStep>()
            {
                new ExporterStep("Validate", "icon-plugin", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.ValidatePack)),
                new ExporterStep("Files", "icon-script-alt", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.ImportFiles)),
                new ExporterStep("Media", "icon-pictures-alt-2", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.ImportMedia)),
                new ExporterStep("Import", "icon-box", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.Import)),
                new ExporterStep("Second Pass", "icon-box", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.ImportSecondPass)),
                new ExporterStep("Finalize", "icon-box", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.ImportFinalize)),
                new ExporterStep("Report", "icon-slideshow", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.ImportResults)),
                new ExporterStep("Clean", "icon-brush-alt-2", new Func<Guid, SyncPackRequest, SyncPackResponse>(this.exporterService.Cleaning))
            };
        }

        private SyncPackResponse ImportStep(Guid guid, SyncPackRequest syncPackRequest)
        {
            syncPackRequest.Folder = exporterService.CreateImportFolder(guid);
            var syncPackImporter = Current.Factory.GetInstance<SyncPackService>();

            var response = syncPackImporter.ImportItems(syncPackRequest);
            return response;
        }

        private SyncPackResponse CleanReport(Guid guid, SyncPackRequest request)
        {
            return SyncPackResponseHelper.Succeed(true);
        }

        private SyncPackResponse PrepareStep(Guid guid, SyncPackRequest request)
        {
            var report = exporterService.GetReport(guid, request);
            var uSyncActionList = report.Actions.ToDictionary(action => action.FileName);

            var keyChanges = GetChanges(uSyncActionList.Values, "Key").ToList();
            var aliasChanges = GetChanges(uSyncActionList.Values, "Alias").ToList();

            var syncHandlerOptions = new SyncHandlerOptions(request.HandlerSet);

            var fileNames = syncFileService.GetFiles(request.Folder + "\\uSync", "*.config", true);
            foreach (var fileName in fileNames)
            {
                var node = XElement.Load(fileName);
                var contents = node.ToString();
                var itemType = node.GetItemType();

                var handlerConfigPair = syncHandlerFactory.GetValidHandlerByTypeName(itemType, syncHandlerOptions);
                if (handlerConfigPair == null) 
                    continue;

                contents = ReplaceGuids(contents, keyChanges); 
                contents = ReplaceAliases(contents, aliasChanges);
                
                var currentNode = XElement.Parse(contents);
                var changes = handlerConfigPair.GetChanges(request.GetImportOptions(1, 100), node, currentNode, syncItemFactory).ToList();
                if (!changes.Any(c => c.Change > ChangeDetailType.NoChange)) 
                    continue;

                syncFileService.SaveXElement(currentNode, fileName);

                var uSyncAction = uSyncActionList[fileName];
                var details = uSyncActionList[fileName].Details.ToList();
                details.AddRange(changes);
                uSyncAction.Details = details;

                uSyncActionList[fileName] = uSyncAction;
            }

            report.Actions = syncPackService.SaveActions(request.Folder, uSyncActionList.Values);
            var actionsFileName = Path.Combine(request.Folder, "_actions.config");
            File.Move(actionsFileName, $"{actionsFileName}.bak");

            syncPackService.CleanReportedFolder(request.Folder);

            return report;
        }

        private static string ReplaceAliases(string contents, IEnumerable<(uSyncAction, uSyncChange)> changes)
        {
            string GetValue(string value, string closingElement) => $">{value}</{closingElement}>";

            var itemTypes = new [] {"ContentType", "MediaType", "MemberType"};

            foreach (var (action, change) in changes)
            {
                var itemType = XElement.Load(action.FileName).GetItemType();
                if (!itemTypes.Contains(itemType)) 
                    continue;

                // replace alias in structure nodes
                contents = contents.Replace(GetValue(change.OldValue, itemType), GetValue(change.NewValue, itemType));

                // replace alias in compositions
                contents = contents.Replace(GetValue(change.OldValue, "Composition"), GetValue(change.NewValue, "Composition"));
            }

            return contents;
        }

        private static string ReplaceGuids(string contents, IEnumerable<(uSyncAction, uSyncChange)> changes)
        {
            foreach (var (_, change) in changes)
            {
                contents = contents
                    .Replace(change.OldValue, change.NewValue)
                    .Replace(change.OldValue.Replace("-", ""), change.NewValue.Replace("-", ""));
            }

            return contents;
        }

        private static IEnumerable<(uSyncAction, uSyncChange)> GetChanges(IEnumerable<uSyncAction> actions, params string[] names)
        {
            return from action in actions from detail in action.Details where names.Contains(detail.Name) select (action, detail);
        }

        private SyncPackResponse CloneStep(Guid guid, SyncPackRequest request, ExporterRequest exporterRequest, string projectCode)
        {
            try
            {
                request.Folder = exporterService.CreateImportFolder(guid);
                var sourceSyncPackFullPath = GetSyncPackFilePath(exporterRequest.Name);
                var copyResult = CopySyncPack(sourceSyncPackFullPath, request.Folder);

                var success = copyResult.Success ? ExtractSyncPack(guid) : copyResult;
                RenameMediaFolders(request.Folder, projectCode);
                return success;
            }
            catch (Exception ex)
            {
                return SyncPackResponseHelper.Fail(ex.Message);
            }
        }

        private void RenameMediaFolders(string folder, string projectCode)
        {
            var mediaPath = Path.Combine(folder, "media");
            if (!Directory.Exists(mediaPath)) return;

            var directoryInfo = new DirectoryInfo(mediaPath);
            var directories = directoryInfo.GetDirectories();
            foreach (var directory in directories)
            {
                directory.MoveTo(directory.FullName + projectCode.ToLower());
            }
        }

        private SyncPackResponse ProcessItems(Guid guid, SyncPackRequest syncPackRequest, string projectCode)
        {
            syncPackRequest.Folder = exporterService.CreateImportFolder(guid);

            // cache the project code
            caches.RequestCache.GetCacheItem($"{guid}_ProjectCode", () => projectCode);

            // get the changes
            var response = syncPackService.ReportChanges(syncPackRequest);

            return response;
        }

        private string GetCacheKey(ExporterRequest exporterRequest)
        {
            return $"{GetType().Name}_{exporterRequest.clientId}_{exporterRequest.Id}";
        }

        private static SyncPackResponse CopySyncPack(string sourceSyncPackFilePath, string targetFolder)
        {
            if (sourceSyncPackFilePath == null || !File.Exists(sourceSyncPackFilePath))
            {
                return SyncPackResponseHelper.Fail($"{sourceSyncPackFilePath} not found");
            }

            try
            {
                var fileName = Path.GetFileName(sourceSyncPackFilePath);
                var targetFilePath = Path.Combine(targetFolder, fileName);

                File.Copy(sourceSyncPackFilePath, targetFilePath);
            }
            catch (Exception ex)
            {
                return SyncPackResponseHelper.Fail($"Unable to copy syncpack: {ex.Message}");
            }

            return SyncPackResponseHelper.Succeed();
        }

        private SyncPackResponse ExtractSyncPack(Guid guid)
        {
            try
            {
                // workaround for https://github.com/Jumoo/uSync.Complete.Issues/issues/117
                var sourceDirectory = exporterService.CreateImportFolder(guid);
                var files = syncFileService.GetFiles(sourceDirectory, "*.usync");

                foreach (var zipfile in files)
                {
                    using (var zipArchive = ZipFile.OpenRead(zipfile))
                    {
                        zipArchive.ExtractToDirectory(sourceDirectory);
                    }

                    File.Delete(zipfile);
                }

                // TODO: revert above workaround
                //exporterService.UnpackExport(guid);
            }
            catch (Exception ex)
            {
                return SyncPackResponseHelper.Fail($"Unable to extract syncpack: {ex.Message}");
            }

            return SyncPackResponseHelper.Succeed(true);
        }

        private string GetSyncPackFilePath(string syncPackFileName)
        {
            return Path.Combine(syncPackSourceFolderPath, syncPackFileName);
        }
    }
}
