using GlobalCMSUmbraco.MongoDbRealmAdmin.Models;
using GlobalCMSUmbraco.ProjectsSection.Models.Api;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.ProjectDb;
using GlobalCMSUmbraco.ProjectsSection.Repositories;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;
using Umbraco.Web.Security.Providers;

namespace GlobalCMSUmbraco.ProjectsSection.Services
{
    public class ProjectsSectionService : IProjectsSectionService
    {
        private readonly IProjectsMongoDbService projectsMongoDbService;
        private readonly IRealmAdminService realmAdminService;
        private readonly IContentService contentService;
        private readonly IMediaService mediaService;
        private readonly IUserService userService;
        private readonly IStarterKitsRepository starterKitsRepo;
        private readonly ILogger logger;

        public ProjectsSectionService(IProjectsMongoDbService projectsMongoDbService, IRealmAdminService realmAdminService, IContentService contentService, IMediaService mediaService, IUserService userService, IStarterKitsRepository starterKitsRepo, ILogger logger)
        {
            this.projectsMongoDbService = projectsMongoDbService;
            this.realmAdminService = realmAdminService;
            this.contentService = contentService;
            this.mediaService = mediaService;
            this.userService = userService;
            this.starterKitsRepo = starterKitsRepo;
            this.logger = logger;
        }

        public ApiResponse ConfirmConnectionToMongoDb()
        {
            var response = new ApiResponse();

            // this will create the Projects collection if doesn't exist already
            try
            {
                projectsMongoDbService.CreateMongoDbCollectionIfNotExists(ProjectConstants.GlobalCmsMongoDbDatabase, ProjectConstants.GlobalCmsProjectsCollection);
                response.Success = true;
            }
            catch (Exception ex)
            {
                logger.Error<ProjectsSectionService>(ex, ex.Message);
                response.Error = "Failed to connect to Projects database, see administrator log for details";
            }

            return response;
        }

        public ApiResponse GetProjectsList(IUser currentUser)
        {
            var response = new ApiResponse();

            IList<ProjectModel> projects;
            try
            {
                projects = projectsMongoDbService.GetProjectsList(currentUser);
            }
            catch (Exception exception)
            {
                logger.Error<ProjectsSectionService>(exception, exception.Message);
                response.ReturnValue = Array.Empty<ProjectModel>();
                response.Error = "Failed to retrieve projects";
                return response;
            }

            response.Success = true;
            response.ReturnValue = projects;
            return response;
        }

        public ApiResponse AddProject(AddProjectModel apiProject, IUser currentUser)
        {
            var project = new ProjectModel
            {
                Code = apiProject.Code,
                DisplayName = apiProject.DisplayName,
                MongoDbPublish = apiProject.MongoDbPublish,
                MongoDbPreview = apiProject.MongoDbPreview,
                CreatedDateUtc = DateTime.UtcNow,
                CreatedByName = currentUser?.Name,
                CreatedById = currentUser?.Id ?? 0
            };

            if (!projectsMongoDbService.AddProject(project, out var failReason))
            {
                return new ApiResponse { Error = failReason };
            }

            return new ApiResponse { Success = true, ReturnValue = project.Id };
        }

        public ApiResponse GetStarterKits(string folderRoot)
        {
            var starterKits = starterKitsRepo.GetStarterKits(folderRoot, out var errorMessage);
            var response = new ApiResponse
            {
                ReturnValue = starterKits,
                Success = starterKits != null,
                Error = errorMessage
            };

            return response;
        }

        public Dictionary<string,string> GetStarterKitsForExternalApi(string folderRoot, string starterKitDoesNotContain)
        {
            var allStarterKits = starterKitsRepo.GetStarterKits(folderRoot, out var errorMessage);

            var apiStarterKits = allStarterKits?.Where(x =>
                !x.Key.ToLower().Contains(starterKitDoesNotContain.ToLower()))
                .ToDictionary(x => x.Key, x => x.Value);

            return apiStarterKits;
        }

