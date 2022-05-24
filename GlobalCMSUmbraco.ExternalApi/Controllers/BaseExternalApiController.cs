using System.Configuration;
using System.Net;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Cors;
using Umbraco.Web.WebApi;

namespace GlobalCMSUmbraco.ExternalApi.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class BaseExternalApiController : UmbracoApiController
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            var isEnabled = bool.TryParse(ConfigurationManager.AppSettings["GlobalCMS.EnableExternalApi"], out var val) && val;
            if (!isEnabled)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            base.Initialize(controllerContext);
        }
    }
}
