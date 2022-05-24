using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using GlobalCMSUmbraco.ExternalApi.TokenHandlers;
using Microsoft.IdentityModel.Tokens;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;

namespace GlobalCMSUmbraco.ExternalApi.Attributes
{
    public class JwtBearerTokenAuthorizationAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// When the attribute is decorated on an Umbraco WebApi Controller
        /// </summary>
        /// <param name="actionContext"></param>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                //Auth from the request (HTTP headers)
                if (!IsAuthenticated(actionContext.Request))
                {
                    //Return a HTTP 401 Unauthorised header
                    actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception)
            {
                //Return a HTTP 401 Unauthorised header
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            //Continue as normal
            base.OnActionExecuting(actionContext);
        }

        private static bool IsAuthenticated(HttpRequestMessage request)
        {
            //Try to get the Authorization header in the request
            var ah = request.Headers.Authorization;

            //If no Auth header sent or the scheme is not bearer aka TOKEN
            if (ah == null || ah.Scheme.ToLower() != "bearer")
            {
                return false;
            }

            //Get the JWT token from auth HTTP header param
            var jwtToken = ah.Parameter;

            var tokenHandler = new LoggingJwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            var principal = tokenHandler.ValidateToken(jwtToken, validationParameters, out _);
            return principal.Identity.IsAuthenticated;
        }

        private static TokenValidationParameters GetValidationParameters()
        {
            var secretKey = ApiConstants.SecretKey; // same key as the one that generate the token
            var issuer = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);

            return new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidAudience = issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };
        }
    }
}
