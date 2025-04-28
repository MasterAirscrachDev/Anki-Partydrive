using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static OverdriveServer.NetStructures;

namespace OverdriveServer
{
    class WebInterface
    {
        public void Start()
        {
            string[] args = new string[] { "--urls", "http://localhost:7117" };
            CreateHostBuilder(args).Build().RunAsync();
        }
        //Depricated in favor of WebsocketManager
        IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureLogging(logging => {
                logging.ClearProviders(); logging.SetMinimumLevel(LogLevel.Warning);
            }).ConfigureWebHostDefaults(webBuilder => {
                webBuilder.Configure(app => {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => {
                        endpoints.MapGet("/car_move_update/{instruct}", async context => {
                            var instruct = context.Request.RouteValues["instruct"];
                            try
                            {
                                string data = instruct.ToString();
                                string[] parts = data.Split(':');
                                string carID = parts[0];
                                int speed = int.Parse(parts[1]);
                                float offset = float.Parse(parts[2]);
                                Car car = Program.carSystem.GetCar(carID);
                                if (car == null)
                                {
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                await car.SetCarSpeed(speed, 1000, false, false);
                                await car.SetCarLane(offset);
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Controlled");
                            }
                            catch
                            {
                                context.Response.StatusCode = 400;
                                await context.Response.WriteAsync("Bad Request");
                            }
                        });
                        endpoints.MapGet($"/{SV_SCAN}", async context => {
                            Program.bluetoothInterface.ScanForCars();
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("Scanning for cars");
                        });
                        endpoints.MapGet($"/{SV_GET_CARS}", async context => { //return the list of cars cardata
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(Program.carSystem.CarDataAsJson());
                        });
                        endpoints.MapGet($"/{SV_GET_TRACK}", async context => {
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(Program.trackManager.TrackDataAsJson());
                        });
                        endpoints.MapGet($"/{SV_TR_START_SCAN}", async context => {
                            Program.trackScanner.ScanTrack(1);
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("Scanning track");
                        });
                        endpoints.MapGet($"/{SV_LINEUP}", async context => {
                            context.Response.StatusCode = 200;
                            Program.trackManager.RequestLineup();
                            await context.Response.WriteAsync("Attempting to lineup cars");
                        });
                    });
                    app.Run(async context => {
                        if (context.Request.Path == "/")
                        {
                            //simple html page that lists the cars and has buttons to control them
                            //load the index.html file from the current directory
                            string page = System.IO.File.ReadAllText("index.html");
                            context.Response.ContentType = "text/html";
                            await context.Response.WriteAsync(page);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync($"Failed to find path {context.Request.Path}");
                        }
                    });
                }
            );
            }
        );
    }
}
