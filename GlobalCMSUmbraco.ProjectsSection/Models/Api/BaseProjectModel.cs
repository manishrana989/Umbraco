using GlobalCMSUmbraco.Common;

namespace GlobalCMSUmbraco.ProjectsSection.Models.Api
{
    public abstract class BaseProjectModel
    {
        /// <summary>
        /// Project unique id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Project code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Flag for whether currently running the 'Developer Edition' of the Global CMS in case needed client-side
        /// </summary>
        public bool IsDeveloperEdition => AppSettings.IsDeveloperEdition;
    }
}
