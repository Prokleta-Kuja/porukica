using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace porukica
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
            services.AddQuartzServer();

            services.Configure<Settings>(Configuration);

            services.AddControllers();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.Configure<RemoteBrowserFileStreamOptions>(o =>
            {
                //o.MaxBufferSize = 1024 * 1024; // default 1024kb
                // Breaks when over 20kb
                //o.MaxSegmentSize = 64 * 1024;  // default 20kb
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
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.ContentRootPath, C.UPLOAD_DIR)),
                RequestPath = C.DOWNLOAD_DIR,
                ServeUnknownFileTypes = true,
                DefaultContentType = "text/plain",
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub(o =>
                {
                    // o.TransportMaxBufferSize = o.ApplicationMaxBufferSize = 1024 * 1024;
                });
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
