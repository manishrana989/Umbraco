using DCHMediaPicker.Data.Models;
using System;
using System.Collections.Generic;

namespace DCHMediaPicker.Data.Repositories.Interfaces
{
    public interface ITrackedMediaItemRepository
    {
        void Create(TrackedMediaItem mediaItem);

        void ClearNode(int nodeId);

        List<TrackedMediaItem> Get(int limit, int skip, int projectId, bool showExpired);

        long GetCount(int projectId, bool showExpired);

        List<TrackedMediaItem> GetExpiryItems(DateTime expiryCutoff);

        void Update(TrackedMediaItem mediaItem);
    }
}
