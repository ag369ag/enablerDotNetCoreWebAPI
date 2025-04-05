using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace testASPWebAPI.Auth
{
    public class BasicAuth : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BasicAuth(
            IOptionsMonitor<AuthenticationSchemeOptions> option,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
            ) : base( option, logger, encoder, clock ) { }
        
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Unauthorized");
            }

            string authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader))
            {
                return AuthenticateResult.Fail("Unauthorized");
                
            }

            if(!authHeader.StartsWith("basic ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Unauthorized");
            }

            string token = authHeader.Substring(6);
            string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(token));

            string[] creds = credentials.Split(":");

            if (creds[0] != "Admin" && creds[1] != "admin123")
            {
                return AuthenticateResult.Fail("Authentication failed");
            }


            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, creds[0]),
                new Claim(ClaimTypes.Role, "Admin")
            };
            
            var identity = new ClaimsIdentity(claims, "Basic");
            var claimPrincipal = new ClaimsPrincipal(identity);

            /*if (creds[0].Equals("admin") && creds[1].Equals("admin"))
            {
            }*/
                return AuthenticateResult.Success(new AuthenticationTicket(claimPrincipal, Scheme.Name));
        }
    }
}
