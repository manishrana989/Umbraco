using DCHMediaPicker.Data.Models;
using System.Threading.Tasks;

namespace DCHMediaPicker.Data.Repositories.Interfaces
{
    public interface IDCHMediaRepository
    {
        Task<DCHResponse> SearchAsync(DCHSearchRequest searchRequest);
        Task<DCHPublicLinks> GetPublicLinksAsync(int id);
        Task<DCHMediaItem> GetAssetByIdAsync(string id);
        void OverrideSettings(string endpoint, string clientId, string clientSecret);
    }
}