using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.AspNetCore.DataProtection;

namespace Host8.EntityFramework
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection()
                .SetApplicationName("JS-EF-Sample");

            // Add BFF services to DI - also add server-side session management
            var cn = _configuration.GetConnectionString("db");
            services.AddBff(options =>
            {
                options.BackchannelLogoutAllUserSessions = true;
                options.EnableSessionCleanup = true;    
            })
                .AddRemoteApis()
                .AddEntityFrameworkServerSideSessions(options=> {
                    //options.UseSqlServer(cn);
                    options.UseSqlite(cn);
                });

            // local APIs
            services.AddControllers();

            // cookie options
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "cookie";
                options.DefaultChallengeScheme = "oidc";
                options.DefaultSignOutScheme = "oidc";
            })
                .AddCookie("cookie", options =>
                {
                    // host prefixed cookie name
                    options.Cookie.Name = "__Host-spa-ef";

                    // strict SameSite handling
                    options.Cookie.SameSite = SameSiteMode.Strict;
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "https://a-ci.ncats.io/_api/auth/ls";

                    // confidential client using code flow + PKCE
                    options.ClientId = "bff-duende";
                    // options.ClientSecret = "07bfe72e-530a-4e1d-9cda-ff736bd4e3eb";
                    options.ResponseType = "code";
                    options.ResponseMode = "query";

                    options.MapInboundClaims = false;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    // request scopes + refresh tokens
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("api");
                    options.Scope.Add("offline_access");
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseSerilogRequestLogging();
            app.UseDeveloperExceptionPage();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseRouting();

            // adds antiforgery protection for local APIs
            app.UseBff();

            // adds authorization for local and remote API endpoints
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // local APIs
                endpoints.MapControllers()
                    .RequireAuthorization()
                    .AsBffApiEndpoint();

                // login, logout, user, backchannel logout...
                endpoints.MapBffManagementEndpoints();

                // proxy endpoint for cross-site APIs
                // all calls to /api/* will be forwarded to the remote API
                // user or client access token will be attached in API call
                // user access token will be managed automatically using the refresh token
                endpoints.MapRemoteBffApiEndpoint("/api", "https://localhost:5010")
                    .RequireAccessToken(TokenType.UserOrClient);
            });
        }
    }
}
