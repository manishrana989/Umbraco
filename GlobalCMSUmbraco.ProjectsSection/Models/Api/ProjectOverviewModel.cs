namespace GlobalCMSUmbraco.ProjectsSection.Models.Api
{
    public class ProjectOverviewModel : BaseProjectModel
    {

        /// <summary>
        /// Display name for the project
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// MongoDb database name for published items
        /// </summary>
        public string MongoDbPublish { get; set; }

        /// <summary>
        /// MongoDb database name for preview (saved, not published) items
        /// </summary>
        public string MongoDbPreview { get; set; }

        /// <summary>
        /// MongoDb Realm App name for published items
        /// </summary>
        public string MongoDbRealmPublishApp { get; set; }

        /// <summary>
        /// MongoDb Realm App name for preview (saved, not published) items
        /// </summary>
        public string MongoDbRealmPreviewApp { get; set; }

        /// <summary>
        /// Url of the Content node in the back-office
        /// </summary>
        public string ContentNodeUrl { get; set; }

        /// <summary>
        /// Url of the Media node in the back-office
        /// </summary>
        public string MediaNodeUrl { get; set; }

        /// <summary>
        /// Url of the User Group in the back-office
        /// </summary>
        public string UserGroupUrl { get; set; }

        /// <summary>
        /// Url of the front-end website. Should be the root domain with scheme, e.g. https://domain.com
        /// </summary>
        public string FrontEndUrl { get; set; }

        /// <summary>
        /// Url of the front-end config file e.g. api/config.json
        /// </summary>
        public string FeConfigFile { get; set; }

        /// <summary>
        /// Can this project be deleted?
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// Does this project have any users?
        /// </summary>
        public bool HasProjectUsers { get; set; }
    }
}
