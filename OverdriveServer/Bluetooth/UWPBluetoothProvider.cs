#if NET8_0_OR_GREATER && WINDOWS10_0_22621_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using OverdriveServer.Bluetooth;

namespace OverdriveServer.Bluetooth.UWP
{
    // UWP UUID wrapper
    public class UWPBluetoothUuid : IBluetoothUuid
    {
        private readonly Guid _guid;

        public UWPBluetoothUuid(Guid guid)
        {
            _guid = guid;
        }

        public UWPBluetoothUuid(string uuid)
        {
            _guid = Guid.Parse(uuid);
        }

        public string Value => _guid.ToString();

        public Guid ToGuid() => _guid;

        public bool Equals(IBluetoothUuid? other)
        {
            return other is UWPBluetoothUuid uwpOther && _guid.Equals(uwpOther._guid);
        }

        public override bool Equals(object? obj) => Equals(obj as IBluetoothUuid);
        public override int GetHashCode() => _guid.GetHashCode();

        // Implicit conversion to Guid
        public static implicit operator Guid(UWPBluetoothUuid uuid) => uuid._guid;
        public static implicit operator UWPBluetoothUuid(Guid guid) => new(guid);
    }

    // UWP scan filter wrapper
    public class UWPBluetoothScanFilter : IBluetoothScanFilter
    {
        public List<IBluetoothUuid> Services { get; } = new();
        public string? DeviceName { get; set; }
        public bool AcceptAllDevices { get; set; }
    }

    // UWP scan options wrapper
    public class UWPBluetoothScanOptions : IBluetoothScanOptions
    {
        public List<IBluetoothScanFilter> Filters { get; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool AcceptAllAdvertisements { get; set; }
    }

    // UWP Advertisement wrapper
    public class UWPBluetoothAdvertisement : IBluetoothAdvertisement
    {
        public IBluetoothDevice Device { get; }
        public string? Name { get; }
        public List<IBluetoothUuid> Uuids { get; }
        public Dictionary<int, byte[]> ManufacturerData { get; }
        public int Rssi { get; }

        public UWPBluetoothAdvertisement(
            IBluetoothDevice device, 
            string? name, 
            List<IBluetoothUuid> uuids, 
            Dictionary<int, byte[]> manufacturerData, 
            int rssi)
        {
            Device = device;
            Name = name;
            Uuids = uuids;
            ManufacturerData = manufacturerData;
            Rssi = rssi;
        }
    }

    // UWP Characteristic value changed event args
    public class UWPCharacteristicValueChangedEventArgs : ICharacteristicValueChangedEventArgs
    {
        public byte[] Value { get; }

        public UWPCharacteristicValueChangedEventArgs(byte[] value)
        {
            Value = value;
        }
    }

    // UWP Characteristic wrapper
    public class UWPBluetoothCharacteristic : IBluetoothCharacteristic
    {
        private readonly GattCharacteristic _characteristic;
        private bool _disposed;

        public UWPBluetoothCharacteristic(GattCharacteristic characteristic)
        {
            _characteristic = characteristic;
            Uuid = new UWPBluetoothUuid(_characteristic.Uuid);
            Properties = ConvertProperties(_characteristic.CharacteristicProperties);

            // Subscribe to value changed events
            _characteristic.ValueChanged += OnValueChanged;
        }

        public IBluetoothUuid Uuid { get; }
        public BluetoothCharacteristicProperties Properties { get; }

        public event EventHandler<ICharacteristicValueChangedEventArgs>? CharacteristicValueChanged;

        private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var buffer = args.CharacteristicValue;
            var data = new byte[buffer.Length];
            DataReader.FromBuffer(buffer).ReadBytes(data);
            
            CharacteristicValueChanged?.Invoke(this, new UWPCharacteristicValueChangedEventArgs(data));
        }

        public async Task<byte[]> ReadValueAsync()
        {
            try
            {
                var result = await _characteristic.ReadValueAsync();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    var buffer = result.Value;
                    var data = new byte[buffer.Length];
                    DataReader.FromBuffer(buffer).ReadBytes(data);
                    return data;
                }
                throw new InvalidOperationException($"Failed to read characteristic: {result.Status}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading characteristic: {ex.Message}", ex);
            }
        }

