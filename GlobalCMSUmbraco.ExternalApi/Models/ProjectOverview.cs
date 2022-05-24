using GlobalCMSUmbraco.ProjectsSection.Models.Api;

namespace GlobalCMSUmbraco.ExternalApi.Models
{
    public class ProjectOverview
    {
        /// <summary>
        /// Display name for the project
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Can this project be deleted?
        /// </summary>
        public bool CanBeDeleted { get; set; }

        /// <summary>
        /// Does this project have Realm Apps?
        /// </summary>
        public bool HasRealmApps { get; set; }

        /// <summary>
        /// MongoDb Realm App name for published items
        /// </summary>
        public string MongoDbRealmPublishApp { get; set; }

        /// <summary>
        /// MongoDb Realm App name for preview (saved, not published) items
        /// </summary>
        public string MongoDbRealmPreviewApp { get; set; }

        /// <summary>
        /// True if this project already has some content (starter kits then not allowed to be used)
        /// </summary>
        public bool HasContent { get; set; }

        /// <summary>
        /// True if this project already has a user group configured
        /// </summary>
        public bool HasPermissions { get; set; }

        /// <summary>
        /// Url of the front-end website. Should be the root domain with scheme, e.g. https://domain.com
        /// </summary>
        public string FrontEndUrl { get; set; }

        /// <summary>
        /// Url of the front-end config file e.g. api/config.json
        /// </summary>
        public string FeConfigFile { get; set; }

        /// <summary>
        /// Copy method
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ProjectOverview FromProjectOverviewModel(ProjectOverviewModel source)
        {
            var destination = new ProjectOverview
            {
                CanBeDeleted = source.CanDelete,
                DisplayName = source.DisplayName,
                FrontEndUrl = source.FrontEndUrl,
                FeConfigFile = source.FeConfigFile,
                HasContent = !string.IsNullOrWhiteSpace(source.ContentNodeUrl),
                HasPermissions = source.HasProjectUsers,
                HasRealmApps = !string.IsNullOrWhiteSpace(source.MongoDbRealmPublishApp),
                MongoDbRealmPublishApp = source.MongoDbRealmPublishApp,
                MongoDbRealmPreviewApp = source.MongoDbRealmPreviewApp,
            };

            return destination;
        }
    }
}
