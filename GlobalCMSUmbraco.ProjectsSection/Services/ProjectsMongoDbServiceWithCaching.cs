using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.ProjectDb;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Cache;
using Umbraco.Core.Models.Membership;

namespace GlobalCMSUmbraco.ProjectsSection.Services
{
    public class ProjectsMongoDbServiceWithCaching : IProjectsMongoDbService
    {
        private const string CachePrefix = "CachedProjectsMongoDbService";

        private readonly IProjectsMongoDbService uncachedService;
        private readonly IAppPolicyCache appCachePolicy;


        public ProjectsMongoDbServiceWithCaching(IProjectsMongoDbService uncachedService, AppCaches appCaches)
        {
            this.uncachedService = uncachedService ?? throw new ArgumentNullException(nameof(uncachedService));
            appCachePolicy = appCaches.RuntimeCache;
        }

        public bool AddProject(ProjectModel project, out string failReason)
        {
            var rtn = uncachedService.AddProject(project, out failReason);

            InvalidateCache();

            return rtn;
        }

        public bool DeleteProjectFromProjectsList(string id)
        {
            var rtn = uncachedService.DeleteProjectFromProjectsList(id);

            InvalidateCache();

            return rtn;
        }

        public void DeleteProjectMongoDbDatabases(string publishDbName, string previewDbName)
        {
            uncachedService.DeleteProjectMongoDbDatabases(publishDbName, previewDbName);

            InvalidateCache();
        }

        public void SaveParametersForProject(string dbName, ParametersModel parameters)
        {
            uncachedService.SaveParametersForProject(dbName, parameters);

            InvalidateCache();
        }

        public bool SaveProject(ProjectModel project)
        {
            var rtn = uncachedService.SaveProject(project);

            InvalidateCache();

            return rtn;
        }

        public void CreateMongoDbCollectionIfNotExists(string dbName, string collectionName)
        {
            var cacheKey = GetCacheKey(nameof(CreateMongoDbCollectionIfNotExists), $"d{dbName}", $"k{collectionName}");

            // If key exists then this has been called
            if (appCachePolicy.SearchByKey(cacheKey).Any())
            {
                return;
            }

            uncachedService.CreateMongoDbCollectionIfNotExists(dbName, collectionName);

            // GetProjectsList is now invalid
            InvalidateCache();

            appCachePolicy.Insert(cacheKey, () => true);
        }

        #region Cached Get methods

        public ParametersModel GetParametersForProject(string dbName)
        {
            var cacheKey = GetCacheKey(nameof(GetParametersForProject), $"d{dbName}");

            return appCachePolicy.GetCacheItem(cacheKey, () => uncachedService.GetParametersForProject(dbName));
        }

        public ProjectModel GetProjectByCode(string projectCode)
        {
            var cacheKey = GetCacheKey(nameof(GetProjectByCode), $"c{projectCode}");

            return appCachePolicy.GetCacheItem(cacheKey, () => uncachedService.GetProjectByCode(projectCode));
        }

        public ProjectModel GetProjectById(string projectId)
        {
            var cacheKey = GetCacheKey(nameof(GetProjectById), $"i{projectId}");

            return appCachePolicy.GetCacheItem(cacheKey, () => uncachedService.GetProjectById(projectId));
        }

        public ProjectModel GetProjectByContentNodeId(int contentId)
        {
            var cacheKey = GetCacheKey(nameof(GetProjectByContentNodeId), $"n{contentId}");

            return appCachePolicy.GetCacheItem(cacheKey, () => uncachedService.GetProjectByContentNodeId(contentId));
        }

        public ProjectModel GetProjectByMediaNodeId(int mediaId)
        {
            var cacheKey = GetCacheKey(nameof(GetProjectByMediaNodeId), $"m{mediaId}");

            return appCachePolicy.GetCacheItem(cacheKey, () => uncachedService.GetProjectByMediaNodeId(mediaId));
        }

        public List<ProjectModel> GetProjectsList()
        {
            var cacheKey = GetCacheKey(nameof(GetProjectsList));

            return appCachePolicy.GetCacheItem(cacheKey, () => uncachedService.GetProjectsList());
        }

        public List<ProjectModel> GetProjectsList(IUser currentUser)
        {
            var cacheKey = GetCacheKey(nameof(GetProjectsList), $"u{currentUser.Id}");

            return appCachePolicy.GetCacheItem(cacheKey, () => uncachedService.GetProjectsList(currentUser));
        }

        #endregion

        #region Cache helpers
        /// <summary>
        /// Invalidates the entire cache. We can't be more clever because doing so would make assumptions on the behaviour of the uncached version
        /// </summary>
        private void InvalidateCache()
        {
            appCachePolicy.ClearByKey(GetCacheKey());
        }

        private static string GetCacheKey(params string[] subKeys)
        {
            return $"{CachePrefix}_{string.Join("_", subKeys)}";
        }
        #endregion
    }
}
