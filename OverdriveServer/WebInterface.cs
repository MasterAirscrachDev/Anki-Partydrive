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
                                    await car.SetCarLane(offset);

                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Controlled");
                                }
                                catch{
                                    context.Response.StatusCode = 400;
                                    await context.Response.WriteAsync("Bad Request");
                                }
                            });
                            endpoints.MapGet("/resetcenter/{car}", async context =>{
                                var car = context.Request.RouteValues["car"];
                                Car c = Program.carSystem.GetCar(car.ToString());
                                if(c == null){
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                await c.SetCarTrackCenter(0);
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Centered");
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
                                Program.CheckCurrentTrack();
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
                            endpoints.MapGet("/canceltrackscan/{car}", async context =>{
                                var car = context.Request.RouteValues["car"];
                                Car c = Program.carSystem.GetCar(car.ToString());
                                if(c == null){
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Car not found");
                                    return;
                                }
                                await Program.CancelScan(c);
                                context.Response.StatusCode = 200;
                                //Console.WriteLine("Cancelled scan");
                                await context.Response.WriteAsync("Cancelled scan");
                            });
                            endpoints.MapGet("/disconnectcar/{instruct}", async context =>{
                                var instruct = context.Request.RouteValues["instruct"];
                                string carID = instruct.ToString();
                                if(carID == "all"){
                                    for(int i = 0; i < Program.carSystem.CarCount(); i++){
                                        await Program.carSystem.GetCar(i).RequestCarDisconnect();
                                    }
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
                            endpoints.MapGet("/track", async context =>{
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(Program.trackManager.TrackDataAsJson());
                            });
                        });
                        app.Run(async context =>{
                            if (context.Request.Path == "/")
                            { 
                                //simple html page that lists the cars and has buttons to control them
                                string carElements = "";
                                for(int i = 0; i < Program.carSystem.CarCount(); i++){
                                    Car car = Program.carSystem.GetCar(i);
                                    carElements += $"<div><h2>{car.id}</h2><p>{car.name}</p></div>";
                                }
//i should probably be beaten to death with mallets for this
string page = $@"<!DOCTYPE html>
<html>
<head>
    <title>Overdrive Server</title>
</head>
<body>
    <h1>Overdrive Server</h1>
    <h2>Cars</h2>
    {carElements}
    <form action='/scan'>
        <button type='submit'>Scan for cars</button>
    </form>
    <form action='/setlights/'>
        <input type='text' name='instruct' placeholder='CarID:R:G:B'>
        <button type='submit'>Set Lights</button>
    </form>
    <form action='/controlcar/'>
        <input type='text' name='instruct' placeholder='CarID:Speed:Offset'>
        <button type='submit'>Control Car</button>
    </form>

    <script>
    document.addEventListener('DOMContentLoaded', function() "+@"{
        // Select all forms
        const forms = document.querySelectorAll('form');

        forms.forEach(form => {
            form.addEventListener('submit', function(event) {
                event.preventDefault(); // Prevent default form submission

                let actionUrl = form.getAttribute('action'); // Get the form's action URL
                const formData = new FormData(form); // Collect form data

                // Construct query string
                data = '';
                //if formdata has 1 entry, set data to that entry
                if(formData.entries().next().value){
                    data = formData.entries().next().value[1];
                }
                actionUrl += data; // Append data to action URL
                console.log(actionUrl);
                // Send the request asynchronously
                fetch(actionUrl, {
                    method: 'GET',
                })
                .then(response => response.text()) // Convert response to text (or JSON if expected)
                .then(data => {
                    console.log('Success:', data); // Handle success
                })
                .catch((error) => {
                    console.error('Error:', error); // Handle errors
                });
            });
        });
    });
    </script>
</body>
</html>";
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
