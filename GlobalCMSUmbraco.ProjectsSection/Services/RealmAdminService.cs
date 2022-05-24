using GlobalCMSUmbraco.MongoDbRealmAdmin;
using GlobalCMSUmbraco.MongoDbRealmAdmin.Api.Tasks;
using GlobalCMSUmbraco.MongoDbRealmAdmin.Cli.Tasks;
using GlobalCMSUmbraco.MongoDbRealmAdmin.Models;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace GlobalCMSUmbraco.ProjectsSection.Services
{
    public class RealmAdminService : IRealmAdminService
    {
        private readonly ICopyRealmProjectTemplateTask copyProjectTemplateTask;
        private readonly IPrepareRealmExportFilesTask prepareExportFilesTask;
        private readonly IExportRealmAppTask exportTask;
        private readonly ICreateRealmUserTask createRealmUserTask;
        private readonly IListAppsTask listAppsTask;
        private readonly ILogger logger;

        public RealmAdminService(ICopyRealmProjectTemplateTask copyProjectTemplateTask, IPrepareRealmExportFilesTask prepareExportFilesTask, IExportRealmAppTask exportTask, ICreateRealmUserTask createRealmUserTask, IListAppsTask listAppsTask, ILogger logger)
        {
            this.copyProjectTemplateTask = copyProjectTemplateTask;
            this.prepareExportFilesTask = prepareExportFilesTask;
            this.exportTask = exportTask;
            this.createRealmUserTask = createRealmUserTask;
            this.listAppsTask = listAppsTask;
            this.logger = logger;
        }

        #region Apps
        public async Task<string> CreatePreviewApp(string diageoProjectCode, IRealmConfig realmConfig, IAtlasApiCredentials apiCredentials)
        {
            return await CreateApp(diageoProjectCode, realmConfig, apiCredentials, true);
        }

        public async Task<string> CreatePublishedApp(string diageoProjectCode, IRealmConfig realmConfig, IAtlasApiCredentials apiCredentials)
        {
            return await CreateApp(diageoProjectCode, realmConfig, apiCredentials, false);
        }

        public async Task<string> CreateApp(string diageoProjectCode, IRealmConfig realmConfig, IAtlasApiCredentials apiCredentials, bool isPreview)
        {
            string templateDirectoryPath = GetLocalPathFromConfig("Realm:TemplatePath", "~/App_Data/MondoDbRealm/mfqcb");
            string exportDirectoryPath = GetLocalPathFromConfig("Realm:ExportPath", "~/App_Data/TEMP/MondoDbRealm");

            string realmCliExecPath = GetLocalPathFromConfig("Realm:RealmCliExecPath", "..\\..\\lib\\mongodb-realm-cli\\realm-cli.exe");
            string realmCliConfigFilePath = GetLocalPath("~/App_Data/TEMP/MondoDbRealm/config.txt");

            if (string.IsNullOrWhiteSpace(realmCliExecPath))
            {
                this.logger.Info<RealmAdminService>("Realm CLI not available");
                return null;
            }


            this.logger.Debug<RealmAdminService>("RealmCliExecPath: {RealmCliExecPath}", realmCliExecPath);
            this.logger.Debug<RealmAdminService>("RealmCliConfigFilePath: {RealmCliConfigFilePath}", realmCliConfigFilePath);

            return await CreateApp(templateDirectoryPath, diageoProjectCode, realmConfig, apiCredentials, isPreview, exportDirectoryPath, realmCliExecPath, realmCliConfigFilePath);
        }

        private async Task<string> CreateApp(string templatePath, string diageoProjectCode, IRealmConfig realmConfig, IAtlasApiCredentials apiCredentials, bool isPreview, string exportPath, string realmCliExecPath, string realmCliConfigFilePath)
        {
            var exportPathFullName = copyProjectTemplateTask.Execute(templatePath, exportPath);

            prepareExportFilesTask.Execute(exportPathFullName, diageoProjectCode, isPreview, realmConfig.ClusterName, realmConfig.ClusterLocation);

            var clientAppId = await exportTask.ExecuteAsync(realmCliExecPath, realmCliConfigFilePath, apiCredentials, realmConfig.RealmProjectId, exportPathFullName);

            return clientAppId;
        }

        private static string GetLocalPathFromConfig(string configKey, string defaultValue = null)
        {
            var configValue = ConfigurationManager.AppSettings[configKey];

            string path = configValue.IfNullOrWhiteSpace(defaultValue);
            return GetLocalPath(path);
        }

        private static string GetLocalPath(string path)
        {
            string localPath;
            if (path.StartsWith("~"))
            {
                localPath = HostingEnvironment.MapPath(path);
            }
            else if(Path.IsPathRooted(path) == false)
            {
                localPath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, path);
            }
            else
            {
                localPath = path;

                localPath = SpecialFolderReplacement(localPath, "%AppData%", Environment.SpecialFolder.ApplicationData);
                localPath = SpecialFolderReplacement(localPath, "%LocalAppData%", Environment.SpecialFolder.LocalApplicationData);
                localPath = SpecialFolderReplacement(localPath, "%ProgramFiles%", Environment.SpecialFolder.ProgramFiles);
                localPath = SpecialFolderReplacement(localPath, "%ProgramFiles(X86)%", Environment.SpecialFolder.ProgramFilesX86);
            }

            var di = new DirectoryInfo(localPath);
            return di.FullName;
        }

        private static string SpecialFolderReplacement(string path, string windowsPathKey, Environment.SpecialFolder specialFolder)
        {
            return path.Replace(windowsPathKey, Environment.GetFolderPath(specialFolder), StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion

        #region Users

        public async Task<IEnumerable<IRealmAppDetails>> GetAppDetails(IRealmConfig realmConfig, IAtlasApiCredentials adminApiCredentials)
        {
            var apps = await listAppsTask.Execute(adminApiCredentials, realmConfig.RealmProjectId);
            return apps;
        }

        public async Task<IRealmAppDetails> GetAppDetailsByClientAppId(string clientAppId, IRealmConfig realmConfig, IAtlasApiCredentials adminApiCredentials)
        {
            foreach(var app in await GetAppDetails(realmConfig, adminApiCredentials))
            {
                if (string.IsNullOrWhiteSpace(app?.ClientAppId))
                    continue;

                if (app.ClientAppId.Equals(clientAppId, StringComparison.InvariantCultureIgnoreCase))
                    return app;
            }

            return null;
        }

        public async Task<IRealmUser> CreateRealmUser(string appId, string userName, IRealmConfig realmConfig, IAtlasApiCredentials adminApiCredentials)
        {
            var user = await createRealmUserTask.Execute(adminApiCredentials, realmConfig.RealmProjectId, appId, userName);

            return user;
        }
        #endregion
    }
}
