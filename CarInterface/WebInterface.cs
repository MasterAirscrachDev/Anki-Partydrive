using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OverdriveServer
{
    class WebInterface
    {
        int linkCooldown = 0;
        public void Start(){
            string[] args = new string[]{"--urls", "http://localhost:7117"};
            CreateHostBuilder(args).Build().RunAsync();
        }
        IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureLogging(logging =>
                { logging.ClearProviders(); logging.SetMinimumLevel(LogLevel.Warning); }).ConfigureWebHostDefaults(webBuilder =>{
                    webBuilder.Configure(app =>{
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>{
                            endpoints.MapGet("/controlcar/{instruct}", async context =>{
                                var instruct = context.Request.RouteValues["instruct"];
                                try{
                                    string data = instruct.ToString();
                                    string[] parts = data.Split(':');
                                    string carID = parts[0];
                                    int speed = int.Parse(parts[1]);
                                    float offset = float.Parse(parts[2]);
                                    Car car = Program.carSystem.GetCar(carID);
                                    if(car == null){
                                        context.Response.StatusCode = 404;
                                        await context.Response.WriteAsync("Car not found");
                                        return;
                                    }
                                    await car.SetCarSpeed(speed);
                                    await car.SetCarTrackCenter(0);
                                    await car.SetCarLane(offset);

                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Controlled");
                                }
                                catch{
                                    context.Response.StatusCode = 400;
                                    await context.Response.WriteAsync("Bad Request");
                                }
                            });
                            endpoints.MapGet("/scan", async context =>{
                                Program.bluetoothInterface.ScanForCars();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Scanning for cars");
                            });
                            endpoints.MapGet("/cars", async context =>{
                                context.Response.ContentType = "application/json";
                                //return the list of cars cardata
                                await context.Response.WriteAsync(Program.carSystem.CarDataAsJson());
                            });
                            endpoints.MapGet("/batteries", async context =>{
                                for(int i = 0; i < Program.carSystem.CarCount(); i++){
                                    await Program.carSystem.GetCar(i).RequestCarBattery();
                                }
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Got battery levels, call /cars to get them");
                            });
                            endpoints.MapGet("/setlights/{instruct}", async context =>{
                                var instruct = context.Request.RouteValues["instruct"];
                                try{
                                    string data = instruct.ToString();
                                    string[] parts = data.Split(':');
                                    string carID = parts[0];
                                    float r = float.Parse(parts[1]);
                                    float g = float.Parse(parts[2]);
                                    float b = float.Parse(parts[3]);
                                    Car car = Program.carSystem.GetCar(carID);
                                    if(car == null){
                                        context.Response.StatusCode = 404;
                                        await context.Response.WriteAsync("Car not found");
                                        return;
                                    }
                                    await car.SetCarLightsPattern(r, g, b);
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Lights set");
                                }
                                catch{
                                    context.Response.StatusCode = 400;
                                    await context.Response.WriteAsync("Bad Request");
                                }
                            });
                            endpoints.MapGet("/registerlogs", async context =>{
                                Program.Log("Application Registered");
                                Program.GetLog(); Program.GetUtilLog();
                                Program.SetLogging(false);
                                linkCooldown = 5;
                                UnlinkApplication();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Logs registered, call /logs to get logs");
                            });
                            endpoints.MapGet("/logs", async context =>{
                                linkCooldown = 5;
                                await context.Response.WriteAsync(Program.GetLog());
                            });
                            endpoints.MapGet("/utillogs", async context =>{
                                await context.Response.WriteAsync(Program.GetUtilLog());
                            });
                            endpoints.MapGet("/clearcardata", async context =>{
                                Program.carSystem.ClearCarData();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Cleared car data");
                            });
                            endpoints.MapGet("/tts/{message}", async context =>{
                                var message = context.Request.RouteValues["message"];
                                Program.TTS(message.ToString());
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync($"Spoken: {message}");
                            });
                            endpoints.MapGet("/scantrack/{instruct}", async context =>{
                                var instruct = context.Request.RouteValues["instruct"];
                                string data = instruct.ToString();
                                string[] parts = data.Split(':');
                                string carID = parts[0];
                                int finishlines = int.Parse(parts[1]);
                                Car car = Program.carSystem.GetCar(carID);
                                if(car == null){
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                Program.ScanTrack(car, finishlines);
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Scanning track");
                            });
                        });
                        app.Run(async context =>{
                            if (context.Request.Path == "/")
                            { await context.Response.WriteAsync("CarInterface"); }
                            else{
                                context.Response.StatusCode = 404;
                                await context.Response.WriteAsync($"Failed to find path {context.Request.Path}");
                            }
                        });
                    });
                });
        async Task UnlinkApplication(){
            while(linkCooldown > 0){
                await Task.Delay(1000);
                linkCooldown--;
            }
            Program.SetLogging(true);
            Program.Log("Application Unlinked");
        }
    }
}