        public ApiResponse ContentImported(string projectCode, string selectedSyncPackFile, bool createUserGroup)
        {
            var response = new ApiResponse();

            var dbProject = projectsMongoDbService.GetProjectByCode(projectCode);
            if (dbProject == null)
            {
                response.Error = $"Project {projectCode} not found";
                return response;
            }

            // log which starter kit was used to create clone from
            dbProject.StarterKit = selectedSyncPackFile;

            if (!SetProjectContentRootNode(dbProject))
            {
                // something has gone wrong if no content root node for this project
                response.Error = $"No '{dbProject.Code}' root content node found, did you miss the import step?";
                return response;
            }

            SetMediaContentRootNode(dbProject);     // don't think the task should fail if no media, perhaps a starter kit won't have one?

            if (createUserGroup)
            {
                CreateUserGroupIfNotExisting(dbProject);
            }

            // finally write back to mongodb
            if (!projectsMongoDbService.SaveProject(dbProject))
            {
                response.Error = "Project failed to be updated";
                return response;
            }

            response.Success = true;
            return response;
        }

        public ApiResponse CreatePermissions(string projectCode, string userName, string userEmail)
        {
            var response = new ApiResponse();

            var dbProject = projectsMongoDbService.GetProjectByCode(projectCode);
            if (dbProject == null)
            {
                response.Error = $"Project '{projectCode}' not found";
                return response;
            }

            if (!dbProject.ContentNodeId.HasValue || !dbProject.MediaNodeId.HasValue)
            {
                response.Error = $"Project '{projectCode}' has no content or media";
                return response;
            }

            // need to check that user doesn't exist before proceeding
            if (!string.IsNullOrEmpty(userEmail) && userService.GetByEmail(userEmail) != null)
            {
                response.Error = $"User email '{userEmail}' is already in use";
                return response;
            }

            // get (or create) the project user group
            var projectUserGroup = CreateUserGroupIfNotExisting(dbProject) as IReadOnlyUserGroup;

            // write back to mongodb in case user group was created / link was set
            if (!projectsMongoDbService.SaveProject(dbProject))
            {
                response.Error = "Project failed to be updated";
                return response;
            }

            var projectAdminGroup = userService.GetUserGroupByAlias(ProjectConstants.ProjectAdministratorUserGroupAlias) as IReadOnlyUserGroup;
            if (projectAdminGroup == null)
            {
                logger.Info<ProjectsSectionService>("No User Group found with alias {alias}", ProjectConstants.ProjectAdministratorUserGroupAlias);
            }


            if (!string.IsNullOrEmpty(userName) || !string.IsNullOrWhiteSpace(userEmail))
            {
                // add new backoffice user with random password
                var user = userService.CreateUserWithIdentity(userEmail, userEmail);
                var newPassword = GenerateNewPassword();
                user.RawPasswordValue = (Membership.Providers["UsersMembershipProvider"] as UsersMembershipProvider).HashPasswordForStorage(newPassword);
                user.Name = userName;

                // add user to project specific and admin groups
                user.AddGroup(projectUserGroup);
                if (projectAdminGroup != null)
                {
                    user.AddGroup(projectAdminGroup);
                }
                userService.Save(user);

                SendEmailToNewProjectAdminUser(projectCode, userName, userEmail, newPassword);
            }

            response.Success = true;
            return response;
        }

