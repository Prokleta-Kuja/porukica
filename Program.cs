using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace porukica
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Cleanup all uploads on startup and ensure folder is created
            var uploads = new DirectoryInfo(C.UPLOAD_DIR);
            if (uploads.Exists)
                uploads.Delete(true);

            uploads.Create();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
