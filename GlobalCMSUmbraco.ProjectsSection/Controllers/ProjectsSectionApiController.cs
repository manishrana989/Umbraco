using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GlobalCMSUmbraco.ProjectsSection.Models.Api;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using Umbraco.Web.WebApi;
using uSync.Expansions.Core;
using uSync.Exporter;

namespace GlobalCMSUmbraco.ProjectsSection.Controllers
{
    public class ProjectsSectionApiController : UmbracoAuthorizedApiController
    {
        private readonly IProjectsSectionService projectsSectionService;
        private readonly ICloneSyncPackService cloneSyncPackService;
        private readonly string appDataFolder;

        public ProjectsSectionApiController(IProjectsSectionService projectsSectionService, ICloneSyncPackService cloneSyncPackService)
        {
            this.projectsSectionService = projectsSectionService;
            this.cloneSyncPackService = cloneSyncPackService;
            this.appDataFolder = HttpContext.Current.Server.MapPath("~/App_Data/");
        }

        /// <summary>
        ///  GetApi - Called in our ServerVariablesParser.Parsing event handler
        ///  this gets the URL of the APIs, so we don't have to hardwire it anywhere
        /// </summary>
        [HttpGet]
        public bool GetApi() => true;

        /// <summary>
        /// Returns apiResponse.Success true if connection confirmed, apiResponse.ReturnValue is true if DeveloperEdition
        /// </summary>
        [HttpGet]
        public ApiResponse ConfirmConnection()
        {
            var apiResponse = projectsSectionService.ConfirmConnectionToMongoDb();
            apiResponse.ReturnValue = Common.AppSettings.IsDeveloperEdition;
            return apiResponse;
        }

        [HttpGet]
        public ApiResponse GetProjects()
        {
            return projectsSectionService.GetProjectsList(UmbracoContext.Security.CurrentUser);
        }

        [HttpPost]
        public ApiResponse AddProject(AddProjectModel project)
        {
            if (HttpContext.Current == null)
            {
                return null;
            }

            return projectsSectionService.AddProject(project, UmbracoContext.Security.CurrentUser);
        }

        [HttpGet]
        public ApiResponse GetStarterKits()
        {
            if (HttpContext.Current == null)
            {
                return null;
            }

            return projectsSectionService.GetStarterKits(appDataFolder);
        }

        [HttpPost]
        public ExporterResponse CloneSyncPack(AddProjectModel project)
        {
            if (HttpContext.Current == null)
            {
                return null;
            }

            // do initial checks, set variables etc
            if (string.IsNullOrEmpty(project.Code))
            {
                return new ExporterResponse
                {
                    Response = SyncPackResponseHelper.Fail("Missing project code")
                };
            }

            cloneSyncPackService.CreateCloneSteps(project.USyncRequest, project.Code);
            return ReportProgress(project.USyncRequest);
        }

        [HttpPost]
        public ExporterResponse ReportProgress(ExporterRequest syncRequest)
        {
            return cloneSyncPackService.Process(syncRequest);
        }

        [HttpPost]
        public ApiResponse ContentImported(AddProjectModel project)
        {
            return projectsSectionService.ContentImported(project.Code, project.SelectedSyncPackFile, true);
        }

        [HttpGet]
        public ApiResponse GetProjectOverview(string id)
        {
            return projectsSectionService.GetProjectOverview(id);
        }

        [HttpGet]
        public ApiResponse GetProjectSettings(string id)
        {
            return projectsSectionService.GetProjectSettings(id);
        }

        [HttpPost]
        public ApiResponse SaveProjectSettings(string id,  ProjectSettingsModel settings)
        {
            return projectsSectionService.SaveProjectSettings(id, settings);
        }

        [HttpPost]
        public ApiResponse SaveProjectDelete(string id)
        {
            return projectsSectionService.DeleteProject(id);
        }

        [HttpGet]
        public ApiResponse GetProjectStarterKit(string id)
        {
            return projectsSectionService.GetProjectStarterKit(id, appDataFolder);
        }

        [HttpGet]
        public ApiResponse GetRealmStatus(string id)
        {
            return projectsSectionService.GetRealmStatus(id);
        }

        [HttpPost]
        public async Task<ApiResponse> CreateRealmApps(string id)
        {
            return await projectsSectionService.CreateRealmAppsAsync(id);
        }
    }
}
 