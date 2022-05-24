using System.Collections.Generic;
using GlobalCMSUmbraco.ProjectsSection.Models.Api;
using System.Threading.Tasks;
using Umbraco.Core.Models.Membership;

namespace GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces
{
    public interface IProjectsSectionService
    {
        ApiResponse ConfirmConnectionToMongoDb();

        ApiResponse GetProjectsList(IUser currentUser);

        ApiResponse AddProject(AddProjectModel apiProject, IUser currentUser);

        ApiResponse GetStarterKits(string folderRoot);
        Dictionary<string, string> GetStarterKitsForExternalApi(string folderRoot, string starterKitDoesNotContain);

        ApiResponse ContentImported(string projectCode, string selectedSyncPackFile, bool createUserGroup);

        ApiResponse CreatePermissions(string projectCode, string userName, string userEmail);

        ApiResponse GetProjectOverview(string projectId);

        ApiResponse GetProjectSettings(string projectId);

        ApiResponse GetRealmStatus(string projectId);

        Task<ApiResponse> CreateRealmAppsAsync(string projectId);

        Task<ApiResponse> CreateRealmUserAsync(string projectId, bool publishedApplication, string userName = null);

        ApiResponse SaveProjectSettings(string projectId, ProjectSettingsModel settings);

        ApiResponse GetProjectStarterKit(string projectId, string folderRoot);

        ApiResponse DeleteProject(string projectId);

        string GetProjectIdByCode(string projectCode);
    }
}
