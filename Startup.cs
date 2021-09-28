using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Google.Apis.Auth.AspNetCore3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json.Linq;
using System.IO;

namespace GoogleCalendarApiWebExample
{
    public class Startup
    {

        private const string ClientSecretFilenameVariable = "GoogleApiJson";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            string clientSecretFilename = Environment.GetEnvironmentVariable(ClientSecretFilenameVariable);
            if (string.IsNullOrEmpty(clientSecretFilename))
            {
                throw new InvalidOperationException($"Please set the {ClientSecretFilenameVariable} environment variable before running tests.");
            }

            var secrets = JObject.Parse(File.ReadAllText(clientSecretFilename))["web"];

            // Check that this is a "web" client ID, not any other type of client ID like "installed app".
            // The "web" element should have been present in the json so secrets value shouldn't be null.
            if (secrets is null)
            {
                throw new InvalidOperationException(
                    $"The type of the OAuth2 client ID specified in {ClientSecretFilenameVariable} should be Web Application. You can read more about setting up OAuth2 client IDs here: https://support.google.com/cloud/answer/6158849?hl=en");
            }
            var projectId = secrets["project_id"].Value<string>();
            var clientId = secrets["client_id"].Value<string>();
            var clientSecret = secrets["client_secret"].Value<string>();    


            // This configures Google.Apis.Auth.AspNetCore3 for use in this app.
            services
                .AddAuthentication(o =>
                {
            // This forces challenge results to be handled by Google OpenID Handler, so there's no
            // need to add an AccountController that emits challenges for Login.
            o.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
            // This forces forbid results to be handled by Google OpenID Handler, which checks if
            // extra scopes are required and does automatic incremental auth.
            o.DefaultForbidScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
            // Default scheme that will handle everything else.
            // Once a user is authenticated, the OAuth2 token info is stored in cookies.
            o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddGoogleOpenIdConnect(options =>
                {
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