        public ApiResponse GetProjectOverview(string projectId)
        {
            var response = new ApiResponse();

            var project = projectsMongoDbService.GetProjectById(projectId);
            if (project == null)
            {
                response.Error = $"Project id '{projectId}' not found";
                return response;
            }

            var projectOverview = new ProjectOverviewModel
            {
                Id = projectId,
                Code = project.Code,
                DisplayName = project.DisplayName,
                MongoDbPublish = project.MongoDbPublish,
                MongoDbPreview = project.MongoDbPreview,
                MongoDbRealmPublishApp = project.MongoDbRealmPublishClientAppId,
                MongoDbRealmPreviewApp = project.MongoDbRealmPreviewClientAppId,
                FrontEndUrl = project.FrontEndUrl,
                FeConfigFile = project.FeConfigFile,
                CanDelete = true
            };

            if (project.ContentNodeId.GetValueOrDefault() > 0 || ProjectContentRootNodeExists(project.Code))
            {
                projectOverview.CanDelete = false;
                if (project.ContentNodeId.HasValue)
                {
                    projectOverview.ContentNodeUrl = $"/umbraco/#/content/content/edit/{project.ContentNodeId.Value}";
                }
            }

            if (project.MediaNodeId.GetValueOrDefault() > 0 || ProjectMediaRootNodeExists(project.Code))
            {
                projectOverview.CanDelete = false;
                if (project.MediaNodeId.HasValue)
                {
                    projectOverview.MediaNodeUrl = $"/umbraco#/media/media/edit/{project.MediaNodeId.Value}";
                }
            }

            if (project.UserGroupId.GetValueOrDefault() > 0)
            {
                projectOverview.CanDelete = false;
                projectOverview.UserGroupUrl = $"/umbraco#/users/users/group/{project.UserGroupId}";

                if (userService.GetUserGroupById(project.UserGroupId.Value) is IReadOnlyUserGroup projectGroup)
                {
                    projectOverview.HasProjectUsers = userService.GetAllInGroup(projectGroup.Id).Any();
                }
            }

            response.Success = true;
            response.ReturnValue = projectOverview;

            return response;
        }

        public ApiResponse GetProjectSettings(string projectId)
        {
            var response = new ApiResponse();

            var project = projectsMongoDbService.GetProjectById(projectId);
            if (project == null)
            {
                response.Error = $"Project id '{projectId}' not found";
                return response;
            }

            var model = new ProjectSettingsModel
            {
                Id = projectId,
                Code = project.Code,
                DisplayName = project.DisplayName,
                FrontEndUrl = project.FrontEndUrl,
                FeConfigFile = project.FeConfigFile,
                CmsMapsApiKey = project.CmsMapsApiKey,
                ApprovalEmailReminderMins = project.ApprovalEmailReminderMins.GetValueOrDefault(),
                BrandName = project.BrandName,
                DchOverrideSettings = project.DchOverrideSettings,
                DchClientId = project.DchClientId,
                DchClientSecret = project.DchClientSecret,
                DchEndpointSecret = project.DchEndpointSecret,
            };

            // Get FrontEndMapsApiKey from Projects own db
            if (!string.IsNullOrEmpty(project.MongoDbPublish))
            {
                var parameters = projectsMongoDbService.GetParametersForProject(project.MongoDbPublish);
                if (parameters != null)
                {
                    model.FrontEndMapsApiKey = parameters.MapsApiKey;
                }
            }

            response.Success = true;
            response.ReturnValue = model;

            return response;
        }

        public ApiResponse SaveProjectSettings(string projectId, ProjectSettingsModel settings)
        {
            var response = new ApiResponse();

            // retrieve project to be updated
            var dbProject = projectsMongoDbService.GetProjectById(projectId);
            if (dbProject == null)
            {
                response.Error = $"Project id '{projectId}' not found";
                return response;
            }

            // update project properties
            dbProject.DisplayName = settings.DisplayName;
            dbProject.FrontEndUrl = settings.FrontEndUrl;
            dbProject.FeConfigFile = settings.FeConfigFile;
            dbProject.CmsMapsApiKey = settings.CmsMapsApiKey;
            dbProject.ApprovalEmailReminderMins = settings.ApprovalEmailReminderMins;
            dbProject.BrandName = settings.BrandName;
            dbProject.DchOverrideSettings = settings.DchOverrideSettings;
            dbProject.DchClientId = settings.DchClientId;
            dbProject.DchClientSecret = settings.DchClientSecret;
            dbProject.DchEndpointSecret = settings.DchEndpointSecret;

            // write back to mongodb
            if (!projectsMongoDbService.SaveProject(dbProject))
            {
                response.Error = "Project failed to be updated";
                return response;
            }

            // write front-end parameters to Publish db
            if (!string.IsNullOrEmpty(dbProject.MongoDbPublish))
            {
                var parameters = projectsMongoDbService.GetParametersForProject(dbProject.MongoDbPublish) ?? new ParametersModel();
                parameters.MapsApiKey = settings.FrontEndMapsApiKey;
                projectsMongoDbService.SaveParametersForProject(dbProject.MongoDbPublish, parameters);
            }

            response.Success = true;
            return response;
        }

