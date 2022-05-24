using DCHMediaPicker.Core.Models;
using DCHMediaPicker.Core.Services.Interfaces;
using System.Collections.Generic;
using System.Web.Http;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;
using static DCHMediaPicker.Core.Models.CmsSettings;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb;
using System.Threading.Tasks;

namespace DCHMediaPicker.Web.Controllers
{
    [PluginController("DCHMediaPicker")]
    public class ApiController : UmbracoAuthorizedJsonController
    {
        private readonly IMediaItemTrackingService _trackingService;
        private readonly IDCHMediaService _mediaService;
        private readonly IExpiryService _expiryService;
        private readonly IUmbracoContextFactory _umbracoContext;
        private readonly IProjectsSectionService _projectsSectionService;
        public ApiController(IMediaItemTrackingService trackingService, IDCHMediaService mediaService, IExpiryService expiryService, IUmbracoContextFactory umbracoContext, IProjectsSectionService projectsSectionService)
        {
            _trackingService = trackingService;
            _mediaService = mediaService;
            _expiryService = expiryService;
            _umbracoContext = umbracoContext;
            _projectsSectionService = projectsSectionService;
        }

        [HttpGet]
        public async Task<SearchResults> BasicSearch(string q, int nodeId, int page = 1)
        {
            return await _mediaService.BasicSearchAsync(q, nodeId, page);
        }

        [HttpPost]
        public async Task<SearchResults> AdvancedSearch([FromBody]AdvancedSearchRequest searchRequest)
        {
            return await _mediaService.AdvancedSearchAsync(searchRequest);
        }

        [HttpGet]
        public DashboardResponse Dashboard(int page, int projectId, bool showExpired)
        {
            return _trackingService.GetTrackedMediaItems(page, projectId, showExpired);
        }

        [HttpGet]
        public List<ExpiryEmailData> SendEmails()
        {
            return _expiryService.SendExpiryReminders();
        }

        [HttpGet]
        public List<Collection> Collections(int nodeId)
        {
            using (var ensuredContext = _umbracoContext.EnsureUmbracoContext())
            {
                var contentCache = ensuredContext.UmbracoContext.Content;
                var contentItem = contentCache.GetById(nodeId);
                var contentRoot = contentItem.Root();
                var projectList = _projectsSectionService.GetProjectsList(UmbracoContext.Security.CurrentUser);
                
                if (projectList.Success)
                {
                    var projects = (List<ProjectModel>)projectList.ReturnValue;
                    foreach (var project in projects)
                    {
                        if (project.ContentNodeId == contentRoot.Id)
                        {
                            var collections = new List<Collection>();
                            var searchString = project.BrandName;

                            collections.Add(new Collection()
                            {
                                Name = $"Search for '{searchString}'",
                                CollectionType = "Keywords",
                                SearchTerms = searchString
                            });

                            collections.Add(new Collection()
                            {
                                Name = $"Modified in last 30 days",
                                CollectionType = "Modified",
                                Keywords = searchString,
                                Days = 30
                            });

                            collections.Add(new Collection()
                            {
                                Name = $"Modified in last 7 days",
                                CollectionType = "Modified",
                                Keywords = searchString,
                                Days = 7
                            });

                            collections.Add(new Collection()
                            {
                                Name = $"Modified in last 24 hours",
                                CollectionType = "Modified",
                                Keywords = searchString,
                                Days = 1
                            });

                            return collections;
                        }
                    }
                }
            }

            return new List<Collection>();
        }

        [HttpGet]
        public List<Project> Projects()
        {
            var projects = new List<Project>();
            var projectList = _projectsSectionService.GetProjectsList(UmbracoContext.Security.CurrentUser);

            if (projectList.Success)
            {
                foreach (var project in (List<ProjectModel>)projectList.ReturnValue)
                {
                    if (project.ContentNodeId != null)
                    {
                        projects.Add(new Project()
                        {
                            Id = (int)project.ContentNodeId,
                            ProjectName = project.BrandName ?? project.Code
                        });
                    }
                }
            }

            return projects;
        }

        [HttpPost]
        public async Task<MediaItem> ResolveItem(MediaItem mediaItem, int nodeId)
        {
            PublicLinks publicLinks = await _mediaService.GetPublicLinksAsync(mediaItem.Id, nodeId);

            if (!string.IsNullOrEmpty(publicLinks.Url))
            {
                mediaItem.Thumbnail = publicLinks.Thumbnail;
                mediaItem.Url = publicLinks.Url;
                mediaItem.Resolved = true;
            }

            return mediaItem;
        }
    }
}