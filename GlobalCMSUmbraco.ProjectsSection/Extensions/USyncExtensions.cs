using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobalCMSUmbraco.ProjectsSection.Models;
using Umbraco.Core;
using uSync.Expansions.Core;
using uSync.Expansions.Core.Models;
using uSync.Expansions.Core.Services;
using uSync.Exporter;
using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Hubs;
using uSync8.Core.Serialization;

namespace GlobalCMSUmbraco.ProjectsSection.Extensions
{
    public static class USyncExtensions
    {
        public static SyncSerializerOptions GetSerializerOptions(this HandlerSettings settings, uSyncImportOptions importOptions)
        {
            var serializerOptions = new SyncSerializerOptions(importOptions.Flags, settings.Settings);
            serializerOptions.MergeSettings(importOptions.Settings);
            return serializerOptions;
        }

        public static void IncrementStep(this ExporterResponse exportResponse, IList<ExporterStep> steps)
        {
            if (exportResponse.Response.AllPagesProcessed)
            {
                ++exportResponse.StepIndex;
                exportResponse.NextPage = 0;
            }

            if (exportResponse.StepIndex < steps.Count)
                return;

            exportResponse.ExportComplete = true;
        }

        public static void IncrementPage(this ExporterResponse exporterResponse, int currentPage)
        {
            if (exporterResponse.Response.AllPagesProcessed || exporterResponse.Response.ResetPaging)
                exporterResponse.NextPage = 0;
            else
                exporterResponse.NextPage = currentPage + 1;
        }

        public static void UpdateRequestFromConfig(this SyncPackRequest request, uSyncConfig config, SyncHandlerService handlerService)
        {
            request.HandlerSet = request.HandlerSet ?? config.GetExtensionSetting<string>(uSyncExporter.AppName, "HandlerSet", handlerService.DefaultSet());
            request.PageSize = config.GetExtensionSetting(uSyncExporter.AppName, "PageSize", 50);
            
            if (request.Options == null)
                request.Options = new SyncPackOptions();

            if (!config.GetExtensionSetting(uSyncExporter.AppName, "NoFolders", false))
            {
                var delimitedList1 = config.GetExtensionSetting<string>(uSyncExporter.AppName, "AdditionalFolders", string.Empty).ToDelimitedList();
                var delimitedList2 = config.GetExtensionSetting<string>(uSyncExporter.AppName, "AdditionalExclusions", string.Empty).ToDelimitedList();
                
                request.Options.Folders.Merge(delimitedList1);
                request.Options.SystemExclusions.Merge(delimitedList2);
                request.Options.SystemExclusions.CleanRegExPatterns();
            }
            else
                request.Options.Folders = new List<string>();
        }

        public static ExporterResponse ProcessSteps(this ExporterRequest request, List<ExporterStep> steps, uSyncConfig config, SyncHandlerService handlerService)
        {
            if (request.StepIndex >= steps.Count)
                return new ExporterResponse
                {
                    ExportComplete = true
                };

            if (request.Id == Guid.Empty)
                request.Id = Guid.NewGuid();

            request.Request.UpdateRequestFromConfig(config, handlerService);
            request.Request.Callbacks = new HubClientService(request.clientId).Callbacks();

            var step = steps[request.StepIndex];
            var exporterResponse = new ExporterResponse
            {
                StepIndex = request.StepIndex,
                Response = step.Step(request.Id, request.Request),
                Id = request.Id
            };

            if (exporterResponse.Response.Items == null || !exporterResponse.Response.Items.Any())
                exporterResponse.Response.Items = request.Request.Items;

            exporterResponse.NextFolder = exporterResponse.Response.NextFolder;
                
            exporterResponse.IncrementPage(request.Request.PageNumber);
            exporterResponse.IncrementStep(steps);
            exporterResponse.Progress = SyncStepProgressHelper.UpdateProgress(steps, exporterResponse.StepIndex);
            return exporterResponse;
        }

        public static bool AllFoldersAreProcessed(this SyncPackRequest request, string folder) => string.IsNullOrWhiteSpace(folder);

        public static string GetNextHandlerFolder(this SyncPackRequest request, string currentFolder, IEnumerable<string> handlerFolders)
        {
            var list = handlerFolders.ToList();
            var num = list.IndexOf(currentFolder);
            if (num != -1)
            {
                var index = num + 1;
                if (index < list.Count)
                    return list[index];
            }

            return string.Empty;
        }

        public static USyncFolderInfo GetPagedFolder(this SyncPackRequest request, IEnumerable<string> handlerFolders)
        {
            var list = handlerFolders.ToList();
            if (!list.Any())
                return null;

            var folderInfo = new USyncFolderInfo
            {
                ProgressMax = 100
            };

            if (string.IsNullOrWhiteSpace(request.HandlerFolder))
            {
                if (list.Count > 0)
                {
                    folderInfo.ProgressMin = 0;
                    folderInfo.ProgressMax = 100 / list.Count;
                    folderInfo.FolderName = list[0];
                    return folderInfo;
                }
            }
            else if (list.Contains(request.HandlerFolder))
            {
                folderInfo.FolderName = request.HandlerFolder;
                var num1 = 100 / list.Count;
                var num2 = list.IndexOf(request.HandlerFolder);
                folderInfo.ProgressMin = num1 * num2;
                folderInfo.ProgressMax = num1 * num2 + num1;
                return folderInfo;
            }

            throw new DirectoryNotFoundException(request.HandlerFolder);
        }

        public static uSyncPagedImportOptions GetImportOptions(this SyncPackRequest request, int min, int max)
        {
            var pagedImportOptions = new uSyncPagedImportOptions
            {
                HandlerSet = request.HandlerSet,
                Callbacks = request.Callbacks,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                ProgressMin = min,
                ProgressMax = max
            };

            if (pagedImportOptions.Settings == null)
                pagedImportOptions.Settings = new Dictionary<string, string>();

            if (request.Options.Cultures != null && request.Options.Cultures.Any())
            {
                pagedImportOptions.Settings["Cultures"] = string.Join(",", request.Options.Cultures);
            }

            return pagedImportOptions;
        }

        public static int CalculateProgress(this uSyncPagedImportOptions options, int value, int total) => (int) ((double) options.ProgressMin + (double) value / (double) total * (double) (options.ProgressMax - options.ProgressMin));
        public static bool IsPagingComplete(this SyncPackRequest request, int total) => request.PageSize == 0 || request.PageNumber * request.PageSize + request.PageSize >= total;
    }
}