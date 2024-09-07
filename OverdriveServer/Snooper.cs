using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using InTheHand.Bluetooth;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.Advertisement;

namespace OverdriveServer
{
    class Snooper
    {
        BluetoothDevice snoopingCar;
        GattServiceProvider virtualCar;
        GattLocalCharacteristic readNotifyCharacteristic;
        GattLocalCharacteristic writeCharacteristic;
        public async Task Start()
        {
            Console.WriteLine("Snooper started");
            await InitaliseBletooth();
            await Task.Delay(-1);
        }
        async Task InitaliseBletooth(){
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
                Program.Log($"Advertisement received {args.Name}");
                if(args.Name.Contains("Drive")){
                    snoopingCar = args.Device;
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
                CreateVirtualCar();

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
        async Task CreateVirtualCar(){
            //create a new bluetooth device
            Program.Log("Creating virtual car");
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

            var writer = new DataWriter();
            writer.WriteString(snoopingCar.Name);
            advertisingParameters.ServiceData = writer.DetachBuffer();



            //publish the service
            Program.Log("Publishing service");
            virtualCar.StartAdvertising(advertisingParameters);
            Console.WriteLine("Virtual car created and advertising");

        }
        void OnVirtualCarCharacteristicChanged(object sender, GattReadRequestedEventArgs args){
            //relay step

            //log step
        }
        void OnVirtualCarWriteRequested(object sender, GattWriteRequestedEventArgs args){
            //relay step

            //log step
        }
        void OnCarCharacteristicChanged(object sender, GattCharacteristicValueChangedEventArgs args){
            //relay step

            //log step
        }
    }
}
