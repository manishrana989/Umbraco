using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using Umbraco.Core.Logging;

namespace GlobalCMSUmbraco.ExternalApi.Services
{
    public class BackgroundTasksService : IBackgroundTasksService
    {
        private readonly IProjectsSectionService projectsSectionService;
        private readonly ICloneSyncPackService cloneSyncPackService;
        private readonly ILogger logger;

        public BackgroundTasksService(IProjectsSectionService projectsSectionService, ICloneSyncPackService cloneSyncPackService, ILogger logger)
        {
            this.projectsSectionService = projectsSectionService;
            this.cloneSyncPackService = cloneSyncPackService;
            this.logger = logger;
        }

        public void StartStarterKitClone(string projectCode, string starterKitId)
        {
            // API controller will have already validated that projectId and starterKitId are valid to use before returning the Accepted response

            logger.Info<BackgroundTasksService>("StartStarterKitClone background job started, project: {code}", projectCode);

            if (cloneSyncPackService.CloneAndProcess(projectCode, starterKitId))
            {
                var response = projectsSectionService.ContentImported(projectCode, starterKitId, true);
                if (response.Success)
                {
                    logger.Info<BackgroundTasksService>("StartStarterKitClone background job completed, project: {code}", projectCode);
                    return;
                }

                logger.Info<BackgroundTasksService>("StartStarterKitClone background job aborted, project: {code}, reason: {reason}", projectCode, response.Error);
                return;
            }

            logger.Info<BackgroundTasksService>("StartStarterKitClone background job aborted, project: {code}, reason: import process failed", projectCode);
        }
    }
}
