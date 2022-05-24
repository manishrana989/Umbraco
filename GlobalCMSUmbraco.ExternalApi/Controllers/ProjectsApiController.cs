using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using GlobalCMSUmbraco.ExternalApi.Attributes;
using GlobalCMSUmbraco.ExternalApi.Models;
using GlobalCMSUmbraco.ExternalApi.Services;
using GlobalCMSUmbraco.ProjectsSection.Models.Api;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using Hangfire;
using Swashbuckle.Swagger.Annotations;
using Umbraco.Core.Logging;

namespace GlobalCMSUmbraco.ExternalApi.Controllers
{
    /// <summary>
    /// Projects CRUD external api
    /// </summary>
    [JwtBearerTokenAuthorization]
    [RoutePrefix("ExternalApi/Projects")]
    [ShowInSwagger]
    public class ProjectsApiController : BaseExternalApiController
    {
        private readonly IProjectsSectionService projectsSectionService;
        private readonly IBackgroundTasksService backgroundTasksService;
        private readonly string appDataFolder;
        private readonly ILogger logger;

        public ProjectsApiController(IProjectsSectionService projectsSectionService, IBackgroundTasksService backgroundTasksService, ILogger logger)
        {
            this.projectsSectionService = projectsSectionService;
            this.backgroundTasksService = backgroundTasksService;
            this.appDataFolder = HttpContext.Current.Server.MapPath("~/App_Data/");
            this.logger = logger;
        }

        /// <summary>
        /// Add a new project (Add Step 1)
        /// </summary>
        /// <remarks>
        /// Call this endpoint to create a new project in the targeted environment.
        ///
        /// If the project is successfully created a `200` response code is returned with a boolean value. If `true` then Realm Apps are enabled in this environment so you should continue with Step 2. If `false` then Realm Apps are disabled so you should go to Step 3. 
        ///
        /// If the project failed to be created a `400` response code is returned with the reason for the failure.
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        /// <param name="displayName">Description for the project (optional, for internal use only)</param>
        [HttpPost]
        [Route("CreateProject")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(bool))]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        public IHttpActionResult CreateProject(string projectCode, string displayName = null)
        {
            logger.Info<ProjectsApiController>("API CreateProject started");
            
            var model = new AddProjectModel
            {
                Code = projectCode,
                DisplayName = displayName,
                MongoDbPublish = projectCode,
                MongoDbPreview = projectCode + "-Preview"
            };

            var response = projectsSectionService.AddProject(model, null);
            if (response.Success)
            {
                // project created, return whether should proceed to Realm step, or side-step
                if (bool.TryParse(ConfigurationManager.AppSettings["Realm:Disable"], out var disableRealm) && disableRealm)
                {
                    logger.Info<ProjectsApiController>("API CreateProject completed, project: {code}", projectCode);
                    return Ok(false);
                }
                logger.Info<ProjectsApiController>("API CreateProject completed, project: {code}", projectCode);
                return Ok(true);
            }

            logger.Info<ProjectsApiController>("API CreateProject aborted, project: {code}, reason: {reason}", projectCode, response.Error);
            return BadRequest(response.Error);
        }

        /// <summary>
        /// Create Realm apps (Add Step 2)
        /// </summary>
        /// <remarks>
        /// Call this endpoint to create the Realm Apps for this project. Please note that creating Realm Apps can take some time to complete.
        /// A `200` response code is returned when the action has completed.
        ///
        /// Use the `GetOverview` endpoint if you want to check the status of the Realm Apps for this project before calling this endpoint.
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        [HttpPost]
        [Route("ConfigureRealm")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public async Task<IHttpActionResult> ConfigureRealm(string projectCode)
        {
            logger.Info<ProjectsApiController>("API ConfigureRealm started, project: {code}", projectCode);

            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                logger.Info<ProjectsApiController>("API ConfigureRealm aborted, project code {code} not recognised", projectCode);
                return NotFound();
            }

            var response = await projectsSectionService.CreateRealmAppsAsync(projectId);
            if (response.Success)
            {
                logger.Info<ProjectsApiController>("API ConfigureRealm completed, project: {code}", projectCode);
                return Ok();
            }

            logger.Info<ProjectsApiController>("API ConfigureRealm aborted, project: {code}, reason: ({reason})", projectCode, response.Error);
            return BadRequest(response.Error);
        }


