using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Http;
using GlobalCMSUmbraco.ExternalApi.Attributes;
using GlobalCMSUmbraco.ExternalApi.TokenHandlers;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.Swagger.Annotations;

namespace GlobalCMSUmbraco.ExternalApi.Controllers
{
    [RoutePrefix("ExternalApi/Token")]
    [ShowInSwagger]
    public class TokenApiController : BaseExternalApiController
    {
        /// <summary>
        /// Generate a bearer token to use for your API session
        /// </summary>
        /// <remarks>
        /// If your API key is recognised a Json Web Token will be returned. This token should be saved and included in the header of all requests as `'Authorization: Bearer $TOKEN'`
        ///
        /// If your key is not recognised a `401` response code will be returned.
        /// </remarks>
        /// <param name="key">Your API key</param>
        /// <returns>A JWT token if key was recognised</returns>
        [Route("Generate")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(string))]
        [HttpPost]
        public IHttpActionResult Generate(string key)
        {
            //TODO: properly confirm key is valid, maybe log date last used?
            if (key != "Test_Api_Key_123")
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            var issuer = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);

            var secretKey = ApiConstants.SecretKey; // secret key which will be used later during validation
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenExpires = DateTime.Now.AddMinutes(ApiConstants.TokenExpiresMinutes);

            // TODO key id
            var permClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
                new Claim("keyId", "1")
            };

            var token = new JwtSecurityToken(
                issuer: issuer, 
                audience: issuer, 
                expires: tokenExpires,
                claims: permClaims,
                signingCredentials: credentials);

            var jwtToken = new LoggingJwtSecurityTokenHandler().WriteToken(token);
            return Json(jwtToken);
        }

        /// <summary>
        /// Confirm if bearer token is authenticated
        /// </summary>
        /// <remarks>
        /// Call this endpoint to test whether you have correctly set the authorization header.
        /// If your headers are correctly set and your request is authorised a `200` response code will be returned.
        /// Otherwise a `401` response code will be returned.
        /// </remarks>
        [JwtBearerTokenAuthorization]
        [Route("Confirm")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(bool))]
        [HttpGet]
        public IHttpActionResult Confirm()
        {
            return Json(true);
        }
    }
}
