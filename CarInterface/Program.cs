using InTheHand.Bluetooth;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace CarInterface
{
    class Program
    {
        static List<Car> cars = new List<Car>();
        static List<BluetoothUuid> advertisedServices = new List<BluetoothUuid>();
        static BluetoothUuid ServiceID = BluetoothUuid.FromGuid(new Guid("BE15BEEF-6186-407E-8381-0BD89C4D8DF4"));
        static BluetoothUuid ReadID = BluetoothUuid.FromGuid(new Guid("BE15BEE0-6186-407E-8381-0BD89C4D8DF4"));
        static BluetoothUuid WriteID = BluetoothUuid.FromGuid(new Guid("BE15BEE1-6186-407E-8381-0BD89C4D8DF4"));
        static string SysLog = "", UtilityLog = "";
        static bool printLog = true, scanningForCars = false;
        static async Task Main(string[] args)
        {
            Bluetooth.AvailabilityChanged += (s, e) =>
            {
                Log($"Bluetooth availability changed");
            };
            Bluetooth.AdvertisementReceived += OnAdvertisementReceived;
            args = new string[]{"--urls", "http://localhost:7117"};
            CreateHostBuilder(args).Build().RunAsync();
            StartBLEScan();
            await GetCars();
            await Task.Delay(-1);
        }
        static void Log(string message){
            SysLog += message + "\n";
            if(printLog){ Console.WriteLine(message); }
        }
        static void UtilLog(string message){
            UtilityLog += message + "\n";
        }
        static void StartBLEScan(){
            var leScanOptions = new BluetoothLEScanOptions();
            leScanOptions.AcceptAllAdvertisements = true;
            var scan = Bluetooth.RequestLEScanAsync(leScanOptions);
            if(scan == null)
            { Log("Scan failed"); return; }
            Log("Scan started");
        }
        static async Task GetCars(){ 
            if(scanningForCars){ Log("Already scanning for cars"); return; }
            scanningForCars = true;
            // Use default request options for simplicity
            var requestOptions = new RequestDeviceOptions();
            requestOptions.AcceptAllDevices = true;
            // Create a cancellation token source with a timeout of 5 seconds
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var cancellationToken = cancellationTokenSource.Token;
            // Scan for devices
            var devices = await Bluetooth.ScanForDevicesAsync(requestOptions, cancellationToken);
            
            if(devices.Count == 0)
            { Log("No devices found"); return; }
            //we dont really car about the devices, we just want to wait for the scan to finish
            // foreach(var device in devices)
            // {
            //     Log($"name: {device.Name}, id: {device.Id}");
            // }
            scanningForCars = false;
        }
        static void OnAdvertisementReceived(object? sender, BluetoothAdvertisingEvent args){
            try{
                if(args.Name.Contains("Drive")){
                    if(!cars.Exists(car => car.id == args.Device.Id)){
                        
                        Log($"Advertisement received for car");
                        Log($"Manufacturer data: {args.ManufacturerData.Count}, Service data: {args.ServiceData.Count}");
                        foreach(var data in args.ManufacturerData)
                        { // Assuming data.Value is the byte array containing the anki_vehicle_adv_mfg_t data
                            if (data.Value.Length >= 6) // Ensure there are enough bytes
                            {
                                var identifier = BitConverter.ToUInt32(data.Value, 0);
                                var model_id = data.Value[4];
                                var product_id = BitConverter.ToUInt16(data.Value, 5);

                                Log($"car info: Identifier: {identifier}, Model ID: {model_id}, Product ID: {product_id}");
                            }
                        }
                        foreach(var data in args.ServiceData)
                        { Log($"{data.Key}: {BytesToString(data.Value)}"); }
                        Log($"car name: {args.Name}, id {args.Device.Id}, strength {args.Rssi}");
                        string name = args.Name; //give cars placeholder names until we can get the real name
                        if(args.Device.Id == "E7FFF13FD1FF"){ name = "Truck Sticker";}
                        else if(args.Device.Id == "E70E96A36CD3"){ name = "Sport Sticker";}
                        else if(args.Device.Id == "E6E40DEA6A75"){ name = "DeadShock";}
                        else if(args.Device.Id == "CD73BF704022"){ name = "Skull";}
                        cars.Add(new Car{ name = name, id = args.Device.Id, device = args.Device, data = new CarData{ name = name, id = args.Device.Id }});
                        ConnectToCarAsync(cars[cars.Count - 1]);
                    }
                } 
            }
            catch{
                advertisedServices.AddRange(args.Uuids);
                Log($"Advertisement received, not car {args.Name}");
                foreach(var data in args.ManufacturerData)
                {
                    Log($"{data.Key}: {BytesToString(data.Value)}");
                }
            }
        }
        static async Task ConnectToCarAsync(Car car){
            Log($"Connecting to car {car.name}");

            await car.device.Gatt.ConnectAsync();
            if(car.device.Gatt.IsConnected){
                Log($"Connected to car {car.name}");
                CheckCarConnection(car);
                //Expected
                //Service              "BE15BEEF-6186-407E-8381-0BD89C4D8DF4"
                //Characteristic Read  "BE15BEE0-6186-407E-8381-0BD89C4D8DF4"
                //Characteristic Write "BE15BEE1-6186-407E-8381-0BD89C4D8DF4"
                var service = await car.device.Gatt.GetPrimaryServiceAsync(ServiceID);
                //subscribe to characteristic changed event on read characteristic
                var characteristic = await service.GetCharacteristicAsync(ReadID);
                characteristic.CharacteristicValueChanged += (sender, args) => {
                    CarCharacteristicChanged(sender, args, car);
                };
                await characteristic.StartNotificationsAsync();
                await EnableSDKMode(car);
                await Task.Delay(500);
                UtilLog($"-1:{car.id}");
                //await SetCarSpeed(car, 100);
            }
            else{
                Log($"Failed to connect to car {car.name}");
            }
        }
        static async Task CheckCarConnection(Car car){
            while(car.device.Gatt.IsConnected){
                await Task.Delay(5000);
            }
            Log($"Car disconnected {car.name}");
            cars.Remove(car);
        }
        static async Task EnableSDKMode(Car car){
            //4 bytes 0x03 0x90 0x01 0x01
            byte[] data = new byte[]{0x03, 0x90, 0x01, 0x01};
            await WriteToCarAsync(car.id, data, true);
        }
        static async Task SetCarSpeed(Car car, int speed){
            //speed = 0-1000, rescale to 300 - 1200
            speed = (int)(speed * 0.9 + 300);
            byte[] data = new byte[7];
            data[0] = 0x06;
            data[1] = 0x24;
            //speed as int16
            data[2] = (byte)(speed & 0xFF);
            data[3] = (byte)((speed >> 8) & 0xFF);
            //1000 as int16
            data[4] = 0xE8;
            data[5] = 0x03;
            await WriteToCarAsync(car.id, data, true);
        }
        static async Task SetCarTrackCenter(Car car, float offset){
            byte[] data = new byte[6];
            data[0] = 0x05;
            data[1] = 0x2c;
            BitConverter.GetBytes((float)offset).CopyTo(data, 2); // Offset value (?? 68,23,-23,68 seem to be lane values 1-4)
            await WriteToCarAsync(car.id, data, true);
        }
        static async Task SetCarLane(Car car, float lane){
            byte[] data = new byte[12];
            data[0] = 11;
            data[1] = 0x25;
            int horizontalSpeedmm = 250, horizontalAccelmm = 1000;
            //horizontal speed as int16
            data[2] = (byte)(horizontalSpeedmm & 0xFF);
            data[3] = (byte)((horizontalSpeedmm >> 8) & 0xFF);
            //horizontal accel as int16
            data[4] = (byte)(horizontalAccelmm & 0xFF);
            data[5] = (byte)((horizontalAccelmm >> 8) & 0xFF);
            //lane as float
            BitConverter.GetBytes((float)lane).CopyTo(data, 6);

            await WriteToCarAsync(car.id, data, true);
        }

        static async Task RequestCarBattery(Car car){
            byte[] data = new byte[]{0x01, 0x1a};
            await WriteToCarAsync(car.id, data, true);
        }

        static async Task WriteToCarAsync(string carID, byte[] data, bool response = false){
            var car = cars.Find(car => car.id == carID);
            if(car == null){
                Log($"Car {carID} not found");
                return;
            }
            var service = await car.device.Gatt.GetPrimaryServiceAsync(ServiceID);
            var characteristic = await service.GetCharacteristicAsync(WriteID);
            //send someth
            if(response){ await characteristic.WriteValueWithResponseAsync(data); }
            else{ await characteristic.WriteValueWithoutResponseAsync(data); }
        }
        static void CarCharacteristicChanged(object sender, GattCharacteristicValueChangedEventArgs args, Car car){
            ParseMessage(args.Value, car);
        }
        static void ParseMessage(byte[] content, Car car){
            byte id = content[1];
            if(id == 0x17){//23 ping response
                Log($"[23] Ping response: {BytesToString(content)}");
            } else if(id == 0x19){ //25 version response
                int version = content[2];
                Log($"[25] Version response: {version}");
            } else if(id == 0x1b){ //27 battery response
                int battery = content[2];
                int maxBattery = 3800;
                Log($"[27] Battery response: {battery} / {maxBattery}");
                UtilLog($"27:{car.id}:{battery}");
                car.data.battery = battery;
            } else if(id == 0x27){ //39 where is car
                int trackLocation = content[2];
                int trackID = content[3];
                float offset = BitConverter.ToSingle(content, 4);
                int speed = BitConverter.ToInt16(content, 8);
                //tf does location mean
                UtilLog($"39:{car.id}:{trackLocation}:{trackID}:{offset}:{speed}");
                Log($"[39] {car.name} Track location: {trackLocation}, track ID: {trackID}, offset: {offset}, speed: {speed}");
                //IDs
                //39 FnF Straight 40 Straight
                //17 FnF Curve 18 Curve
                //57 FnF Powerup
                //
                car.data.trackPosition = trackLocation;
                car.data.trackID = trackID;
                car.data.laneOffset = offset;
                car.data.speed = speed;
            } else if(id == 0x29){ //41 car track pice update
                try{
                    if(content.Length < 18){ return; } //not enough data
                    int trackPiece = (sbyte)content[2];
                    int oldTrackPiece = (sbyte)content[3];
                    float offset = BitConverter.ToSingle(content, 4);
                    int uphillCounter = content[14];
                    int downhillCounter = content[15];
                    int leftWheelDistance = content[16];
                    int rightWheelDistance = content[17];

                    // There is a shorter segment for the starting line track.
                    string crossedStartingLine = "";
                    if ((leftWheelDistance < 0x25) && (leftWheelDistance > 0x19) && (rightWheelDistance < 0x25) && (rightWheelDistance > 0x19)) {
                        crossedStartingLine = " (Crossed Starting Line)";
                    }
                    UtilLog($"41:{car.id}:{trackPiece}:{oldTrackPiece}:{offset}:{uphillCounter}:{downhillCounter}:{leftWheelDistance}:{rightWheelDistance}:{!string.IsNullOrEmpty(crossedStartingLine)}");
                    Log($"[41] {car.name} Track: {trackPiece} from {oldTrackPiece}, up:{uphillCounter}down:{downhillCounter}, offest: {offset} LwheelDist: {leftWheelDistance}, RwheelDist: {rightWheelDistance} {crossedStartingLine}");
                }
                catch{
                    Log($"[41] Error parsing car track update: {BytesToString(content)}");
                    return;
                }
                
            } else if(id == 0x2a){ //42 car error
                int error = content[2];
                Log($"[42] {car.name} error: {error}");
            } //43 ONOH FALL
            else if(id == 0x2b){ //43 ONOH FALL
                Log($"[43] {car.name} fell off track");
            } else if(id == 0x53){ //83 FnF specialBlock
                Log($"[83] {car.name} hit special block");
            }


            else{
                Log($"Unknown message {id} [{IntToByteString(id)}]: {BytesToString(content)}");
                //54
                //77
            }
        }
        static string IntToByteString(int number)
        {
            //as 0x00
            return "0x" + number.ToString("X2");
        }
        static string BytesToString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            // Example of mapping a custom endpoint (similar to your scenario)
                            endpoints.MapGet("/controlcar/{instruct}", async context =>
                            {
                                var instruct = context.Request.RouteValues["instruct"];
                                try{
                                    string data = instruct.ToString();
                                    string[] parts = data.Split(':');
                                    string carID = parts[0];
                                    int speed = int.Parse(parts[1]);
                                    float offset = float.Parse(parts[2]);
                                    Car car = cars.Find(car => car.id == carID);
                                    if(car == null){
                                        context.Response.StatusCode = 404;
                                        await context.Response.WriteAsync("Car not found");
                                        return;
                                    }
                                    await SetCarSpeed(car, speed);
                                    //await SetCarLaneOffset(car, offset);
                                    await SetCarLane(car, offset);
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Speed set");
                                }
                                catch{
                                    context.Response.StatusCode = 400;
                                    await context.Response.WriteAsync("Bad Request");
                                }
                            });
                            endpoints.MapGet("/scan", async context =>
                            {
                                await GetCars();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Scanning for cars");
                            });
                            endpoints.MapGet("/cars", async context =>
                            {
                                context.Response.ContentType = "application/json";
                                //return the list of cars cardata
                                CarData[] carData = new CarData[cars.Count];
                                for(int i = 0; i < cars.Count; i++){
                                    carData[i] = cars[i].data;
                                }
                                string json = JsonConvert.SerializeObject(carData);
                                await context.Response.WriteAsync(json);
                            });
                            endpoints.MapGet("/batteries", async context =>
                            {
                                for(int i = 0; i < cars.Count; i++){
                                    await RequestCarBattery(cars[i]);
                                }
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Got battery levels, call /cars to get them");
                            });
                            endpoints.MapGet("/registerlogs", async context =>
                            {
                                Log("Application Registered");
                                SysLog = "";
                                printLog = false;
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Logs registered, call /logs to get logs");
                            });
                            endpoints.MapGet("/logs", async context =>
                            {
                                await context.Response.WriteAsync(SysLog);
                                SysLog = "";
                            });
                            endpoints.MapGet("/utillogs", async context =>
                            {
                                await context.Response.WriteAsync(UtilityLog);
                                UtilityLog = "";
                            });
                        });
                        app.Run(async context =>
                        {
                            if (context.Request.Path == "/")
                            { await context.Response.WriteAsync("CarInterface"); }
                            else{
                                context.Response.StatusCode = 404;
                                await context.Response.WriteAsync($"Failed to find path {context.Request.Path}");
                            }
                        });
                    });
                });
    }
    class Car{
        public required string name;
        public required string id;
        //bluetooth connection
        public required BluetoothDevice device;
        public required CarData data;
    }
    [System.Serializable]
    class CarData{
        public required string name;
        public required string id;
        public int trackPosition;
        public int trackID;
        public float laneOffset;
        public int speed;
        public int battery;
    }
}