        /// <summary>
        /// Create Realm User (Add Step 3)
        /// </summary>
        /// <remarks>
        /// Call this endpoint to create a MongoDb Realm user. 
        /// For project setup this will need to be called TWICE (once with published true and once with published false)
        /// A `200` response code is returned when the action has completed.
        /// 
        /// Requires the Realm apps to have already been created (use the ConfigureRealm method)
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        /// <param name="publishedApplication">True if the api key is for the published application. False if the api key is for the preview application</param>
        /// <param name="userName">The name of the user, containing no spaces (optional)</param>
        [HttpPost]
        [Route("CreateRealmUser")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public async Task<IHttpActionResult> CreateRealmUser(string projectCode, bool publishedApplication, string userName = null)
        {
            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                return NotFound();
            }

            var response = await projectsSectionService.CreateRealmUserAsync(projectId, publishedApplication, userName);
            if (response.Success)
            {
                var returnModel = RealmUser.FromRealmUserModel((RealmUserModel)response.ReturnValue);
                return Ok(returnModel);
            }

            return BadRequest(response.Error);
        }

        /// <summary>
        /// Add starting content for the project (Add Step 4)
        /// </summary>
        /// <remarks>
        /// Call this endpoint to request the creation of the starting content for this project from a starter kit.
        /// A `202` response code is returned when the request has been Accepted.
        ///
        /// The process is run as a background task. Use the `GetOverview` endpoint to check whether the process has completed: if the `HasContent` property is `true` then the content creation process has completed, so the next step can be run.
        ///
        /// Use the `GetStarterKits` endpoint to get the list of available starter kits.
        ///
        /// Use the `GetOverview` endpoint if you want to check whether this project already has content (starter kits cannot be used once content has been created)
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        /// <param name="starterKitId">The Id of the chosen starter kit</param>
        [HttpPost]
        [Route("CloneStarterKit")]
        [SwaggerResponseRemoveDefaults]
        [SwaggerResponse(HttpStatusCode.Accepted)]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult CloneStarterKit(string projectCode, string starterKitId)
        {
            logger.Info<ProjectsApiController>("API CloneStarterKit started, project: {code}", projectCode);

            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                logger.Info<ProjectsApiController>("API CloneStarterKit aborted, project code {code} not recognised", projectCode);
                return NotFound();
            }

            // validate starter kit id
            var apiStarterKits = projectsSectionService.GetStarterKitsForExternalApi(appDataFolder, ApiConstants.StarterKitDoesNotContain);
            if (apiStarterKits == null || !apiStarterKits.ContainsKey(starterKitId))
            {
                logger.Info<ProjectsApiController>("API CloneStarterKit aborted, starter kit id {starterKitId} not recognised", starterKitId);
                return BadRequest("Invalid starter kit id");
            }

