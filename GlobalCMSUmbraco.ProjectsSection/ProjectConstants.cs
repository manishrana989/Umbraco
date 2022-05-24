
namespace GlobalCMSUmbraco.ProjectsSection
{
    public static class ProjectConstants
    {
        /// <summary>
        /// Name of the MongoDb global database for managing projects
        /// </summary>
        public const string GlobalCmsMongoDbDatabase = "Global_CMS";

        /// <summary>
        /// Name of the MongoDb global database collection for managing projects
        /// </summary>
        public const string GlobalCmsProjectsCollection = "Projects";

        /// <summary>
        /// App Data folder location for starter kits
        /// </summary>
        public const string AppDataStarterKitsFolder = "StarterKitSyncPacks";

        /// <summary>
        /// App Data folder location for storing created clones
        /// </summary>
        public const string AppDataClonesFolder = "ClonedSyncPacks";

        /// <summary>
        /// Name of the MongoDb project database collection for Parameters (settings managed by CMS)
        /// </summary>
        public const string ProjectDbParametersCollection = "Params";

        /// <summary>
        /// Alias of the Project Administrators group
        /// </summary>
        public const string ProjectAdministratorUserGroupAlias = "projectAdmin";
    }
}
