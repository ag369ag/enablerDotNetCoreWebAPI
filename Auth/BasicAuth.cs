using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using testASPWebAPI.Data;

namespace testASPWebAPI.Auth
{
    public class BasicAuth : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ApplicationDBContext _dbContext;
        public BasicAuth(
            IOptionsMonitor<AuthenticationSchemeOptions> option,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ApplicationDBContext dbContext
            ) : base( option, logger, encoder, clock ) {
        
            _dbContext = dbContext;
        }
        
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
            try
            {
                var loggedInUser = _dbContext.API_Auth_User.ToList().Where(a => a.Username == creds[0]).First();
                if (loggedInUser == null)
                {
                    return AuthenticateResult.Fail("Authentication failed. User does not exist");
                }

                if(loggedInUser.isActive == 0)
                {
                    return AuthenticateResult.Fail("Authentication failed. User is not active");
                } 

                if (creds[0] != loggedInUser.Username || creds[1] != loggedInUser.Password)
                {
                    return AuthenticateResult.Fail("Authentication failed");
                }

            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail("Authentication failed. Error occured while validating user.");
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
