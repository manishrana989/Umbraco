using Hangfire.Dashboard;
using System.Linq;
using System.Web;
using Umbraco.Core.Composing;
using Umbraco.Web.Security;

namespace DCHMediaPicker.Core.Filters
{
    public class UmbracoAuthorisationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var auth = new HttpContextWrapper(HttpContext.Current).GetUmbracoAuthTicket();
            if (auth != null)
            {
                var backofficeUser = Current.Services.UserService.GetByUsername(auth.Identity.Name);
                return backofficeUser.Groups.FirstOrDefault(p => p.Alias == "admin") != null;
            }

            return false;
        }
    }
}