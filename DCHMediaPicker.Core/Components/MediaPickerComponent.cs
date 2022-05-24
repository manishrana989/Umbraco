using DCHMediaPicker.Core.Models;
using DCHMediaPicker.Core.Services;
using DCHMediaPicker.Core.Services.Interfaces;
using DCHMediaPicker.Data.Models;
using DCHMediaPicker.Data.Models.Interfaces;
using DCHMediaPicker.Data.Repositories;
using DCHMediaPicker.Data.Repositories.Interfaces;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Configuration;
using DCHMediaPicker.Core.Dashboards;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Blocks;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;

namespace DCHMediaPicker.Core.Components
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class MediaPickerComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            if (ConfigurationManager.GetSection("dchMediaPicker") == null)
            {
                // current site not configured to work with the dchMediaPicker
                // means DevEdition.Site can reference MongoDbPublisher project for publishing to mongodb
                // but without needing to support local use of the DCH Media Picker (for now, at least)
                composition.Dashboards().Remove<MediaPickerDashboard>();
                return;
            }

            composition.Components().Append<MediaPickerComponent>();
            composition.Register<IMediaItemTrackingService, MediaItemTrackingService>();
            composition.Register<ITrackedMediaItemRepository, TrackedMediaItemRepository>();
            composition.Register<IDCHMediaService, DCHMediaService>();
            composition.Register<IDCHMediaRepository, DCHMediaRepository>();
            composition.Register<IApiSettings>(x => new ApiSettings()
            {
                Endpoint = Helper.GetAppSetting(Constants.Endpoint),
                ClientId = Helper.GetAppSetting(Constants.ClientId),
                ClientSecret = Helper.GetAppSetting(Constants.ClientSecret),
                CorrelationId = Helper.GetAppSetting(Constants.CorrelationId),
                UserAgent = Helper.GetAppSetting(Constants.UserAgent)
            });
            composition.Register<IExpiryService, ExpiryService>(Lifetime.Singleton);
        }
    }

    public class MediaPickerComponent : IComponent
    {
        private readonly IMediaItemTrackingService _trackingService;
        private readonly ILogger _logger;
        private readonly IUmbracoContextFactory _umbracoContext;

        public MediaPickerComponent(ILogger logger, IMediaItemTrackingService trackingService, IUmbracoContextFactory umbracoContext)
        {
            _logger = logger;
            _trackingService = trackingService;
            _umbracoContext = umbracoContext;
        }

        public void Initialize()
        {
            ContentService.Published += ContentService_Published;
            ContentService.Unpublished += ContentService_Unpublished;

            RecurringJob.AddOrUpdate<IExpiryService>("ExpiryService", x => x.SendExpiryReminders(), Helper.GetAppSetting(Constants.RemindersCron));
        }

        public void Terminate()
        {
            ContentService.Published -= ContentService_Published;
            ContentService.Unpublished -= ContentService_Unpublished;
        }

        private void ContentService_Published(Umbraco.Core.Services.IContentService sender, Umbraco.Core.Events.ContentPublishedEventArgs e)
        {
            foreach (IContent publishedItem in e.PublishedEntities)
            {
                using (UmbracoContextReference ensuredContext = _umbracoContext.EnsureUmbracoContext())
                {
                    Umbraco.Web.PublishedCache.IPublishedContentCache contentCache = ensuredContext.UmbracoContext.Content;
                    IPublishedContent contentItem = contentCache.GetById(publishedItem.Id);

                    if (contentItem != null)
                    {
                        int projectId = contentItem.Root().Id;
                        List<MediaItem> mediaItemsToTrack = FindMediaItems(contentItem);

                        try
                        {
                            _trackingService.TrackMediaItems(mediaItemsToTrack, publishedItem.Id, publishedItem.WriterId, projectId);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error<MediaPickerComponent>(ex, "DCH error publishing content");
                        }
                    }
                    else
                    {
                        _logger.Error<MediaPickerComponent>("Unable to find project ID for node id {publishedItem}", publishedItem.Id);
                    }
                }
            }
        }

        private void ContentService_Unpublished(Umbraco.Core.Services.IContentService sender, Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            foreach (IContent publishedItem in e.PublishedEntities)
            {
                try
                {
                    _trackingService.TrackMediaItems(new List<MediaItem>(), publishedItem.Id, 0, 0);
                }
                catch (Exception ex)
                {
                    _logger.Error<MediaPickerComponent>(ex, "DCH error unpublishing content");
                }
            }
        }

        private List<MediaItem> FindMediaItems(IPublishedElement element)
        {
            List<MediaItem> mediaItems = new List<MediaItem>();
            foreach (var property in element.Properties)
            {
                FindProperties(property, mediaItems);
            }

            return mediaItems;
        }

        private void FindPropertyElements(IPublishedElement element, List<MediaItem> mediaItems)
        {
            foreach (var property in element.Properties)
            {
                FindProperties(property, mediaItems);
            }
        }

        private void FindProperties(IPublishedProperty property, List<MediaItem> mediaItems)
        {
            try
            {
                var propertyValue = property.GetValue();

                if (propertyValue != null)
                {
                    if (propertyValue is IEnumerable<MediaItem> dchMediaItems)
                    {
                        mediaItems.AddRange(dchMediaItems);
                    }
                    else if (propertyValue is BlockListModel blockListModel)
                    {
                        foreach (var item in blockListModel)
                        {
                            FindPropertyElements(item.Content, mediaItems);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.Error<MediaPickerComponent>(exc, exc.Message);
            }
        }
    }
}