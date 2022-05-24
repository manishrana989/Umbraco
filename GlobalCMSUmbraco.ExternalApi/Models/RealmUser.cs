using GlobalCMSUmbraco.ProjectsSection.Models.Api;

namespace GlobalCMSUmbraco.ExternalApi.Models
{
    public class RealmUser
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

        /// <summary>
        /// Copy method
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static RealmUser FromRealmUserModel(RealmUserModel source)
        {
            var destination = new RealmUser
            {
                UserId = source.UserId,
                ApiKey = source.ApiKey,
                UserFriendlyName = source.UserFriendlyName,
                UserDisabled = source.UserDisabled,
            };

            return destination;
        }
    }
}