            var response = projectsSectionService.GetProjectOverview(projectId);
            if (response.Success)
            {
                var overview = ProjectOverview.FromProjectOverviewModel((ProjectOverviewModel) response.ReturnValue);
                if (overview.HasContent)
                {
                    logger.Info<ProjectsApiController>("API CloneStarterKit aborted, project: {code}, reason: project already has content", projectCode);
                    return BadRequest($"Project '{projectCode}' already has content");
                }

                // start cloning in the background (using a Hangfire BackgroundJob)
                BackgroundJob.Enqueue(() => backgroundTasksService.StartStarterKitClone(projectCode, starterKitId));

                logger.Info<ProjectsApiController>("API CloneStarterKit request accepted, project: {code}, starter kit: {starterKitId}", projectCode, starterKitId);
                return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.Accepted));
            }

            logger.Info<ProjectsApiController>("API CloneStarterKit aborted, project: {code}, reason: {reason}", projectCode, response.Error);
            return BadRequest(response.Error);
        }

        /// <summary>
        /// Add a user for the project (Add Step 5)
        /// </summary>
        /// <remarks>
        /// Call this endpoint to create a 'project' administrator user for this project.
        /// A `200` response code is returned when the action has completed.
        ///
        /// Use the `GetOverview` endpoint if you want to check whether this project already has permissions configured. Permissions cannot be configured until after the project has some content.
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        /// <param name="name">Display name of the Project Administrator</param>
        /// <param name="email">Email address of the Project Administrator</param>
        [HttpPost]
        [Route("ConfigurePermissions")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult ConfigurePermissions(string projectCode, string name, string email)
        {
            logger.Info<ProjectsApiController>("API ConfigurePermissions started, project: {code}", projectCode);

            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                logger.Info<ProjectsApiController>("API ConfigurePermissions aborted, project code {code} not recognised", projectCode);
                return NotFound();
            }

            var response = projectsSectionService.GetProjectOverview(projectId);
            if (response.Success)
            {
                var overview = ProjectOverview.FromProjectOverviewModel((ProjectOverviewModel)response.ReturnValue);
                if (!overview.HasContent)
                {
                    logger.Info<ProjectsApiController>("API ConfigurePermissions aborted, project: {code}, reason: project has no content", projectCode);
                    return BadRequest($"Permissions cannot be configured for '{projectCode}' as the project has no content");
                }
                if (overview.HasPermissions)
                {
                    logger.Info<ProjectsApiController>("API ConfigurePermissions aborted, project: {code}, reason: permissions already configured", projectCode);
                    return BadRequest($"Project '{projectCode}' permissions have already been configured");
                }

                response = projectsSectionService.CreatePermissions(projectCode, name, email);
                if (response.Success)
                {
                    logger.Info<ProjectsApiController>("API ConfigurePermissions completed, project: {code}", projectCode);
                    return Ok();
                }

                logger.Info<ProjectsApiController>("API ConfigurePermissions aborted, project: {code}, reason: {reason}", projectCode, response.Error);
                return BadRequest(response.Error);
            }

            logger.Info<ProjectsApiController>("API ConfigurePermissions aborted, project: {code}, reason: {reason}", projectCode, response.Error);
            return BadRequest(response.Error);
        }

        /// <summary>
        /// Get available starter kits
        /// </summary>
        /// <remarks>
        /// This endpoint returns the available starter kits as an array with the following properties:
        /// 
        /// - `Id`: the value that should be used when calling the `CloneStarterKit` endpoint
        /// - `Name`: should be displayed to the user to make their selection
        /// </remarks>
        [HttpGet]
        [Route("GetStarterKits")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(List<StarterKit>))]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        public IHttpActionResult GetStarterKits()
        {
            logger.Info<ProjectsApiController>("API GetStarterKits started");

            var apiStarterKits = projectsSectionService.GetStarterKitsForExternalApi(appDataFolder, ApiConstants.StarterKitDoesNotContain);
            if (apiStarterKits != null && apiStarterKits.Count > 0)
            {
                logger.Info<ProjectsApiController>("API GetStarterKits completed");
                return Ok(apiStarterKits.Select(x => new StarterKit
                    {
                        Id = x.Key,
                        Name = x.Value
                    })
                    .ToList());
            }

            logger.Info<ProjectsApiController>("API GetStarterKits aborted, reason: no starter kits found");
            return BadRequest("No starter kits found");
        }

        /// <summary>
        /// Get project details and current status
        /// </summary>
        /// <remarks>
        /// This endpoint returns all the key details for a project, plus flags for whether operations (e.g. Delete) can be done.
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        [HttpGet]
        [Route("GetOverview")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ProjectOverview))]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetOverview(string projectCode)
        {
            logger.Info<ProjectsApiController>("API GetOverview started, project: {code}", projectCode);

            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                logger.Info<ProjectsApiController>("API GetOverview aborted, project code {code} not recognised", projectCode);
                return NotFound();
            }

            var response = projectsSectionService.GetProjectOverview(projectId);
            if (response.Success)
            {
                var returnModel = ProjectOverview.FromProjectOverviewModel((ProjectOverviewModel) response.ReturnValue);
                logger.Info<ProjectsApiController>("API GetOverview completed, project: {code}", projectCode);
                return Ok(returnModel);
            }

            logger.Info<ProjectsApiController>("API GetOverview aborted, project: {code}, reason: {reason}", projectCode, response.Error);
            return BadRequest(response.Error);
        }

        /// <summary>
        /// Get project's current settings
        /// </summary>
        /// <remarks>
        /// This endpoint returns all configurable project settings:
        /// - `DisplayName`: project description (optional, for internal use only)
        /// - `FrontEndUrl`: url of the front-end website, e.g. https://domain.com
        /// - `CmsMapsApiKey`: key to be used in the CMS for maps API for this project
        /// - `FrontEndMapsApiKey`: key to be used in the front-end for maps API for this project
        /// - `ApprovalEmailReminderMins`: frequency (in minutes) to send content approval email reminders to editors. Set to 0 to indicate no approval reminder emails should be sent. 
        /// - `BrandName`: used in DCH image searching
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        [HttpGet]
        [Route("GetSettings")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ProjectSettings))]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetSettings(string projectCode)
        {
            logger.Info<ProjectsApiController>("API GetSettings started, project: {code}", projectCode);

            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                logger.Info<ProjectsApiController>("API GetSettings aborted, project code {code} not recognised", projectCode);
                return NotFound();
            }

            var response = projectsSectionService.GetProjectSettings(projectId);
            if (response.Success)
            {
                var returnModel = ProjectSettings.FromProjectSettingsModel((ProjectSettingsModel)response.ReturnValue);
                logger.Info<ProjectsApiController>("API GetSettings completed, project: {code}", projectCode);
                return Ok(returnModel);
            }

            logger.Info<ProjectsApiController>("API GetSettings aborted, project: {code}, reason: {reason}", projectCode, response.Error);
            return BadRequest(response.Error);
        }

        /// <summary>
        /// Save changes to project settings
        /// </summary>
        /// <remarks>
        /// Call this endpoint to update the editable project settings.
        /// A `200` response code is returned when the action has completed.
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        /// <param name="settings">Object with updated project settings</param>
        [HttpPost]
        [Route("UpdateSettings")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult UpdateSettings(string projectCode, ProjectSettings settings)
        {
            logger.Info<ProjectsApiController>("API UpdateSettings started, project: {code}", projectCode);

            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                logger.Info<ProjectsApiController>("API UpdateSettings aborted, project code {code} not recognised", projectCode);
                return NotFound();
            }

            var projectSettingsModel = settings.GetProjectSettingsModel(projectId, projectCode);
            var response = projectsSectionService.SaveProjectSettings(projectId, projectSettingsModel);
            if (response.Success)
            {
                logger.Info<ProjectsApiController>("API UpdateSettings completed, project: {code}", projectCode);
                return Ok();
            }

            logger.Info<ProjectsApiController>("API UpdateSettings aborted, project: {code}, reason: {reason}", projectCode, response.Error);
            return BadRequest(response.Error);
        }

        /// <summary>
        /// Delete this project
        /// </summary>
        /// <remarks>
        /// Call this endpoint to delete the project. This can only be done if no content or users have been created already.
        /// A `200` response code is returned when the action has completed.
        ///
        /// Use the `GetOverview` endpoint to check whether this project can be deleted.
        /// </remarks>
        /// <param name="projectCode">Project code, e.g. PR1234</param>
        [HttpPost]
        [Route("DeleteProject")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.BadRequest, Type = typeof(string))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult DeleteProject(string projectCode)
        {
            logger.Info<ProjectsApiController>("API DeleteProject started, project: {code}", projectCode);

            var projectId = projectsSectionService.GetProjectIdByCode(projectCode);
            if (string.IsNullOrEmpty(projectId))
            {
                logger.Info<ProjectsApiController>("API DeleteProject aborted, project code {code} not recognised", projectCode);
                return NotFound();
            }

            var response = projectsSectionService.DeleteProject(projectId);
            if (response.Success)
            {
                logger.Info<ProjectsApiController>("API DeleteProject completed, project: {code}", projectCode);
                return Ok();
            }

            logger.Info<ProjectsApiController>("API DeleteProject aborted, project: {code}, reason: {reason}", projectCode, response.Error);
            return BadRequest(response.Error);
        }
    }
}