        public ApiResponse DeleteProject(string projectId)
        {
            var response = new ApiResponse();

            var project = projectsMongoDbService.GetProjectById(projectId);
            if (project == null)
            {
                response.Error = $"Project id '{projectId}' not found";
                return response;
            }

            var overviewResponse = GetProjectOverview(projectId);
            if (((ProjectOverviewModel)overviewResponse.ReturnValue).CanDelete == false)
            {
                response.Error = $"Project '{project.Code}' cannot be deleted";
                return response;
            }

            projectsMongoDbService.DeleteProjectMongoDbDatabases(project.MongoDbPublish, project.MongoDbPreview);
            if (projectsMongoDbService.DeleteProjectFromProjectsList(project.Id))
            {
                response.Success = true;
                return response;
            }

            response.Error = $"Failed to delete project '{project.Code}' from global Projects db";
            return response;
        }

        public ApiResponse GetProjectStarterKit(string projectId, string folderRoot)
        {
            var response = new ApiResponse();

            var project = projectsMongoDbService.GetProjectById(projectId);
            if (project == null)
            {
                response.Error = $"Project id '{projectId}' not found";
                return response;
            }

            var model = new ProjectStarterKitModel
            {
                Code = project.Code,
                StarterKitUsed = project.StarterKit?.Replace(".usync", "").Replace("-", " ").Replace("_", " "),
                CanCreateFromStarterKit = !ProjectContentRootNodeExists(project.Code)
            };

            if (model.CanCreateFromStarterKit)
            {
                var starterKitsResponse = GetStarterKits(folderRoot);
                if (starterKitsResponse.Success)
                {
                    model.StarterKits = (Dictionary<string, string>)starterKitsResponse.ReturnValue;
                }

                if (!model.StarterKits.Any())
                {
                    response.Success = false;
                    response.Error = starterKitsResponse.Error;
                    return response;
                }
            }

            response.Success = true;
            response.ReturnValue = model;
            return response;
        }

        public ApiResponse GetRealmStatus(string projectId)
        {
            var project = projectsMongoDbService.GetProjectById(projectId);
            if (project == null)
            {
                return new ApiResponse
                {
                    Error = $"Project id '{projectId}' not found"
                };
            }

            var model = new RealmStatusModel
            {
                Id = projectId,
                Code = project.Code,
                PublishedAppName = project.MongoDbRealmPublishClientAppId,
                PreviewAppName = project.MongoDbRealmPreviewClientAppId,
            };

            if (!string.IsNullOrWhiteSpace(model.PublishedAppName) && !string.IsNullOrWhiteSpace(model.PreviewAppName))
            {
                model.RealmAppsCreated = true;
            }

            if (bool.TryParse(ConfigurationManager.AppSettings["Realm:Disable"], out var disableRealm) && disableRealm)
            {
                logger.Info<ProjectsSectionService>("Realm Apps disabled in configuration");
                model.RealmAppsDisabled = true;
            }

            return new ApiResponse
            {
                Success = true,
                ReturnValue = model
            };
        }

