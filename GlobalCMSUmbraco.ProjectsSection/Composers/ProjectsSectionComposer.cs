using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using GlobalCMSUmbraco.ProjectsSection.Controllers;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.JavaScript;

namespace GlobalCMSUmbraco.ProjectsSection.Composers
{
    [RuntimeLevel(MinLevel = Umbraco.Core.RuntimeLevel.Run)]
    public class ProjectsSectionComposer : ComponentComposer<ProjectsSectionComponent>
    { }

    /// <summary>
    /// Component to add the URL of the controller APIs so don't have to be hardwired
    /// </summary>
    public class ProjectsSectionComponent : IComponent
    {
        private readonly ILogger logger;

        public ProjectsSectionComponent(ILogger logger)
        {
            this.logger = logger;
        }

        public void Initialize()
        {
            ServerVariablesParser.Parsing += ServerVariablesParser_Parsing;
        }

        private void ServerVariablesParser_Parsing(object sender, Dictionary<string, object> e)
        {
            if (HttpContext.Current == null)
            {
                logger.Warn<ProjectsSectionComponent>("HttpContext is null");
                return;
            }

            UrlHelper urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

            e["ProjectsSection"] = new Dictionary<string, object>()
            {
                { "ApiBaseUrl", urlHelper.GetUmbracoApiServiceBaseUrl<ProjectsSectionApiController>(controller => controller.GetApi()) }
            };
        }

        public void Terminate()
        {

        }
    }
}
