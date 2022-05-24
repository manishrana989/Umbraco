using System;
using MongoDB.Bson.Serialization.Attributes;

namespace GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb
{
    /// <summary>
    /// Class defining the structure of the GlobalCms database 'Projects' collection. For settings that are maintained by CMS and not visible to front-end.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ProjectModel
    {
        /// <summary>
        /// Id is a guid created by CMS when inserting the project, used as id in links in navigation (stored as a string)
        /// </summary>
        [BsonId]
        public string Id { get; set; }

        /// <summary>
        /// Project code
        /// </summary>
        public string Code { get; set; }

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
        /// Id of the starting Content node for this project
        /// </summary>
        public int? ContentNodeId { get; set; }

        /// <summary>
        /// Umbraco key of the starting Content node for this project (stored as a string)
        /// </summary>
        public string ContentNodeKey { get; set; }

        /// <summary>
        /// Id of the starting Media node for this project
        /// </summary>
        public int? MediaNodeId { get; set; }

        /// <summary>
        /// Umbraco key of the starting Media node for this project (stored as a string)
        /// </summary>
        public string MediaNodeKey { get; set; }

        /// <summary>
        /// Name of the starter kit from which this project was cloned
        /// </summary>
        public string StarterKit { get; set; }

        /// <summary>
        /// Id of the User Group that allows access to the Content and Media nodes for this project
        /// </summary>
        public int? UserGroupId { get; set; }

        /// <summary>
        /// Alias of the User Group that allows access to the Content and Media nodes for this project
        /// </summary>
        public string UserGroupAlias { get; set; }

        /// <summary>
        /// Timestamp for when the project was first added via the Projects Section
        /// </summary>
        public DateTime CreatedDateUtc { get; set; }

        /// <summary>
        /// Display name of the user that created the project via the Projects Section
        /// </summary>
        public string CreatedByName { get; set; }

        /// <summary>
        /// Id of the user that created the project via the Projects Section
        /// </summary>
        public int CreatedById { get; set; }

        /// <summary>
        /// Url of the front-end website. Should be the root domain with scheme, e.g. https://domain.com
        /// </summary>
        public string FrontEndUrl { get; set; }

        /// <summary>
        /// Url of the front-end config file e.g. api/config.json
        /// </summary>
        public string FeConfigFile { get; set; }
        

        /// <summary>
        /// Build Environment should be 'Fargate' or 'Alto'
        /// </summary>
        public string BuildEnvironment { get; set; }

        /// <summary>
        /// Purge Cache should be 'OnPublish' or 'OnSave'
        /// </summary>
        public string PurgeCache { get; set; }

        /// <summary>
        /// Maps API key to be used in the CMS for this project
        /// </summary>
        public string CmsMapsApiKey { get; set; }

        /// <summary>
        /// Number of minutes between approval reminder emails. Null or 0 means don't send reminders
        /// </summary>
        public int? ApprovalEmailReminderMins { get; set; }

        /// <summary>
        /// Used by DCH for filtering images
        /// </summary>
        public string BrandName { get; set; }

        /// <summary>
        /// The name of the Preview Realm App
        /// </summary>
        public string MongoDbRealmPreviewClientAppId { get; set; }

        /// <summary>
        /// The name of the Published Realm App
        /// </summary>
        public string MongoDbRealmPublishClientAppId { get; set; }

        /// <summary>
        /// Toggle to allow the DCH api endpoint, client id and client secret to be overridden from within the CMS
        /// </summary>
        public bool DchOverrideSettings { get; set; }

        /// <summary>
        /// Allow the DCH client id to be overridden from within the CMS
        /// </summary>
        public string DchClientId { get; set; }

        /// <summary>
        /// Allow the DCH client secret to be overridden from within the CMS
        /// </summary>
        public string DchClientSecret { get; set; }

        /// <summary>
        /// Allow the DCH endpoint to be overridden from within the CMS
        /// </summary>
        public string DchEndpointSecret { get; set; }
        
    }
}