        public async Task<ApiResponse> CreateRealmAppsAsync(string projectId)
        {
            var response = new ApiResponse();

            if (bool.TryParse(ConfigurationManager.AppSettings["Realm:Disable"], out var disableRealm) && disableRealm)
            {
                response.Error = "Realm App creation disabled";
                return response;
            }

            var project = projectsMongoDbService.GetProjectById(projectId);
            if (project == null)
            {
                response.Error = $"Project id '{projectId}' not found";
                return response;
            }

            string previewAppName;
            string publishedAppName;

            try
            {
                previewAppName = await CreateRealmApp(project.Code, false);
                publishedAppName = await CreateRealmApp(project.Code, true);
            }
            catch (Exception exc)
            {
                logger.Error<ProjectsSectionService>(exc, exc.Message);
                return new ApiResponse { Success = false, Error = "Error creating Realm App" };
            }

            if (string.IsNullOrEmpty(previewAppName) || string.IsNullOrEmpty(publishedAppName))
            {
                return new ApiResponse { Success = false, Error = "Error creating Realm App" };
            }

            project.MongoDbRealmPreviewClientAppId = previewAppName;
            project.MongoDbRealmPublishClientAppId = publishedAppName;
            projectsMongoDbService.SaveProject(project);

            response.Success = true;
            return response;
        }

        public string GetProjectIdByCode(string projectCode)
        {
            return projectsMongoDbService.GetProjectByCode(projectCode)?.Id;
        }

        private async Task<string> CreateRealmApp(string diageoProjectCode, bool isPublished)
        {
            if (string.IsNullOrWhiteSpace(diageoProjectCode))
            {
                throw new ArgumentException("Project code is not set", "diageoProjectCode");
            }

            var adminApiCredentials = AtlasApiCredentials.FromConfig();
            var realmConfig = RealmConfig.FromConfig();

            var appName = isPublished ?
                            await realmAdminService.CreatePublishedApp(diageoProjectCode, realmConfig, adminApiCredentials) :
                            await realmAdminService.CreatePreviewApp(diageoProjectCode, realmConfig, adminApiCredentials);

            if (string.IsNullOrEmpty(appName))
            {
                throw new InvalidOperationException("Realm App is not set after creation. Unknown error");
            }

            return appName;
        }

        public async Task<ApiResponse> CreateRealmUserAsync(string projectId, bool publishedApplication, string userName = null)
        {
            var response = new ApiResponse();

            var project = projectsMongoDbService.GetProjectById(projectId);
            if (project == null)
            {
                response.Error = $"Project id '{projectId}' not found";
                return response;
            }

            var clientAppId = publishedApplication ? project.MongoDbRealmPublishClientAppId : project.MongoDbRealmPreviewClientAppId;


            var adminApiCredentials = AtlasApiCredentials.FromConfig();
            var realmConfig = RealmConfig.FromConfig();

            var app = await realmAdminService.GetAppDetailsByClientAppId(clientAppId, realmConfig, adminApiCredentials);
            if (app == null)
            {
                response.Error = $"App '{clientAppId}' not found";
                return response;
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "u" + DateTime.Now.Ticks.ToString();
            }
            var realmUser = await realmAdminService.CreateRealmUser(app.Id, userName, realmConfig, adminApiCredentials);


            var realmUserModel = new RealmUserModel
            {
                Id = projectId,
                Code = project.Code,
                UserId = realmUser.Id,
                ApiKey = realmUser.Key,
                UserFriendlyName = realmUser.Name,
                UserDisabled = realmUser.Disabled
            };

            response.Success = true;
            response.ReturnValue = realmUserModel;

            return response;
        }

        private bool SetProjectContentRootNode(ProjectModel dbProject)
        {
            // find root node whose name is this project
            var contentNode = contentService.GetRootContent().FirstOrDefault(x => x.Name == dbProject.Code);
            if (contentNode == null)
            {
                return false;
            }

            dbProject.ContentNodeId = contentNode.Id;
            dbProject.ContentNodeKey = contentNode.Key.ToString();
            return true;
        }

