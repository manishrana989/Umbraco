using GlobalCMSUmbraco.ProjectsSection.Models.Api;

namespace GlobalCMSUmbraco.ExternalApi.Models
{
    public class ProjectSettings
    {
        /// <summary>
        /// Display name for the project
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Url of the front-end website. Should be the root domain with scheme, e.g. https://domain.com
        /// </summary>
        public string FrontEndUrl { get; set; }

        /// <summary>
        /// Url of the front-end config file e.g. api/config.json
        /// </summary>
        public string FeConfigFile { get; set; }

        /// <summary>
        /// Maps API key to be used in the CMS for this project
        /// </summary>
        public string CmsMapsApiKey { get; set; }

        /// <summary>
        /// Maps API key to be used in the front-end website
        /// </summary>
        public string FrontEndMapsApiKey { get; set; }

        /// <summary>
        /// Frequency of approval email reminders (minutes). Set to 0 to not send reminders
        /// </summary>
        public int ApprovalEmailReminderMins { get; set; }

        /// <summary>
        /// Used by DCH for filtering images
        /// </summary>
        public string BrandName { get; set; }

        /// <summary>
        /// Toggle to allow the DCH api endpoint, client id and client secret to be overridden from within the CMS
        /// </summary>
        public bool DchOverrideSettings { get; private set; }

        /// <summary>
        /// Allow the DCH client id to be overridden from within the CMS
        /// </summary>
        public string DchClientId { get; private set; }

        /// <summary>
        /// Allow the DCH client secret to be overridden from within the CMS
        /// </summary>
        public string DchClientSecret { get; private set; }

        /// <summary>
        /// Allow the DCH endpoint to be overridden from within the CMS
        /// </summary>
        public string DchEndpointSecret { get; private set; }

        public static ProjectSettings FromProjectSettingsModel(ProjectSettingsModel source)
        {
            var detination = new ProjectSettings
            {
                DisplayName = source.DisplayName,
                FrontEndUrl = source.FrontEndUrl,
                FeConfigFile = source.FeConfigFile,
                CmsMapsApiKey = source.CmsMapsApiKey,
                FrontEndMapsApiKey = source.FrontEndMapsApiKey,
                ApprovalEmailReminderMins = source.ApprovalEmailReminderMins,
                BrandName = source.BrandName,
                DchOverrideSettings = source.DchOverrideSettings,
                DchClientId = source.DchClientId,
                DchClientSecret = source.DchClientSecret,
                DchEndpointSecret = source.DchEndpointSecret,
            };

            return detination;
        }

        public ProjectSettingsModel GetProjectSettingsModel(string id, string code)
        {
            return new ProjectSettingsModel
            {
                Id = id,
                Code = code,
                ApprovalEmailReminderMins = ApprovalEmailReminderMins,
                BrandName = BrandName,
                CmsMapsApiKey = CmsMapsApiKey,
                DisplayName = DisplayName,
                FrontEndMapsApiKey = FrontEndMapsApiKey,
                FrontEndUrl = FrontEndUrl,
                FeConfigFile = FeConfigFile
            };
        }
    }
}
