using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Bluetooth;

namespace OverdriveServer {
    class BluetoothInterface {
        List<string> checkingIDs = new List<string>();
        bool scanningForCars = false;
        public async Task InitaliseBletooth(){
            Bluetooth.AvailabilityChanged += (s, e) =>
            { Program.Log($"Bluetooth availability changed"); };
            Bluetooth.AdvertisementReceived += OnAdvertisementReceived;
            StartBLEScan();
        }
        public void ScanForCars(){ GetCars(); }
        void StartBLEScan(){
            var leScanOptions = new BluetoothLEScanOptions();
            leScanOptions.AcceptAllAdvertisements = true;
            var scan = Bluetooth.RequestLEScanAsync(leScanOptions);
            if(scan == null) { Program.Log("Scan failed"); return; }
            Program.Log("Scan started");
        }
        async Task GetCars(){ 
            if(scanningForCars){ Program.Log("Already scanning for cars"); return; }
            scanningForCars = true;
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
            scanningForCars = false;
        }
        void OnAdvertisementReceived(object? sender, BluetoothAdvertisingEvent args){
            try{
                if(args.Name.Contains("Drive")){
                    if(Program.carSystem.GetCar(args.Device.Id) == null && !checkingIDs.Contains(args.Device.Id)){
                        checkingIDs.Add(args.Device.Id);
                        //Program.Log($"Advertisement received for car");
                        //Log($"Manufacturer data: {args.ManufacturerData.Count}, Service data: {args.ServiceData.Count}");
                        foreach(var data in args.ManufacturerData) { // Assuming data.Value is the byte array containing the anki_vehicle_adv_mfg_t data
                            if (data.Value.Length >= 6) { // Ensure there are enough bytes
                                var identifier = BitConverter.ToUInt32(data.Value, 0);
                                var model_id = data.Value[4];
                                var product_id = BitConverter.ToUInt16(data.Value, 5);
                                Program.Log($"car info: Identifier: {identifier}, Model ID: {model_id}, Product ID: {product_id}");
                            }
                        }
                        foreach(var data in args.ServiceData) { Program.Log($"{data.Key}: {Program.BytesToString(data.Value)}"); }
                        Program.Log($"[0] car name: {args.Name}, id {args.Device.Id}, strength {args.Rssi}");
                        Program.carSystem.ConnectToCarAsync(args.Device);
                    }
                } 
            }
            catch{
                Program.Log($"Advertisement received, not car {args.Name}");
                foreach(var data in args.ManufacturerData) { Program.Log($"{data.Key}: {Program.BytesToString(data.Value)}"); }
            }
        }
        public void RemoveCarCheck(string id){
            checkingIDs.Remove(id);
        }
    }
}
