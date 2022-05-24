namespace GlobalCMSUmbraco.ProjectsSection.Models.Api
{
    public class RealmUserModel : BaseProjectModel
    {
        /// <summary>
        /// The Id of the user
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The API key (never retrievable after creation)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// A friendly name for the user
        /// </summary>
        public string UserFriendlyName { get; set; }

        /// <summary>
        /// If the user is diabled
        /// </summary>
        public bool UserDisabled { get; set; }
    }
}
