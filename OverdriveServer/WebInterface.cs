using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.IO;
using static OverdriveServer.NetStructures;

namespace OverdriveServer {
    class WebInterface {
        public void Start(){
            string[] args = new string[]{"--urls", "http://0.0.0.0:7117"};
            CreateHostBuilder(args).Build().RunAsync();
            Console.WriteLine("ControlPanel: http://localhost:7117 (ctrl+click to open)");
        }
        
        IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureLogging(logging => { 
                logging.ClearProviders(); logging.SetMinimumLevel(LogLevel.Warning); }).ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.Configure(app =>{
                        app.UseRouting();
                        
                        app.UseEndpoints(endpoints => {
                            // Index page
                            endpoints.MapGet("/", async context => {
                                string page = File.ReadAllText(Path.Combine("ControlPanel", "index.html"));
                                context.Response.ContentType = "text/html";
                                await context.Response.WriteAsync(page);
                            });
                        });
                        
                        // Serve static files from ControlPanel directory
                        app.UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = new PhysicalFileProvider(
                                Path.Combine(Directory.GetCurrentDirectory(), "ControlPanel")),
                            RequestPath = ""
                        });
                    }
                );
            }
        );
    }
}
