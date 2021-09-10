using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, o =>
                {
                    // Specifying endpoints, so it'll allow organization accounts only, no private MS-acc
                    o.AuthorizationEndpoint = "https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize";
                    o.TokenEndpoint = "https://login.microsoftonline.com/organizations/oauth2/v2.0/token";
                    
                    o.ClientId = _configuration["MsAcc:ClientId"];
                    o.ClientSecret = _configuration["MsAcc:ClientSecret"];
                    o.SaveTokens = true;
                    o.Scope.Add("openid");
                    o.Scope.Add("email");
                    o.Scope.Add("User.Read");
                    // o.Scope.Add("User.Read.All"); requires admin login
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
