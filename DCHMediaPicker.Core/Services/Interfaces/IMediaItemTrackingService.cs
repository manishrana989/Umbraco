using DCHMediaPicker.Core.Models;
using System.Collections.Generic;

namespace DCHMediaPicker.Core.Services.Interfaces
{
    public interface IMediaItemTrackingService
    {
        void TrackMediaItems(IEnumerable<MediaItem> mediaItems, int nodeId, int userId, int projectId);

        DashboardResponse GetTrackedMediaItems(int page, int projectId, bool showExpired);
    }
}