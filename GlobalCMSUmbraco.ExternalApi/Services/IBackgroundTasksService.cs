
namespace GlobalCMSUmbraco.ExternalApi.Services
{
    public interface IBackgroundTasksService
    {
        void StartStarterKitClone(string projectCode, string starterKitId);
    }
}
