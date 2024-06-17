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
using System.Speech.Synthesis;

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
        static List<string> checkingIDs = new List<string>();
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
        static void TTS(string message){
            SpeechSynthesizer synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();
            synth.Speak(message);
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
                    if(!cars.Exists(car => car.id == args.Device.Id) && !checkingIDs.Contains(args.Device.Id)){
                        checkingIDs.Add(args.Device.Id);
                        Log($"Advertisement received for car");
                        //Log($"Manufacturer data: {args.ManufacturerData.Count}, Service data: {args.ServiceData.Count}");
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
                        ConnectToCarAsync(args.Device);
                    }
                } 
            }
            catch{
                advertisedServices.AddRange(args.Uuids);
                Log($"Advertisement received, not car {args.Name}");
                foreach(var data in args.ManufacturerData)
                { Log($"{data.Key}: {BytesToString(data.Value)}"); }
            }
        }
        static async Task ConnectToCarAsync(BluetoothDevice carDevice){
            FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
            Save s = await fs.LoadFile($"{carDevice.Id}.dat");
            string name = "Unknown Car";
            int speedBalance = 0;
            bool hadConfig = false; 
            if(s != null){
                hadConfig = true;
                name = s.GetString("name");
                speedBalance = (int)s.GetInt("speedBalance");
            }
            Log($"Connecting to car {name}");

            await carDevice.Gatt.ConnectAsync();
            if(carDevice.Gatt.IsConnected){
                Log($"Connected to car {name}");
                Car car = new Car(name, carDevice.Id, carDevice, speedBalance);
                if(!hadConfig){
                    s = new Save();
                    s.SetString("name", carDevice.Name);
                    s.SetInt("speedBalance", 0);
                    await fs.SaveFile($"{carDevice.Id}.dat", s);
                }
                //Expected
                //Service              "BE15BEEF-6186-407E-8381-0BD89C4D8DF4"
                //Characteristic Read  "BE15BEE0-6186-407E-8381-0BD89C4D8DF4"
                //Characteristic Write "BE15BEE1-6186-407E-8381-0BD89C4D8DF4"
                var service = await car.device.Gatt.GetPrimaryServiceAsync(ServiceID);
                if(service == null){ return; }
                //subscribe to characteristic changed event on read characteristic
                var characteristic = await service.GetCharacteristicAsync(ReadID);
                if(characteristic == null){ return; }
                cars.Add(car);
                checkingIDs.Remove(carDevice.Id);
                CheckCarConnection(car);
                characteristic.CharacteristicValueChanged += (sender, args) => {
                    CarCharacteristicChanged(sender, args, car);
                };
                await characteristic.StartNotificationsAsync();
                await EnableSDKMode(car);
                await Task.Delay(500);
                UtilLog($"-1:{car.id}:{car.name}");
            }
            else{
                Log($"Failed to connect to car {name}");
                checkingIDs.Remove(carDevice.Id);
            }
        }
        static async Task CheckCarConnection(Car car){
            while(car.device.Gatt.IsConnected){
                await Task.Delay(5000);
            }
            Log($"Car disconnected {car.name}");
            UtilLog($"-2:{car.id}");
            cars.Remove(car);
        }
        static async Task EnableSDKMode(Car car, bool enable = true){
            //4 bytes 0x03 0x90 0x01 0x01
            byte enabled = enable ? (byte)0x01 : (byte)0x00;
            byte[] data = new byte[]{0x03, 0x90, enabled, 0x01};
            await WriteToCarAsync(car.id, data, true);
        }
        static async Task SetCarSpeed(Car car, int speed, int accel = 1000){
            if(car.speedBalance != 0){
                speed = Math.Clamp(speed + car.speedBalance, 0, 1200);
            }
            byte[] data = new byte[7];
            data[0] = 0x06;
            data[1] = 0x24;
            //speed as int16
            data[2] = (byte)(speed & 0xFF);
            data[3] = (byte)((speed >> 8) & 0xFF);
            //accel as int16
            data[4] = (byte)(accel & 0xFF);
            data[5] = (byte)((accel >> 8) & 0xFF);
            await WriteToCarAsync(car.id, data, true);
        }
        static async Task SetCarTrackCenter(Car car, float offset){
            byte[] data = new byte[6];
            data[0] = 0x05;
            data[1] = 0x2c;
            BitConverter.GetBytes((float)offset).CopyTo(data, 2); 
            await WriteToCarAsync(car.id, data, true);
        }
        static async Task SetCarLane(Car car, float lane){ // Offset value (?? 68,23,-23,68 seem to be lane values 1-4)
            byte[] data = new byte[12];
            data[0] = 11;
            data[1] = 0x25;
            int horizontalSpeedmm = 100, horizontalAccelmm = 1000;
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
        static async Task SetCarLights(Car car, float r, float g, float b){
            byte[] data = new byte[18];
            data[0] = 0x11; //size
            data[1] = 0x33; //id (set lights) 51
            data[2] = 0x03; //???
            data[3] = 0x00; //???
            data[4] = 0x00; //???
            int rByte =  r == 1 ? 0x01 : 0x00;
            int gByte =  g == 1 ? 0x01 : 0x00;
            int bByte =  b == 1 ? 0x01 : 0x00;
            data[5] = (byte)rByte; //red
            data[6] = (byte)rByte; //red2??
            data[7] = 0x00; //??
            data[8] = 0x03; //??
            data[9] = 0x00; //??
            data[10] = (byte)gByte; //green
            data[11] = (byte)gByte; //green2??
            data[12] = 0x00; //??
            data[13] = 0x02; // Solid ??
            data[14] = 0x00; //??
            data[15] = (byte)bByte; //blue
            data[16] = (byte)bByte; //blue2??
            data[17] = 0x00; //??
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
                //36 ??? 39 FnF Straight 40 Straight
                //17 18 20 23 FnF Curve / Curve
                //57 FnF Powerup
                //34 PreFinishLine
                //33 Start/Finish

                car.data.trackPosition = trackLocation;
                car.data.trackID = trackID;
                car.data.laneOffset = offset;
                car.data.speed = speed;
            } else if(id == 0x29){ //41 car moved between track pieces
                try{
                    if(content.Length < 18){ return; } //not enough data
                    //Console.WriteLine(BytesToString(content));
                    int trackPiece = Convert.ToInt32((sbyte)content[2]);
                    int oldTrackPiece = Convert.ToInt32((sbyte)content[3]);
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
            } else if(id == 0x2b){ //43 ONOH FALL
                UtilLog($"43:{car.id}");
                Log($"[43] {car.name} fell off track");
            } else if(id == 0x36){ //54 car speed changed
                Log($"[54] {car.name} speed changed");
                UtilLog($"54:{car.id}");
            } else if(id == 0x3f){ //63 charging status changed
                bool charging = content[3] == 1;
                UtilLog($"63:{car.id}:{charging}");
                Log($"[63] {car.name} charging: {charging}");
                car.data.charging = charging;
            } else if(id == 0x4d){ //77 Collision Detected
                UtilLog($"77:{car.id}");
                Log($"[77] {car.name} collision detected");
            }
            else if(id == 0x53){ //83 FnF specialBlock
                UtilLog($"83:{car.id}");
                Log($"[83] {car.name} hit special block");
            }

            else{
                Log($"Unknown message {id} [{IntToByteString(id)}]: {BytesToString(content)}");
                //45
                //65
                //134 CarMsgCycleOvertime
            }
        }
        static string IntToByteString(int number)
        { return "0x" + number.ToString("X2"); } //as 0x00
        static string BytesToString(byte[] bytes)
        { return BitConverter.ToString(bytes).Replace("-", ""); }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
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
                                    Car car = cars.Find(car => car.id == carID);
                                    if(car == null){
                                        context.Response.StatusCode = 404;
                                        await context.Response.WriteAsync("Car not found");
                                        return;
                                    }
                                    await SetCarSpeed(car, speed);
                                    await SetCarTrackCenter(car, 0);
                                    await SetCarLane(car, offset);
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Controlled");
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
                            endpoints.MapGet("/setlights/{instruct}", async context =>
                            {
                                var instruct = context.Request.RouteValues["instruct"];
                                try{
                                    string data = instruct.ToString();
                                    string[] parts = data.Split(':');
                                    string carID = parts[0];
                                    float r = float.Parse(parts[1]);
                                    float g = float.Parse(parts[2]);
                                    float b = float.Parse(parts[3]);
                                    Car car = cars.Find(car => car.id == carID);
                                    if(car == null){
                                        context.Response.StatusCode = 404;
                                        await context.Response.WriteAsync("Car not found");
                                        return;
                                    }
                                    await SetCarLights(car, r, g, b);
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Lights set");
                                }
                                catch{
                                    context.Response.StatusCode = 400;
                                    await context.Response.WriteAsync("Bad Request");
                                }
                            });
                            endpoints.MapGet("/registerlogs", async context =>
                            {
                                Log("Application Registered");
                                SysLog = "";
                                UtilityLog = "";
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
                            endpoints.MapGet("/clearcardata", async context =>
                            {
                                for(int i = 0; i < cars.Count; i++){
                                    cars[i].data = new CarData(cars[i].name, cars[i].id);
                                }
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Cleared car data");
                            });
                            endpoints.MapGet("/tts/{message}", async context =>
                            {
                                var message = context.Request.RouteValues["message"];
                                TTS(message.ToString());
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Spoke");
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
        public string name;
        public string id;
        //bluetooth connection
        public BluetoothDevice device;
        public int speedBalance = 0;
        public CarData data;
        public Car(string name, string id, BluetoothDevice device, int speedBalance = 0){
            this.name = name;
            this.id = id;
            this.device = device;
            this.speedBalance = speedBalance;
            this.data = new CarData(name, id);
        }
    }
    [System.Serializable]
    class CarData{
        public string name;
        public string id;
        public int trackPosition;
        public int trackID;
        public float laneOffset;
        public int speed;
        public int battery;
        public bool charging;
        public CarData(string name, string id){
            this.name = name;
            this.id = id;
        }
    }
}