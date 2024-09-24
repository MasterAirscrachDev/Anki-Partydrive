using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using InTheHand.Bluetooth;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.Advertisement;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace OverdriveServer
{
    class Snooper
    {
        InterceptCar snoopingCar;
        GameDevice gamePhone;
        public async Task Start()
        {
            Console.WriteLine("Snooper started");
            snoopingCar = new InterceptCar(this);
            await Task.Delay(-1);
        }
        public void CreateVirtualCar()
        {
            Console.WriteLine("Creating virtual car");
            gamePhone = new GameDevice(this, snoopingCar.GetCarName());
        }   
        class InterceptCar{
            Snooper snooperHost;
            BluetoothDevice snoopingCar;
            string carBLEName;
            public InterceptCar(Snooper host){
                snooperHost = host;
                InitaliseBletooth();
            }

            public async Task InitaliseBletooth(){
                Bluetooth.AvailabilityChanged += (s, e) =>
                { Program.Log($"Bluetooth availability changed"); };
                Bluetooth.AdvertisementReceived += OnAdvertisementReceived;
                StartBLEScan();
                GetCars();
            }

            void StartBLEScan(){
                var leScanOptions = new BluetoothLEScanOptions();
                leScanOptions.AcceptAllAdvertisements = true;
                var scan = Bluetooth.RequestLEScanAsync(leScanOptions);
                if(scan == null)
                { Program.Log("Scan failed"); return; }
                Program.Log("Snooper Scan started");
            }
            async Task GetCars(){ 
                // Use default request options for simplicity
                var requestOptions = new RequestDeviceOptions();
                requestOptions.AcceptAllDevices = true;
                // Create a cancellation token source with a timeout of 5 seconds
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var cancellationToken = cancellationTokenSource.Token;
                // Scan for devices
                var devices = await Bluetooth.ScanForDevicesAsync(requestOptions, cancellationToken);
                if(devices.Count == 0){ Program.Log("No devices found"); return; }
                //we dont really car about the devices, we just want to wait for the scan to finish
            }

            async void OnAdvertisementReceived(object? sender, BluetoothAdvertisingEvent args){
                try{
                    //Program.Log($"Advertisement received {args.Name}");
                    if(args.Name.Contains("Drive")){
                        Program.Log($"Advertisement received, car {args.Name}");
                        snoopingCar = args.Device;
                        carBLEName = args.Name;
                        Bluetooth.AdvertisementReceived -= OnAdvertisementReceived;
                        await ConnectToCar();
                    }
                }
                catch{
                    Program.Log($"Advertisement received, not car {args.Name}");
                    foreach(var data in args.ManufacturerData)
                    { Program.Log($"{data.Key}: {Program.BytesToString(data.Value)}"); }
                }
            }
            async Task ConnectToCar(){
                await snoopingCar.Gatt.ConnectAsync();
                if(snoopingCar.Gatt.IsConnected){
                    Program.Log($"Snooper Connected to car {snoopingCar.Name}");
                    IsCarStillConnected();
                    var service = await snoopingCar.Gatt.GetPrimaryServiceAsync(CarSystem.ServiceID);
                    if(service == null){ return; }
                    var characteristic = await service.GetCharacteristicAsync(CarSystem.ReadID);
                    if(characteristic == null){ return; }
                    characteristic.CharacteristicValueChanged += OnCarCharacteristicChanged;
                    snooperHost.CreateVirtualCar();

                }else{
                    Program.Log($"Failed to connect to car {snoopingCar.Name}");
                }
            }
            async Task IsCarStillConnected(){
                while(snoopingCar.Gatt.IsConnected){
                    await Task.Delay(1000);
                }
                Program.Log($"Car disconnected");
            }
            void OnCarCharacteristicChanged(object sender, GattCharacteristicValueChangedEventArgs args){
                //relay step

                //log step
            }
            public string GetCarName(){
                return carBLEName;
            }
        }
        class GameDevice{
            Snooper snooperHost;
            GattServiceProvider virtualCar;
            GattLocalCharacteristic readNotifyCharacteristic;
            GattLocalCharacteristic writeCharacteristic;
            string carName;

            public GameDevice(Snooper host, string name){
                snooperHost = host;
                carName = name;
                CreateVirtualCar();
            }

            async Task CreateVirtualCar(){
                //create a new bluetooth device
                var service = await GattServiceProvider.CreateAsync(CarSystem.ServiceID);
                if (service.Error != Windows.Devices.Bluetooth.BluetoothError.Success) { 
                    Program.Log("Failed to create service"); return;
                }
                virtualCar = service.ServiceProvider;
                //create a read notify characteristic
                Program.Log("Creating read notify characteristic");
                var readNotifyResult = await virtualCar.Service.CreateCharacteristicAsync(
                    CarSystem.ReadID, new GattLocalCharacteristicParameters
                    {
                        CharacteristicProperties = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Notify | Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Read,
                        ReadProtectionLevel = GattProtectionLevel.Plain,
                    });

                if (readNotifyResult.Error != Windows.Devices.Bluetooth.BluetoothError.Success) {
                    Program.Log("Failed to create read notify characteristic"); return;
                }
                readNotifyCharacteristic = readNotifyResult.Characteristic;
                readNotifyCharacteristic.ReadRequested += OnVirtualCarCharacteristicChanged;
                //create a write characteristic
                Program.Log("Creating write characteristic");
                var writeResult = await virtualCar.Service.CreateCharacteristicAsync(
                    CarSystem.WriteID, new GattLocalCharacteristicParameters
                    {
                        CharacteristicProperties = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Write,
                        WriteProtectionLevel = GattProtectionLevel.Plain,
                    });

                if (writeResult.Error != Windows.Devices.Bluetooth.BluetoothError.Success) {
                    Program.Log("Failed to create write characteristic"); return;
                }
                writeCharacteristic = writeResult.Characteristic;
                writeCharacteristic.WriteRequested += OnVirtualCarWriteRequested;

                var advertisingParameters = new GattServiceProviderAdvertisingParameters
                {
                    IsDiscoverable = true,
                    IsConnectable = true
                };

                //publish the service
                Program.Log("Publishing service");
                virtualCar.StartAdvertising(advertisingParameters);
                Console.WriteLine("Virtual car created and advertising");
                StartAdvertising();
                Console.WriteLine("Virtual car advertising started");

            }
            void StartAdvertising() {
                try {
                    // Validate carName
                    if (string.IsNullOrEmpty(carName)) {
                        throw new ArgumentException("carName cannot be null or empty");
                    }
            
                    // Validate CarSystem.ServiceID
                    if (CarSystem.ServiceID == Guid.Empty) {
                        throw new ArgumentException("CarSystem.ServiceID cannot be an empty GUID");
                    }
            
                    // Create a new BluetoothLEAdvertisement
                    BluetoothLEAdvertisement advertisement = new BluetoothLEAdvertisement();
                    advertisement.LocalName = carName; // Set the server name here
            
                    // Optionally, add service UUIDs to advertise the GATT service
                    advertisement.ServiceUuids.Add(CarSystem.ServiceID);
                    Console.WriteLine("Advertisement created");
            
                    // Create a new BluetoothLEAdvertisementPublisher with the advertisement
                    var publisher = new BluetoothLEAdvertisementPublisher(advertisement);
                    Console.WriteLine("Publisher created");
            
                    // Start advertising
                    publisher.Start();
                    Console.WriteLine("Advertising started");
                } catch (ArgumentException ae) {
                    Program.Log($"Argument error: {ae.Message}"); //Value does not fall within the expected range (Name has invalid Chars??)
                } catch (Exception e) {
                    Program.Log($"Failed to start advertising: {e.Message}");
                }
            }
            void OnVirtualCarCharacteristicChanged(object sender, GattReadRequestedEventArgs args){
                //relay step

                //log step
            }
            void OnVirtualCarWriteRequested(object sender, GattWriteRequestedEventArgs args){
                //relay step

                //log step
            }
        }
    }
}
