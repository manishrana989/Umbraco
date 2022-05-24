using DCHMediaPicker.Data.Models;
using DCHMediaPicker.Data.Models.Interfaces;
using DCHMediaPicker.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Scoping;

namespace DCHMediaPicker.Data.Repositories
{
    public class TrackedMediaItemRepository : ITrackedMediaItemRepository
    {
        private readonly IScopeProvider _scopeProvider;

        public TrackedMediaItemRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Create(TrackedMediaItem mediaItem)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                scope.Database.Insert(mediaItem);
            }
        }

        public void ClearNode(int nodeId)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                scope.Database.DeleteMany<TrackedMediaItem>().Where(x => x.NodeId == nodeId).Execute();
            }
        }

        public List<TrackedMediaItem> Get(int limit, int skip, int projectId, bool showExpired)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                if (showExpired)
                {
                    return scope.Database.Fetch<TrackedMediaItem>().Where(x => x.ProjectId == projectId && x.Expiry < DateTime.Now).OrderBy(x => x.Expiry).Skip(skip).Take(limit).ToList();
                }
                else
                {
                    return scope.Database.Fetch<TrackedMediaItem>().Where(x => x.ProjectId == projectId && x.Expiry > DateTime.Now).OrderBy(x => x.Expiry).Skip(skip).Take(limit).ToList();
                }
            }
        }

        public long GetCount(int projectId, bool showExpired)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                if (showExpired)
                {
                    return scope.Database.Fetch<TrackedMediaItem>().Where(x => x.ProjectId == projectId && x.Expiry < DateTime.Now).Count();
                }
                else
                {
                    return scope.Database.Fetch<TrackedMediaItem>().Where(x => x.ProjectId == projectId && x.Expiry > DateTime.Now).Count();
                }
            }
        }

        public List<TrackedMediaItem> GetExpiryItems(DateTime expiryCutoff)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<TrackedMediaItem>().Where(x => x.Expiry < expiryCutoff && !x.ReminderSent).ToList();
            }
        }

        public void Update(TrackedMediaItem mediaItem)
        {
            using (var scope = _scopeProvider.CreateScope(autoComplete: true))
            {
                scope.Database.Update(mediaItem);
            }
        }
    }
}