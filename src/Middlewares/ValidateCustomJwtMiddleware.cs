using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WebApiSecurityExample2.Options;

namespace WebApiSecurityExample2.Middlewares
{
    /// <summary>
    /// Middleware to validate JWT created and signed by API GW when communicating with backend provider API 
    /// </summary>
    public class ValidateCustomJwtMiddleware : JwtSecurityTokenHandler
    {
        private readonly RequestDelegate _next;
        private readonly IOptions<CustomJwtValidationOptions> _customJwtValidationConfigSettings;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next">The next Delegate in pipeline</param>
        /// /// <param name="customJwtValidationConfigSettings"></param>
        public ValidateCustomJwtMiddleware(RequestDelegate next, IOptions<CustomJwtValidationOptions> customJwtValidationConfigSettings)
        {
            _next = next;
            _customJwtValidationConfigSettings = customJwtValidationConfigSettings;
        }

        /// <summary>
        /// Invoke method called when the Middleware is used
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            // Extract the Bearer token from the 'Authorization' Request Header
            // and trim the 'Bearer' prefix (if present)
            // ** PLACE THE BEARER TOKEN ACQUIRED BY CALLING
            // https://login.microsoftonline.com/07ef7b91-b9b2-444d-a45b-534166ce506a/oauth2/token PASSWORD GRANT FLOW  
            // INTO THE REQUEST HEADER
            var bearerToken = context.Request.Headers["Authorization"].ToString()
                .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

            if (!string.IsNullOrEmpty(bearerToken) 
                && IsCustomJwtValid(bearerToken))
            {
                //Invoke the next middleware in the pipeline
                await _next.Invoke(context);
            }
            else
            {
                context.Response.StatusCode = 403;
            }
        }

     
        /// <summary>
        /// Checks whether a token is valid
        /// </summary>
        /// <param name="token">The token sent by API GW or AAD</param>
        /// <returns>Boolean</returns>
        private bool IsCustomJwtValid(string token)
        {
            // Build the token validation parameters
            var validationParameters = BuildTokenValidationParameters();

            SecurityToken validatedToken = null;
           
                // If token can be read
            if (CanReadToken(token))
            {
                // Validate the token
                ValidateToken(token, validationParameters, out validatedToken);
            }

            return validatedToken != null;
        }

        /// <summary>
        /// Builds JWT validation parameters
        /// </summary>
        /// <returns>TokenValidationParameters</returns>
        private TokenValidationParameters BuildTokenValidationParameters()
        {
            // Variable 'wellKnownUrl' to hold the well-known URL
            // This variable should be extracted into a config level entry and initialised as part of constructor
            // When the Api App is deployed to SAND, PreProd and Prod, the value of this variable MUST materialise to:
            // SAND     :-  https://sand-api.londonmarketgroup.co.uk/discovery/.well-known/openid-configuration
            // PreProd  :-  https://preprod-api.londonmarketgroup.co.uk/discovery/.well-known/openid-configuration
            // Prod     :-  https://api.londonmarketgroup.co.uk/discovery/.well-known/openid-configuration

            // The 'wellKnownUrl' in this sample has been hardcoded to the well-known URL of API GW's AAD (SAND environment)
            // An 'access_token' retrieved from API GW's AAD i.e 'https://login.microsoftonline.com/07ef7b91-b9b2-444d-a45b-534166ce506a/oauth2/token'
            // can be used and will work purely for development environment
            var wellKnownUrl = _customJwtValidationConfigSettings.Value.WellknownDiscoveryUrl;


            // Variable 'validIssuer' to hold the JWT issuer
            // This variable should be extracted into a config level entry and initialised as part of constructor
            // When the Api App is deployed to SAND, PreProd and Prod, the value of this variable MUST materialise to:
            // SAND     :-  https://sand-api.londonmarketgroup.co.uk/
            // PreProd  :-  https://preprod-api.londonmarketgroup.co.uk/
            // Prod     :-  https://api.londonmarketgroup.co.uk/
            var validIssuer = _customJwtValidationConfigSettings.Value.ValidIssuer;


            // Instantiate HttpClient with an instance of HttpClientHandler that can
            // handle Gzip & Deflate response content encoding
            var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            
            // Instantiate ConfigurationManager
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(wellKnownUrl,
                new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever(client));

            // Get/Read the well-known configuration using ConfigurationManager
            var openIdConfig =
                configurationManager.GetConfigurationAsync(CancellationToken.None).Result;

            // Extract the security keys from the retrieved config
            var securityKeys = openIdConfig.SigningKeys.ToList();

            // Build token validation parameters
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, // Validate Issuer's signing key
                IssuerSigningKeys = securityKeys, // Set the IssuerSigningKeys to the ones retrieved from well-known
                RequireSignedTokens = true, // Only signed tokens accepted
                ValidateIssuer = true, // Validate Issuer
                ValidIssuer = validIssuer, // Valid Issuer value
                ValidateLifetime = true, // Validate JWT lifetime
                RequireExpirationTime = true, // Validate the Expiry time of JWT is present
                ValidateAudience = false // Do not validate the audience
            };

            return validationParameters;
        }


        /// <summary>
        /// Override of ValidateSignature to check signing keys and subject of certificate used to sign the JWT
        /// </summary>
        /// <param name="token"></param>
        /// <param name="validationParameters"></param>
        /// <returns></returns>
        protected override JwtSecurityToken ValidateSignature(string token,
            TokenValidationParameters validationParameters)
        {
            // Variable 'subject' to hold the subject of the certificate used for signing the JWT
            // This variable should be extracted into a config level entry and initialised as part of constructor
            // When the Api App is deployed to SAND, PreProd and Prod, the value of this variable MUST materialise to:
            // SAND     :-  CN=sand-api.londonmarketgroup.co.uk
            // PreProd  :-  CN=preprod-api.londonmarketgroup.co.uk
            // Prod     :-  CN=api.londonmarketgroup.co.uk
            
            var subject = _customJwtValidationConfigSettings.Value.ValidCn;

            var jwtSecurityToken = base.ValidateSignature(token, validationParameters);

            if (!(jwtSecurityToken.SigningKey is X509SecurityKey signingKey))
            {
                throw new SecurityTokenInvalidSigningKeyException("Invalid certificate.");
            }

            if (signingKey.Certificate.Subject != subject)
            {
                throw new SecurityTokenInvalidSigningKeyException("Invalid JWT sign certificate.");
            }

            return jwtSecurityToken;
        }
    }
}