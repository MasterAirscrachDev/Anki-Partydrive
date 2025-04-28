using InTheHand.Bluetooth;

namespace OverdriveServer {
    class BluetoothInterface {
        List<string> checkingIDs = new List<string>();
        bool scanningForCars = false;
        public async Task InitaliseBletooth(){
            Bluetooth.AvailabilityChanged += (s, e) =>
            { Program.Log($"Bluetooth availability changed"); };
            Bluetooth.AdvertisementReceived += (sender, args) =>
            { OnAdvertisementReceived(sender, args); };
            StartBLEScan();
        }
        public void ScanForCars(){ GetCars(); }
        async Task StartBLEScan(){
            var leScanOptions = new BluetoothLEScanOptions();
            leScanOptions.AcceptAllAdvertisements = true;
            var scan = await Bluetooth.RequestLEScanAsync(leScanOptions);
            if(scan == null) { Program.Log("Scan failed"); return; }
            Program.Log("Scan started");
        }
        async Task GetCars(){ 
            if(scanningForCars){ Program.Log("Already scanning for cars"); return; }
            scanningForCars = true; 
            try {
                // Use default request options for simplicity
                var requestOptions = new RequestDeviceOptions();
                //requestOptions.AcceptAllDevices = true;
                requestOptions.Timeout = TimeSpan.FromSeconds(5); // Set a timeout for the scan
                requestOptions.Filters.Add(new BluetoothLEScanFilter(){ Services = { CarSystem.ServiceID },  }); // Add the service UUID filter
                // Create a cancellation token source with a timeout of 3 seconds
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var cancellationToken = cancellationTokenSource.Token;
                // Scan for devices with the cancellation token
                var devices = await Bluetooth.ScanForDevicesAsync(requestOptions, cancellationToken);
                //if(devices.Count == 0){ Program.Log("No devices found"); }
                Program.Log($"Bluetooth Scan Complete"); // Log the number of devices found
            }
            catch (OperationCanceledException) {
                Program.Log("Device scan timed out after 5 seconds");
            }
            catch (Exception ex) {
                Program.Log($"Error during device scan: {ex.Message}");
            }
            finally {
                // Always reset the scanning flag, even if an exception occurs
                scanningForCars = false;
            }
        }
        async Task OnAdvertisementReceived(object? sender, BluetoothAdvertisingEvent args){
           if(args.Device == null) { return; }
            string Name = args.Name ?? "Unknown";
            try{
                if(!args.Uuids.Contains(CarSystem.ServiceID)) { return; } //check if the device is a car
                if(Program.carSystem.GetCar(args.Device.Id) == null && !checkingIDs.Contains(args.Device.Id)){
                    checkingIDs.Add(args.Device.Id);
                    //Program.Log($"Found car {Name} ({args.Device.Id})");
                    int model = 0;
                    if(args.ManufacturerData.Count > 0){ model = args.ManufacturerData[61374][1]; }
                    //NetStructures.ModelName modelName = (NetStructures.ModelName)model;
                    //Console.WriteLine($"Car Model: {modelName} ({model})");
                    if(args.ManufacturerData.Count == 0) { Program.Log($"No manufacturer data"); }
                    //foreach(var data in args.ServiceData) { Program.Log($"{data.Key}: {Program.BytesToString(data.Value)}"); }
                    //Program.Log($"[0] car name: {Name}, id {args.Device.Id}, strength {args.Rssi}");
                    Program.carSystem.ConnectToCarAsync(args.Device, model, args.Rssi);
                }
            }
            catch(Exception ex){
                Program.Log($"Advertisement received, not car {Name} ({ex.Message})");
                //foreach(var data in args.ManufacturerData) { Program.Log($"N: {Name} has data =  {data.Key}: {Program.BytesToString(data.Value)}"); }
            }
        }
        public void RemoveCarCheck(string id){
            checkingIDs.Remove(id);
        }
    }
}