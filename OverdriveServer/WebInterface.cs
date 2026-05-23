using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Runtime.InteropServices;
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
                        var controlPanelPath = ControlPanelPath();
                        app.UseRouting();
                        
                        app.UseEndpoints(endpoints => {
                            // Index page
                            endpoints.MapGet("/", async context => {
                                string page = File.ReadAllText(Path.Combine(controlPanelPath, "index.html"));
                                context.Response.ContentType = "text/html";
                                await context.Response.WriteAsync(page);
                            });
                        });
                        
                        // Serve static files from ControlPanel directory
                        app.UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = new PhysicalFileProvider(
                                controlPanelPath),
                            RequestPath = ""
                        });
                    }
                );
            }
        );

        private static string ControlPanelPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "ControlPanel"));
            } else {
                return Path.Combine(Directory.GetCurrentDirectory(), "ControlPanel");
            }
        }
    }
}