        private bool SetMediaContentRootNode(ProjectModel dbProject)
        {
            var mediaNode = mediaService.GetRootMedia().FirstOrDefault(x => x.Name == dbProject.Code);
            if (mediaNode == null)
            {
                return false;
            }

            dbProject.MediaNodeId = mediaNode.Id;
            dbProject.MediaNodeKey = mediaNode.Key.ToString();
            return true;
        }

        private IUserGroup CreateUserGroupIfNotExisting(ProjectModel dbProject)
        {
            // if group created manually before adding via back office, then just update project with details

            var userGroup = userService.GetUserGroupByAlias(dbProject.Code);
            if (userGroup == null)
            {
                // group not yet created: so create a new one using project code as both name and alias
                userGroup = new UserGroup
                {
                    Name = dbProject.Code,
                    Alias = dbProject.Code,
                    StartContentId = dbProject.ContentNodeId,
                    StartMediaId = dbProject.MediaNodeId
                };
                userService.Save(userGroup);
            }

            dbProject.UserGroupId = userGroup.Id;
            dbProject.UserGroupAlias = userGroup.Alias;
            return userGroup;
        }

        private bool ProjectContentRootNodeExists(string projectCode)
        {
            var contentNode = contentService.GetRootContent().FirstOrDefault(x => x.Name == projectCode);
            return contentNode != null;
        }

        private bool ProjectMediaRootNodeExists(string projectCode)
        {
            var mediaNode = mediaService.GetRootMedia().FirstOrDefault(x => x.Name == projectCode);
            return mediaNode != null;
        }

        private string GenerateNewPassword()
        {
            // generate new string password, with a special character in random position
            var passwordLength = 10;
            var password = Guid.NewGuid().ToString().Replace("-", "").Substring(0, passwordLength - 1);
            var specialOffset = (new Random()).Next(1, passwordLength);
            return password.Insert(specialOffset - 1, "%");
        }

        private bool SendEmailToNewProjectAdminUser(string projectCode, string name, string email, string password)
        {
            if (HttpContext.Current == null)
            {
                logger.Warn<ProjectsSectionService>("HttpContext is null");
                return false;
            }

            var subjectTemplatePath = HttpContext.Current.Server.MapPath("/EmailTemplates/NewProjectAdminUser/Subject.txt");
            var emailSubjectTemplate = System.IO.File.ReadAllText(subjectTemplatePath);

            var bodyTemplatePath = HttpContext.Current.Server.MapPath("/EmailTemplates/NewProjectAdminUser/Body.txt");
            var emailBodyTemplate = System.IO.File.ReadAllText(bodyTemplatePath);

            // add dynamic content to email templates
            emailSubjectTemplate = emailSubjectTemplate.Replace("%project-code%", projectCode);
            emailBodyTemplate = emailBodyTemplate.Replace("%project-code%", projectCode);
            emailBodyTemplate = emailBodyTemplate.Replace("%user-name%", name);
            emailBodyTemplate = emailBodyTemplate.Replace("%user-password%", password);

            var loginUrl = ConfigurationManager.AppSettings["Website.BackofficeLoginUrl"];
            if (string.IsNullOrWhiteSpace(loginUrl))
            {
                // set the login url for the email to be the current domain appended with /umbraco
                loginUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/umbraco";
            }
            emailBodyTemplate = emailBodyTemplate.Replace("%login-link%", loginUrl);

            using (var smtpClient = new SmtpClient())
            {
                var mailMessage = new MailMessage
                {
                    Subject = emailSubjectTemplate,
                    Body = emailBodyTemplate,
                    IsBodyHtml = false,
                };
                try
                {
                    mailMessage.To.Add(email);
                    smtpClient.Send(mailMessage);
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error<ProjectsSectionService>(ex, "Failed to send new project admin email to {email}", email);
                    return false;
                }
            }
        }
    }
}
