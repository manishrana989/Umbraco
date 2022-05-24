using DCHMediaPicker.Core.Models;
using DCHMediaPicker.Core.Services.Interfaces;
using DCHMediaPicker.Data.Models;
using DCHMediaPicker.Data.Repositories.Interfaces;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Web;
using Umbraco.Core.Logging;
using System;

namespace DCHMediaPicker.Core.Services
{
    public class MediaItemTrackingService : IMediaItemTrackingService
    {
        private readonly ITrackedMediaItemRepository _trackingRepository;
        private readonly IUmbracoContextFactory _umbracoContext;
        private readonly ILogger _logger;
        private readonly int _itemsPerPage;
        public MediaItemTrackingService(ITrackedMediaItemRepository trackingRepository, IUmbracoContextFactory umbracoContext, ILogger logger)
        {
            _trackingRepository = trackingRepository;
            _itemsPerPage = int.Parse(Helper.GetAppSetting(Constants.ItemsPerPage));
            _umbracoContext = umbracoContext;
            _logger = logger;
        }

        public void TrackMediaItems(IEnumerable<MediaItem> mediaItems, int nodeId, int userId, int projectId)
        {
            _trackingRepository.ClearNode(nodeId);

            foreach (var mediaItem in mediaItems)
            {
                _trackingRepository.Create(new TrackedMediaItem()
                {
                    UserId = userId,
                    NodeId = nodeId,
                    Title = mediaItem.Title,
                    Url = mediaItem.Url,
                    MediaId = mediaItem.Id,
                    Expiry = mediaItem.Expiry,
                    FileName = mediaItem.FileName,
                    ProjectId = projectId,
                    Thumbnail = mediaItem.Thumbnail
                });
            }
        }

        public DashboardResponse GetTrackedMediaItems(int page, int projectId, bool showExpired)
        {
            var dashboardResponse = new DashboardResponse()
            {
                Items = _trackingRepository.Get(_itemsPerPage, Helper.GetSkipAmount(page, _itemsPerPage), projectId, showExpired).Select(x => new TrackedItem()
                {
                    UserId = x.UserId,
                    NodeId = x.NodeId,
                    Title = x.Title,
                    Url = x.Url,
                    Thumbnail = x.Thumbnail,
                    MediaId = x.MediaId,
                    Expiry = x.Expiry,
                    FileName = x.FileName
                }).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(_trackingRepository.GetCount(projectId, showExpired) / (double)_itemsPerPage)
            };

            if (dashboardResponse.Items != null && dashboardResponse.Items.Any())
            {
                foreach (var mediaItem in dashboardResponse.Items)
                {
                    using (var ensuredContext = _umbracoContext.EnsureUmbracoContext())
                    {
                        try
                        {
                            var contentCache = ensuredContext.UmbracoContext.Content;
                            var contentItem = contentCache.GetById(mediaItem.NodeId);
                            mediaItem.PageUrl = contentItem.Url();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error<MediaItemTrackingService>(ex, "Unable to get media item content id: {nodeId}", mediaItem.NodeId);
                        }
                    }
                }
            }

            return dashboardResponse;
        }
    }
}