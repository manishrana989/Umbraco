using GlobalCMSUmbraco.MongoDbRealmAdmin;
using GlobalCMSUmbraco.MongoDbRealmAdmin.Models;
using System.Threading.Tasks;

namespace GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces
{
    public interface IRealmAdminService
    {
        Task<string> CreatePreviewApp(string diageoProjectCode, IRealmConfig realmConfig, IAtlasApiCredentials apiCredentials);
        Task<string> CreatePublishedApp(string diageoProjectCode, IRealmConfig realmConfig, IAtlasApiCredentials apiCredentials);

        Task<string> CreateApp(string diageoProjectCode, IRealmConfig realmConfig, IAtlasApiCredentials apiCredentials, bool isPreview);
        Task<IRealmAppDetails> GetAppDetailsByClientAppId(string clientAppId, IRealmConfig realmConfig, IAtlasApiCredentials adminApiCredentials);
        Task<IRealmUser> CreateRealmUser(string id, string userName, IRealmConfig realmConfig, IAtlasApiCredentials adminApiCredentials);
    }
}
