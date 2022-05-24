using uSync.Exporter;

namespace GlobalCMSUmbraco.ProjectsSection.Models.Api
{
    public class AddProjectModel : BaseProjectModel
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
        /// Name of the sync pack file selected during App Project process from which to clone
        /// </summary>
        public string SelectedSyncPackFile { get; set; }

        /// <summary>
        /// Name of the cloned sync pack created during Add Project process
        /// </summary>
        public string ClonedSyncPackFile { get; set; }

        /// <summary>
        /// Details for passing through the Usync Export request
        /// </summary>
        public ExporterRequest USyncRequest { get; set; }
    }
}
