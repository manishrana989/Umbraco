namespace GlobalCMSUmbraco.ProjectsSection.Models.Api
{
    public class RealmStatusModel : BaseProjectModel
    {
        public string PublishedAppName { get; internal set; }
        public string PreviewAppName { get; internal set; }
        public bool RealmAppsCreated { get; internal set; }
        public bool RealmAppsDisabled { get; internal set; }
    }
}
