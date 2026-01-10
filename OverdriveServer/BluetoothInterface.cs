using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OverdriveServer.Bluetooth;

namespace OverdriveServer 
{
    /// <summary>
    /// Legacy BluetoothInterface that now wraps the new abstracted system
    /// Maintains compatibility with existing code while using the new provider-independent approach
    /// </summary>
    class BluetoothInterface 
    {
        private readonly IBluetoothProvider _bluetoothProvider;

        public BluetoothInterface(IBluetoothProvider bluetoothProvider)
        {
            _bluetoothProvider = bluetoothProvider ?? throw new ArgumentNullException(nameof(bluetoothProvider));
        }

        public async Task InitaliseBletooth()
        {
            _bluetoothProvider.Scanner.AvailabilityChanged += (s, e) =>
            { Program.Log($"Bluetooth availability changed"); };
            _bluetoothProvider.Scanner.AdvertisementReceived += async (sender, args) => 
            { await OnAdvertisementReceived(sender, args); };
            await StartBLEScan();
        }
        async Task StartBLEScan()
        {
            var leScanOptions = _bluetoothProvider.CreateScanOptions();
            leScanOptions.AcceptAllAdvertisements = true;
            var scan = await _bluetoothProvider.Scanner.RequestScanAsync(leScanOptions);
            if(scan == null) { Program.Log("Scan failed"); return; }
            Program.Log("BLE: Scanning for Overdrive cars...");
        }
        async Task OnAdvertisementReceived(object? sender, IBluetoothAdvertisement args)
        {
           if(args.Device == null) { return; }
            string Name = args.Name ?? "Unknown";
            try {
                // Check if the device is a car by looking for Anki manufacturer ID (0xEFBE = 61374)
                uint model = 0;
                const int AnkiManufacturerId = 61374; // 0xEFBE
                
                if(args.ManufacturerData.Count > 0 && args.ManufacturerData.ContainsKey(AnkiManufacturerId))
                {
                    // Valid Anki Overdrive car - extract model from manufacturer data
                    if(args.ManufacturerData[AnkiManufacturerId].Length > 1)
                    {
                        model = args.ManufacturerData[AnkiManufacturerId][1];
                    }
                }
                else
                {
                    // No Anki manufacturer data - not a car
                    return;
                }
                
                // Check if autoConnect is enabled
                if(Program.autoConnect){ await Program.carSystem.ConnectToCarAsync(args.Device, model); } 
                else { Program.carSystem.AddAvailableCar(args.Device, model); }
            }
            catch(Exception ex)
            { Program.Log($"Advertisement received, not car {Name} ({ex.Message})"); }
        }
    }
}