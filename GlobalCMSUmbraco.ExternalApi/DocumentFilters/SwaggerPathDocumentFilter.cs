using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;
using GlobalCMSUmbraco.ExternalApi.Attributes;
using Swashbuckle.Swagger;
using Umbraco.Core;

namespace GlobalCMSUmbraco.ExternalApi.DocumentFilters
{
    public class SwaggerPathDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            var validApis = GetApisWithAttribute(apiExplorer);
            swaggerDoc.paths
                .RemoveAll(pair => !validApis.Any(api => pair.Key.StartsWith($"/{api.RelativePathSansQueryString()}")));
        }

        private static IEnumerable<ApiDescription> GetApisWithAttribute(IApiExplorer apiExplorer)
        {
            return apiExplorer.ApiDescriptions.Where(a => a.GetControllerAndActionAttributes<ShowInSwaggerAttribute>().Any());
        }
    }
}
