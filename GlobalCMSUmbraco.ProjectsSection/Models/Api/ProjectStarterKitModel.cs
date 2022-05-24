using System.Collections.Generic;

namespace GlobalCMSUmbraco.ProjectsSection.Models.Api
{
    public class ProjectStarterKitModel : AddProjectModel
    {
        /// <summary>
        /// Name of the starter kit (display name of sync pack file) that project was cloned from, if any
        /// </summary>
        public string StarterKitUsed { get; set; }

        /// <summary>
        /// If project already has a content node then this will be false: can only create from a starter kit if root content node not yet in content tree
        /// </summary>
        public bool CanCreateFromStarterKit { get; set; }

        public Dictionary<string,string> StarterKits { get; set; } = new Dictionary<string, string>();
    }
}
