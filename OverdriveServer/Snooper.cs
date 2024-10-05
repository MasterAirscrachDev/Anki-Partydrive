using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using InTheHand.Bluetooth;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;

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
                { Console.WriteLine($"Bluetooth availability changed"); };
                Bluetooth.AdvertisementReceived += OnAdvertisementReceived;
                StartBLEScan();
                GetCars();
            }

            void StartBLEScan(){
                var leScanOptions = new BluetoothLEScanOptions();
                leScanOptions.AcceptAllAdvertisements = true;
                var scan = Bluetooth.RequestLEScanAsync(leScanOptions);
                if(scan == null)
                { Console.WriteLine("Scan failed"); return; }
                Console.WriteLine("Snooper Scan started");
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
                if(devices.Count == 0){ Console.WriteLine("No devices found"); return; }
                //we dont really car about the devices, we just want to wait for the scan to finish
            }

            async void OnAdvertisementReceived(object? sender, BluetoothAdvertisingEvent args){
                try{
                    //Console.WriteLine($"Advertisement received {args.Name}");
                    if(args.Name.Contains("Drive")){
                        Console.WriteLine($"Advertisement received, car {args.Name}");
                        snoopingCar = args.Device;
                        carBLEName = args.Name;
                        Bluetooth.AdvertisementReceived -= OnAdvertisementReceived;
                        await ConnectToCar();
                    }
                }
                catch{
                    Console.WriteLine($"Advertisement received, not car {args.Name}");
                    foreach(var data in args.ManufacturerData)
                    { Console.WriteLine($"{data.Key}: {Program.BytesToString(data.Value)}"); }
                }
            }
            async Task ConnectToCar(){
                await snoopingCar.Gatt.ConnectAsync();
                if(snoopingCar.Gatt.IsConnected){
                    Console.WriteLine($"Snooper Connected to car {snoopingCar.Name}");
                    IsCarStillConnected();
                    var service = await snoopingCar.Gatt.GetPrimaryServiceAsync(CarSystem.ServiceID);
                    if(service == null){ return; }
                    var characteristic = await service.GetCharacteristicAsync(CarSystem.ReadID);
                    if(characteristic == null){ return; }
                    characteristic.CharacteristicValueChanged += OnCarCharacteristicChanged;
                    snooperHost.CreateVirtualCar();

                }else{
                    Console.WriteLine($"Failed to connect to car {snoopingCar.Name}");
                }
            }
            async Task IsCarStillConnected(){
                while(snoopingCar.Gatt.IsConnected){
                    await Task.Delay(1000);
                }
                Console.WriteLine($"Car disconnected");
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
                    Console.WriteLine("Failed to create service"); return;
                }
                virtualCar = service.ServiceProvider;
                //create a read notify characteristic
                Console.WriteLine("Creating read notify characteristic");
                var readNotifyResult = await virtualCar.Service.CreateCharacteristicAsync(
                    CarSystem.ReadID, new GattLocalCharacteristicParameters
                    {
                        CharacteristicProperties = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Notify | Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Read,
                        ReadProtectionLevel = GattProtectionLevel.Plain,
                    });

                if (readNotifyResult.Error != Windows.Devices.Bluetooth.BluetoothError.Success) {
                    Console.WriteLine("Failed to create read notify characteristic"); return;
                }
                readNotifyCharacteristic = readNotifyResult.Characteristic;
                readNotifyCharacteristic.ReadRequested += OnVirtualCarCharacteristicChanged;
                //create a write characteristic
                Console.WriteLine("Creating write characteristic");
                var writeResult = await virtualCar.Service.CreateCharacteristicAsync(
                    CarSystem.WriteID, new GattLocalCharacteristicParameters
                    {
                        CharacteristicProperties = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Write,
                        WriteProtectionLevel = GattProtectionLevel.Plain,
                    });

                if (writeResult.Error != Windows.Devices.Bluetooth.BluetoothError.Success) {
                    Console.WriteLine("Failed to create write characteristic"); return;
                }
                writeCharacteristic = writeResult.Characteristic;
                writeCharacteristic.WriteRequested += OnVirtualCarWriteRequested;

                var advertisingParameters = new GattServiceProviderAdvertisingParameters
                {
                    IsDiscoverable = true,
                    IsConnectable = true
                };

                //publish the service
                Console.WriteLine("Publishing service");
                virtualCar.StartAdvertising(advertisingParameters);
                Console.WriteLine("Virtual car created and advertising");
                StartAdvertising();
                Console.WriteLine("Virtual car advertising started");

            }
            void StartAdvertising(){
                try{
                    //CarName is not null or empty here

                    //CarSystem.ServiceID is valid here

                    // Create a new BluetoothLEAdvertisement
                    BluetoothLEAdvertisement advertisement = new BluetoothLEAdvertisement();
                    advertisement.LocalName = carName; // Set the server name here

                    // Optionally, add service UUIDs to advertise the GATT service
                    advertisement.ServiceUuids.Add(CarSystem.ServiceID);
                    Console.WriteLine($"Advertisement created with name: {carName} and ServiceID: {CarSystem.ServiceID}");

                    // Create a new BluetoothLEAdvertisementPublisher with the advertisement
                    var publisher = new BluetoothLEAdvertisementPublisher(advertisement);
                    // Start advertising
                    Console.WriteLine("Publisher created, Starting advertising");
                    publisher.Start();
                    Console.WriteLine("Publisher started");
                }
                catch (ArgumentException ae)
                {
                    Console.WriteLine($"Argument error: {ae.Message}");
                    Console.WriteLine($"Parameter name: {ae.ParamName}");
                    Console.WriteLine($"Stack trace: {ae.StackTrace}");
                }
                catch (InvalidOperationException ioe)
                { Console.WriteLine($"Operation error: {ioe.Message}"); }
                catch (Exception e)
                { Console.WriteLine($"Failed to start advertising: {e.Message}"); }
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
