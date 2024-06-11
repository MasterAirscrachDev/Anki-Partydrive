﻿using InTheHand.Bluetooth;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CarInterface
{
    class Program
    {
        static List<Car> cars = new List<Car>();
        static List<BluetoothUuid> advertisedServices = new List<BluetoothUuid>();
        static BluetoothUuid ServiceID = BluetoothUuid.FromGuid(new Guid("BE15BEEF-6186-407E-8381-0BD89C4D8DF4"));
        static BluetoothUuid ReadID = BluetoothUuid.FromGuid(new Guid("BE15BEE0-6186-407E-8381-0BD89C4D8DF4"));
        static BluetoothUuid WriteID = BluetoothUuid.FromGuid(new Guid("BE15BEE1-6186-407E-8381-0BD89C4D8DF4"));
        static string SysLog = "";
        static bool printLog = true;
        static async Task Main(string[] args)
        {
            Bluetooth.AvailabilityChanged += (s, e) =>
            {
                Log($"Bluetooth availability changed");
            };
            Bluetooth.AdvertisementReceived += OnAdvertisementReceived;
            StartBLEScan();
            await GetCars();
            //specify the port to be 80085
            args = new string[]{"--urls", "http://localhost:7117"};
            CreateHostBuilder(args).Build().RunAsync();
            await Task.Delay(-1);
        }
        static void Log(string message){
            SysLog += message + "\n";
            if(printLog){ Console.WriteLine(message); }
        }
        static void StartBLEScan(){
            var leScanOptions = new BluetoothLEScanOptions();
            leScanOptions.AcceptAllAdvertisements = true;
            var scan = Bluetooth.RequestLEScanAsync(leScanOptions);
            if(scan == null)
            { Log("Scan failed"); return; }
            Log("Scan started");
        }
        //maybe not needed?
        static async Task GetCars(){ 
            // Use default request options for simplicity
            var requestOptions = new RequestDeviceOptions();
            requestOptions.AcceptAllDevices = true;
            // Create a cancellation token source with a timeout of 5 seconds
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var cancellationToken = cancellationTokenSource.Token;
            // Scan for devices
            var devices = await Bluetooth.ScanForDevicesAsync(requestOptions, cancellationToken);
            
            if(devices.Count == 0)
            {
                Log("No devices found");
                return;
            }
            foreach(var device in devices)
            {
                Log($"name: {device.Name}, id: {device.Id}");
            }
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
                        cars.Add(new Car{ name = name, id = args.Device.Id, device = args.Device });
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
                await Task.Delay(1000);
                await SetCarSpeed(car, 500);
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
                Log($"Ping response: {BytesToString(content)}");
            } else if(id == 0x19){ //25 version response
                int version = content[2];
                Log($"Version response: {version}");
            } else if(id == 0x1b){ //27 battery response
                int battery = content[2];
                int maxBattery = 3800;
                Log($"Battery response: {battery} / {maxBattery}");
            } else if(id == 0x27){ //39 where is car
                int trackLocation = content[2];
                int trackID = content[3];
                float offset = BitConverter.ToSingle(content, 4);
                int speed = BitConverter.ToInt16(content, 8);
                bool goingBackwards = content[10] == 0x40; //this might be wrong

                Log($"{car.name} Track location: {trackLocation}, track ID: {trackID}, offset: {offset}, speed: {speed}, wrong way: {goingBackwards}");
            } else if(id == 0x2b){ //43 ONOH FALL
                Log($"{car.name} fell off track");
            }



            else{
                Log($"Unknown message {id}: {BytesToString(content)}");
            }
        }
        static string BytesToString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            if (context.Request.Path == "/")
                            {
                                await context.Response.WriteAsync("CarInterface");
                            }
                            else if (context.Request.Path == "/cars")
                            {
                                context.Response.ContentType = "application/json";
                                //return the list of cars cardata
                                CarData[] carData = new CarData[cars.Count];
                                for(int i = 0; i < cars.Count; i++){
                                    carData[i] = cars[i].data;
                                }
                                string json = JsonConvert.SerializeObject(carData);
                                await context.Response.WriteAsync(json);
                            }
                            else if (context.Request.Path == "/registerlogs"){
                                printLog = false;
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Logs registered, call /logs to get logs");
                            }
                            else if (context.Request.Path == "/logs"){
                                await context.Response.WriteAsync(SysLog);
                                SysLog = "";
                            }


                            else{
                                context.Response.StatusCode = 404;
                                await context.Response.WriteAsync("Not Found");
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
        CarData data;
    }
    [Serializable]
    class CarData{
        public string name;
        public string id;
        int trackPosition;
        int trackID;
        int laneOffset;
        int speed;
    }
}