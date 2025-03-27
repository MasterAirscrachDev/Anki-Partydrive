using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OverdriveServer {
    class WebInterface {
        int linkCooldown = 0;
        
        public void Start(){
            string[] args = new string[]{"--urls", "http://localhost:7117"};
            CreateHostBuilder(args).Build().RunAsync();
        }
        
        IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureLogging(logging => { 
                logging.ClearProviders(); logging.SetMinimumLevel(LogLevel.Warning); }).ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.Configure(app =>{
                        app.UseRouting();
                        app.UseEndpoints(endpoints => {
                            endpoints.MapGet("/controlcar/{instruct}", async context => {
                                var instruct = context.Request.RouteValues["instruct"];
                                try {
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
                                    await car.SetCarLane(offset);
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Controlled");
                                }
                                catch {
                                    context.Response.StatusCode = 400;
                                    await context.Response.WriteAsync("Bad Request");
                                }
                            });
                            endpoints.MapGet("/resetcenter/{instruct}", async context => {
                                var instruct = context.Request.RouteValues["instruct"];
                                string car = instruct.ToString();
                                float offset = 0;
                                if(car.Contains(":")) {
                                    string[] parts = car.Split(':');
                                    car = parts[0];
                                    offset = float.Parse(parts[1]);
                                }
                                Car c = Program.carSystem.GetCar(car);
                                if(c == null) {
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                await c.SetCarTrackCenter(offset);
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Centered");
                            });
                            endpoints.MapGet("/scan", async context => {
                                Program.bluetoothInterface.ScanForCars();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Scanning for cars");
                            });
                            endpoints.MapGet("/cars", async context => { //return the list of cars cardata
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(Program.carSystem.CarDataAsJson());
                            });
                            endpoints.MapGet("/batteries", async context => {
                                for(int i = 0; i < Program.carSystem.CarCount(); i++){ await Program.carSystem.GetCar(i).RequestCarBattery(); }
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Got battery levels, call /cars to get them");
                            });
                            endpoints.MapGet("/setlights/{instruct}", async context => {
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
                            endpoints.MapGet("/clearcardata", async context => {
                                Program.carSystem.ClearCarData();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Cleared car data");
                            });
                            endpoints.MapGet("/tts/{message}", async context => {
                                var message = context.Request.RouteValues["message"];
                                Program.TTS(message.ToString());
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync($"Spoken: {message}");
                            });
                            endpoints.MapGet("/scantrack/{instruct}", async context => {
                                var instruct = context.Request.RouteValues["instruct"];
                                string carID = instruct.ToString();
                                Car car = Program.carSystem.GetCar(carID);
                                if(car == null){
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                Program.ScanTrack(car);
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Scanning track");
                            });
                            endpoints.MapGet("/canceltrackscan/{car}", async context => {
                                var car = context.Request.RouteValues["car"];
                                Car c = Program.carSystem.GetCar(car.ToString());
                                if(c == null){
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                await Program.CancelScan(c);
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Cancelled scan");
                            });
                            endpoints.MapGet("/disconnectcar/{instruct}", async context => {
                                var instruct = context.Request.RouteValues["instruct"];
                                string carID = instruct.ToString();
                                if(carID == "all"){
                                    for(int i = 0; i < Program.carSystem.CarCount(); i++){ await Program.carSystem.GetCar(i).RequestCarDisconnect(); }
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Disconnected all cars");
                                    return;
                                }
                                Car car = Program.carSystem.GetCar(carID);
                                if(car == null){
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                await car.RequestCarDisconnect();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Disconnected");
                            });
                            endpoints.MapGet("/track", async context => {
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(Program.trackManager.TrackDataAsJson());
                            });
                            endpoints.MapGet("/lineup", async context => {
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
