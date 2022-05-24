using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;

namespace GlobalCMSUmbraco.ExternalApi.TokenHandlers
{
    public class LoggingJwtSecurityTokenHandler : JwtSecurityTokenHandler
    {
        

        public override ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            try
            {
                return base.ValidateToken(securityToken, validationParameters, out validatedToken);
            }
            catch (Exception ex)
            {
                Current.Logger.Warn<LoggingJwtSecurityTokenHandler>("JWT validation failed {error}", ex.Message);
                throw;
            }
        }
    }
}
