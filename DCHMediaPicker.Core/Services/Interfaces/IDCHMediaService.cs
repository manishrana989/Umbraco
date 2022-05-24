using DCHMediaPicker.Core.Models;
using System.Threading.Tasks;

namespace DCHMediaPicker.Core.Services.Interfaces
{
    public interface IDCHMediaService
    {
        Task<SearchResults> BasicSearchAsync(string q, int nodeId, int page);
        Task<SearchResults> AdvancedSearchAsync(AdvancedSearchRequest searchRequest);
        Task<PublicLinks> GetPublicLinksAsync(int id, int nodeId);
    }
}