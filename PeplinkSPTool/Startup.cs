using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PeplinkSPTool
{
    public class Startup
    {
        #region Service Configuration
        public void ConfigureServices(IServiceCollection services)
        {
            // Add CORS Policies
            services.AddCors(options =>
            {
                options.AddPolicy(name: "APIPolicy", build => build.AllowAnyOrigin().WithMethods("GET","POST").WithHeaders("authorization","content-type"));
            });
            
            // Configure routing
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                // Stop fucking with JSON
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // App Configuration
            app.UseStatusCodePages();
            app.UseRouting();
            app.UseCors();
            
            // Endpoint Router
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
            });
        }
        #endregion Service Configuration
    }
}