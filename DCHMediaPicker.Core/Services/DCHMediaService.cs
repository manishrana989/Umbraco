using DCHMediaPicker.Core.Factories;
using DCHMediaPicker.Core.Models;
using DCHMediaPicker.Core.Services.Interfaces;
using DCHMediaPicker.Data.Repositories.Interfaces;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Web;
using static DCHMediaPicker.Data.Models.DCHSearchRequest;

namespace DCHMediaPicker.Core.Services
{
    public class DCHMediaService : IDCHMediaService
    {
        private readonly IDCHMediaRepository _mediaRepository;
        private readonly int _itemsPerPage;
        private readonly IList<DCHFilter> _defaultFilters;
        private readonly IUmbracoContextFactory _umbracoContext;
        private readonly IProjectsMongoDbService _projectsMongoDbService;
        public DCHMediaService(IDCHMediaRepository mediaRepository, IUmbracoContextFactory umbracoContext, IProjectsMongoDbService projectsMongoDbService)
        {
            _mediaRepository = mediaRepository;
            _itemsPerPage = int.Parse(Helper.GetAppSetting(Constants.ItemsPerPage));
            _defaultFilters = JsonConvert.DeserializeObject<IList<DCHFilter>>(Helper.GetAppSetting(Constants.DefaultFilters));
            _umbracoContext = umbracoContext;
            _projectsMongoDbService = projectsMongoDbService;
        }

        public async Task<SearchResults> AdvancedSearchAsync(AdvancedSearchRequest searchRequest)
        {
            CheckOverrideSettings(searchRequest.NodeId);
            var request = new DCHSearchRequestFactory().Create(searchRequest, _itemsPerPage, _defaultFilters);
            var results = await _mediaRepository.SearchAsync(request);
            return new SearchResults()
            {
                Items = results.Items?.Select(x => new MediaItem()
                {
                    Id = x.Id,
                    Title = x.Properties?.Title,
                    Url = x.Renditions.Thumbnail?.First()?.Href ?? "",
                    FileName = x.Properties?.FileName,
                    Expiry = GetExpiry(x.Relations)
                }).ToList() ?? new List<MediaItem>(),
                CurrentPage = searchRequest.Page,
                TotalPages = (int)Math.Ceiling(results.TotalItems / (double)_itemsPerPage)
            };
        }

        public async Task<SearchResults> BasicSearchAsync(string q, int nodeId, int page)
        {
            CheckOverrideSettings(nodeId);
            var regex = new Regex(@"^\d{7}$");
            var match = regex.Match(q);

            if (match.Success)
            {
                var asset = await _mediaRepository.GetAssetByIdAsync(q);
                var results = new SearchResults()
                {
                    Items = new List<MediaItem>(),
                    CurrentPage = page,
                    TotalPages = 1
                };

                if (asset != null)
                {
                    results.Items.Add(new MediaItem()
                    {
                        Id = asset.Id,
                        Title = asset.Properties.Title,
                        Url = asset.Renditions.Thumbnail?.First()?.Href ?? "",
                        FileName = asset.Properties?.FileName,
                        Expiry = GetExpiry(asset.Relations)
                    });
                }

                return results;
            }
            else
            {
                var request = new DCHSearchRequestFactory().Create(q, page, _itemsPerPage, _defaultFilters);
                var results = await _mediaRepository.SearchAsync(request);
                return new SearchResults()
                {
                    Items = results.Items?.Select(x => new MediaItem()
                    {
                        Id = x.Id,
                        Title = x.Properties.Title,
                        Url = x.Renditions.Thumbnail?.First()?.Href ?? "",
                        FileName = x.Properties?.FileName,
                        Expiry = GetExpiry(x.Relations)
                    }).ToList() ?? new List<MediaItem>(),
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(results.TotalItems / (double)_itemsPerPage)
                };
            }
        }

        public async Task<PublicLinks> GetPublicLinksAsync(int id, int nodeId)
        {
            CheckOverrideSettings(nodeId);
            var publicLinks = await _mediaRepository.GetPublicLinksAsync(id);
            var returnValue = new PublicLinks()
            {
                Url = GetProperty(publicLinks.Properties, "OriginalAssetURL"),
                Thumbnail = GetProperty(publicLinks.Properties, "ThumbnailURL")
            };

            if (string.IsNullOrEmpty(returnValue.Thumbnail) && GetProperty(publicLinks.Properties, "MIMEType").Equals("video/mp4"))
            {
                // Asset is video with no thumbnail, use default icon
                returnValue.Thumbnail = "/App_Plugins/DCHMediaPicker/img/video-128x128.png";
            }

            return returnValue;
        }

        private DateTime? GetExpiry(JObject data)
        {
            var expiry = data?.SelectToken("['DRM.RightsProfile.RightsProfileToAsset']")?.SelectToken("parents[0].properties")?.SelectToken("['DRM.RightsProfile.CalculatedExpirationDate']");
            return (DateTime?)expiry;
        }

        private string GetProperty(JObject data, string propertyName)
        {
            var property = data?.SelectToken($"['{propertyName}']");
            return (string)property;
        }

        private void CheckOverrideSettings(int nodeId)
        {
            using (var ensuredContext = _umbracoContext.EnsureUmbracoContext())
            {
                var contentCache = ensuredContext.UmbracoContext.Content;
                var contentItem = contentCache.GetById(nodeId);
                var projectNode = contentItem.Ancestor(Helper.GetAppSetting(Constants.ProjectNodeAlias));

                if (projectNode != null)
                {
                    var project = _projectsMongoDbService.GetProjectByContentNodeId(projectNode.Id);
                    if (project.DchOverrideSettings)
                    {
                        _mediaRepository.OverrideSettings(project.DchEndpointSecret, project.DchClientId, project.DchClientSecret);
                    }
                }
            }
        }
    }
}