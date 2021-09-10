using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace azuread_sample
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = new PathString("/login");
                })

                // Remark: MsAcc does not parse User (claims) from the token, but rather calls userinfo endpoint
                // This contains actually less... significantly no tid ("tenant id")
                // So using OpenIdConnect
                // .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, o =>
                // {
                //     // Specifying endpoints, so it'll allow organization accounts only, no private MS-acc
                //     o.AuthorizationEndpoint = "https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize";
                //     o.TokenEndpoint = "https://login.microsoftonline.com/organizations/oauth2/v2.0/token";
                //     
                //     o.ClientId = _configuration["MsAcc:ClientId"];
                //     o.ClientSecret = _configuration["MsAcc:ClientSecret"];
                //     o.SaveTokens = true;
                //     o.Scope.Add("openid");
                //     o.Scope.Add("email");
                //     o.Scope.Add("profile");
                //     // o.Scope.Add("User.Read.All"); requires admin login
                // })
                .AddOpenIdConnect(MicrosoftAccountDefaults.AuthenticationScheme/*OpenIdConnectDefaults.AuthenticationScheme*/, o =>
                {
                    // */organization*, so it'll allow organization accounts only, no private MS-acc
                    o.Authority = "https://login.microsoftonline.com/organizations/v2.0";
                    
                    o.ClientId = _configuration["MsAcc:ClientId"];
                    o.ClientSecret = _configuration["MsAcc:ClientSecret"];
                    o.CallbackPath = "/signin-microsoft";
                    o.UsePkce = true;
                    o.SaveTokens = true;
                    
                    // We need custom issuer validation
                    // Even though requested from https://login.microsoftonline.com/organizations/v2.0, issuer will be
                    // https://login.microsoftonline.com/{tenantid}/v2.0, where tenantid is the current user's tenantid
                    o.TokenValidationParameters.IssuerValidator = (string issuer, SecurityToken token, TokenValidationParameters parameters) =>
                    {
                        if (issuer.StartsWith("https://login.microsoftonline.com/", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return issuer;
                        }
                        
                        string errorMessage = FormattableString.Invariant($"IDX10205: Issuer validation failed. Issuer: '{issuer}'");
                        throw new SecurityTokenInvalidIssuerException(errorMessage) { InvalidIssuer = issuer };
                    };

                    // No I don't want good JWT-names to be mapped to legacy stuff like: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/*
                    var handler = new JwtSecurityTokenHandler();
                    handler.InboundClaimTypeMap.Clear();
                    o.SecurityTokenValidator = handler;

                    o.Scope.Add("email");
                    // o.Scope.Add("openid");
                    // o.Scope.Add("profile");
                });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseRouting();
            
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