        public async Task WriteValueAsync(byte[] value, bool requireResponse = true)
        {
            try
            {
                var buffer = WindowsRuntimeBufferExtensions.AsBuffer(value);
                var writeType = requireResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse;
                
                var result = await _characteristic.WriteValueAsync(buffer, writeType);
                if (result != GattCommunicationStatus.Success)
                {
                    throw new InvalidOperationException($"Failed to write characteristic: {result}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error writing characteristic: {ex.Message}", ex);
            }
        }

        public async Task StartNotificationsAsync()
        {
            try
            {
                var result = await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    
                if (result != GattCommunicationStatus.Success)
                {
                    throw new InvalidOperationException($"Failed to start notifications: {result}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error starting notifications: {ex.Message}", ex);
            }
        }

        public async Task StopNotificationsAsync()
        {
            try
            {
                var result = await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None);
                    
                if (result != GattCommunicationStatus.Success)
                {
                    throw new InvalidOperationException($"Failed to stop notifications: {result}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error stopping notifications: {ex.Message}", ex);
            }
        }

        private static BluetoothCharacteristicProperties ConvertProperties(GattCharacteristicProperties properties)
        {
            var result = BluetoothCharacteristicProperties.None;

            if (properties.HasFlag(GattCharacteristicProperties.Read))
                result |= BluetoothCharacteristicProperties.Read;
            if (properties.HasFlag(GattCharacteristicProperties.Write))
                result |= BluetoothCharacteristicProperties.Write;
            if (properties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                result |= BluetoothCharacteristicProperties.WriteWithoutResponse;
            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                result |= BluetoothCharacteristicProperties.Notify;
            if (properties.HasFlag(GattCharacteristicProperties.Indicate))
                result |= BluetoothCharacteristicProperties.Indicate;

            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _characteristic.ValueChanged -= OnValueChanged;
                _disposed = true;
            }
        }
    }

    // UWP Service wrapper
    public class UWPBluetoothService : IBluetoothService
    {
        private readonly GattDeviceService _service;
        private readonly Dictionary<Guid, IBluetoothCharacteristic> _characteristicCache = new();
        private bool _disposed;

        public UWPBluetoothService(GattDeviceService service)
        {
            _service = service;
            Uuid = new UWPBluetoothUuid(_service.Uuid);
        }

        public IBluetoothUuid Uuid { get; }

        public async Task<IBluetoothCharacteristic> GetCharacteristicAsync(IBluetoothUuid characteristicUuid)
        {
            var guid = characteristicUuid.ToGuid();
            
            if (_characteristicCache.TryGetValue(guid, out var cachedCharacteristic))
                return cachedCharacteristic;

            try
            {
                var result = await _service.GetCharacteristicsForUuidAsync(guid);
                if (result.Status == GattCommunicationStatus.Success && result.Characteristics.Count > 0)
                {
                    var characteristic = new UWPBluetoothCharacteristic(result.Characteristics[0]);
                    _characteristicCache[guid] = characteristic;
                    return characteristic;
                }
                
                throw new InvalidOperationException($"Characteristic {characteristicUuid.Value} not found or access failed: {result.Status}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting characteristic: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<IBluetoothCharacteristic>> GetCharacteristicsAsync()
        {
            try
            {
                var result = await _service.GetCharacteristicsAsync();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    var characteristics = new List<IBluetoothCharacteristic>();
                    foreach (var characteristic in result.Characteristics)
                    {
                        var guid = characteristic.Uuid;
                        if (!_characteristicCache.TryGetValue(guid, out var cachedCharacteristic))
                        {
                            cachedCharacteristic = new UWPBluetoothCharacteristic(characteristic);
                            _characteristicCache[guid] = cachedCharacteristic;
                        }
                        characteristics.Add(cachedCharacteristic);
                    }
                    return characteristics.AsReadOnly();
                }
                
                throw new InvalidOperationException($"Failed to get characteristics: {result.Status}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting characteristics: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var characteristic in _characteristicCache.Values)
                {
                    characteristic.Dispose();
                }
                _characteristicCache.Clear();
                _disposed = true;
            }
        }
    }

    // UWP GATT Server wrapper
    public class UWPBluetoothGattServer : IBluetoothGattServer
    {
        private readonly BluetoothLEDevice _device;
        private readonly Dictionary<Guid, IBluetoothService> _serviceCache = new();
        private bool _disposed;

        public UWPBluetoothGattServer(BluetoothLEDevice device)
        {
            _device = device;
            _device.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        public bool IsConnected => _device.ConnectionStatus == BluetoothConnectionStatus.Connected;

        public event EventHandler? GattServerDisconnected;

        private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                GattServerDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task<IBluetoothService> GetPrimaryServiceAsync(IBluetoothUuid serviceUuid)
        {
            var guid = serviceUuid.ToGuid();
            
            if (_serviceCache.TryGetValue(guid, out var cachedService))
                return cachedService;

            try
            {
                var result = await _device.GetGattServicesForUuidAsync(guid);
                if (result.Status == GattCommunicationStatus.Success && result.Services.Count > 0)
                {
                    var service = new UWPBluetoothService(result.Services[0]);
                    _serviceCache[guid] = service;
                    return service;
                }
                
                throw new InvalidOperationException($"Service {serviceUuid.Value} not found or access failed: {result.Status}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting service: {ex.Message}", ex);
            }
        }

        public async Task ConnectAsync()
        {
            try
            {
                // In UWP, connection happens automatically when accessing services
                // We can trigger it by getting services
                var result = await _device.GetGattServicesAsync();
                if (result.Status != GattCommunicationStatus.Success)
                {
                    throw new InvalidOperationException($"Failed to connect to device: {result.Status}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error connecting to device: {ex.Message}", ex);
            }
        }

        public void Disconnect()
        {
            // UWP doesn't have explicit disconnect, we dispose the services
            foreach (var service in _serviceCache.Values)
            {
                service.Dispose();
            }
            _serviceCache.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
                Disconnect();
                _disposed = true;
            }
        }
    }

    // UWP Device wrapper
    public class UWPBluetoothDevice : IBluetoothDevice
    {
        private readonly BluetoothLEDevice _device;
        private readonly UWPBluetoothGattServer _gattServer;
        private bool _disposed;

        public UWPBluetoothDevice(BluetoothLEDevice device)
        {
            _device = device;
            _gattServer = new UWPBluetoothGattServer(_device);
            Id = _device.BluetoothAddress.ToString("X12");
            Name = _device.Name;
        }

        public string Id { get; }
        public string? Name { get; }
        public IBluetoothGattServer Gatt => _gattServer;

        public void Dispose()
        {
            if (!_disposed)
            {
                _gattServer?.Dispose();
                _disposed = true;
            }
        }
    }

    // UWP Scanner
    public class UWPBluetoothScanner : IBluetoothScanner
    {
        private readonly BluetoothLEAdvertisementWatcher _watcher;
        private readonly Dictionary<ulong, BluetoothLEDevice> _discoveredDevices = new();
        private CancellationTokenSource? _scanCancellation;

        public UWPBluetoothScanner()
        {
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active,
                AllowExtendedAdvertisements = true
            };

            _watcher.Received += OnAdvertisementReceived;
            _watcher.Stopped += OnScanStopped;
        }

        public bool IsScanning => _watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        public event EventHandler<IBluetoothAdvertisement>? AdvertisementReceived;
        public event EventHandler? AvailabilityChanged;

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                // Try to get the device
                BluetoothLEDevice? device = null;
                if (_discoveredDevices.TryGetValue(args.BluetoothAddress, out device))
                {
                    // Use cached device
                }
                else
                {
                    // Try to get device from advertisement
                    try
                    {
                        device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                        if (device != null)
                        {
                            _discoveredDevices[args.BluetoothAddress] = device;
                        }
                    }
                    catch
                    {
                        // Some devices can't be resolved, skip them
                        return;
                    }
                }

                if (device == null) return;

                var uwpDevice = new UWPBluetoothDevice(device);
                
                // Extract UUIDs from advertisement
                var uuids = new List<IBluetoothUuid>();
                foreach (var uuid in args.Advertisement.ServiceUuids)
                {
                    uuids.Add(new UWPBluetoothUuid(uuid));
                }

                // Extract manufacturer data
                var manufacturerData = new Dictionary<int, byte[]>();
                foreach (var data in args.Advertisement.ManufacturerData)
                {
                    var buffer = data.Data;
                    var bytes = new byte[buffer.Length];
                    DataReader.FromBuffer(buffer).ReadBytes(bytes);
                    manufacturerData[(int)data.CompanyId] = bytes;
                }

                var advertisement = new UWPBluetoothAdvertisement(
                    uwpDevice,
                    args.Advertisement.LocalName,
                    uuids,
                    manufacturerData,
                    args.RawSignalStrengthInDBm
                );

                AdvertisementReceived?.Invoke(this, advertisement);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing advertisement: {ex.Message}");
            }
        }

        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            // Handle scan stopped
        }

        public Task<IBluetoothScanResult?> RequestScanAsync(IBluetoothScanOptions options)
        {
            try
            {
                _scanCancellation = new CancellationTokenSource(options.Timeout);
                
                // Configure filters if any
                _watcher.ScanningMode = options.AcceptAllAdvertisements ? 
                    BluetoothLEScanningMode.Passive : BluetoothLEScanningMode.Active;

                // Add service UUID filters
                _watcher.AdvertisementFilter.Advertisement.ServiceUuids.Clear();
                foreach (var filter in options.Filters)
                {
                    foreach (var service in filter.Services)
                    {
                        _watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(service.ToGuid());
                    }
                }

                _watcher.Start();
                
                return Task.FromResult<IBluetoothScanResult?>(new UWPBluetoothScanResult(_watcher));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error starting scan: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<IBluetoothDevice>> ScanForDevicesAsync(IBluetoothScanOptions options, CancellationToken cancellationToken)
        {
            var devices = new List<IBluetoothDevice>();
            var deviceAddresses = new HashSet<ulong>();
            
            void OnDeviceFound(object? sender, IBluetoothAdvertisement advertisement)
            {
                var address = ulong.Parse(advertisement.Device.Id, System.Globalization.NumberStyles.HexNumber);
                if (!deviceAddresses.Contains(address))
                {
                    deviceAddresses.Add(address);
                    devices.Add(advertisement.Device);
                }
            }

            AdvertisementReceived += OnDeviceFound;
            
            try
            {
                using var timeoutCts = new CancellationTokenSource(options.Timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                await RequestScanAsync(options);
                
                await Task.Delay(options.Timeout, combinedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Timeout or cancellation is expected
            }
            finally
            {
                AdvertisementReceived -= OnDeviceFound;
                await StopScanAsync();
            }

            return devices.AsReadOnly();
        }

        public async Task StopScanAsync()
        {
            if (_watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
            {
                _watcher.Stop();
                _scanCancellation?.Cancel();
            }
            
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _scanCancellation?.Cancel();
            _watcher?.Stop();
            _discoveredDevices.Clear();
        }
    }

    // UWP Scan Result
    public class UWPBluetoothScanResult : IBluetoothScanResult
    {
        private readonly BluetoothLEAdvertisementWatcher _watcher;

        public UWPBluetoothScanResult(BluetoothLEAdvertisementWatcher watcher)
        {
            _watcher = watcher;
        }

        public bool IsScanning => _watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        public async Task StopAsync()
        {
            if (_watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
            {
                _watcher.Stop();
            }
            await Task.CompletedTask;
        }
    }

    // Main UWP Bluetooth Provider
    public class UWPBluetoothProvider : IBluetoothProvider
    {
        private readonly UWPBluetoothScanner _scanner;
        private bool _disposed;

        public UWPBluetoothProvider()
        {
            _scanner = new UWPBluetoothScanner();
        }

        public IBluetoothScanner Scanner => _scanner;

        public bool IsAvailable => true; // UWP Bluetooth is generally available on Windows 10+

        public async Task<bool> GetAvailabilityAsync()
        {
            try
            {
                // Check if Bluetooth is available
                var adapter = await BluetoothAdapter.GetDefaultAsync();
                return adapter != null && adapter.IsLowEnergySupported;
            }
            catch
            {
                return false;
            }
        }

        public IBluetoothUuid CreateUuid(Guid guid) => new UWPBluetoothUuid(guid);
        public IBluetoothUuid CreateUuid(string uuid) => new UWPBluetoothUuid(uuid);
        public IBluetoothScanOptions CreateScanOptions() => new UWPBluetoothScanOptions();
        public IBluetoothScanFilter CreateScanFilter() => new UWPBluetoothScanFilter();

        public void Dispose()
        {
            if (!_disposed)
            {
                _scanner?.Dispose();
                _disposed = true;
            }
        }
    }
}

#endif