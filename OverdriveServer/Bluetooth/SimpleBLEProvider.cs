using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OverdriveServer.Bluetooth
{
    /// <summary>
    /// Native SimpleBLE wrapper using P/Invoke to the custom C wrapper library
    /// </summary>
    internal static class NativeSimpleBLE
    {
        // Determine library name based on platform
        private const string LibraryName = "bluetooth_wrapper";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NotificationCallback(IntPtr peripheralHandle, IntPtr data, int dataLen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_init();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_scan_start(int timeoutMs);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_scan_stop();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_get_device_count();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_get_device_info(int index, 
            [MarshalAs(UnmanagedType.LPArray)] byte[] identifier, int idLen,
            [MarshalAs(UnmanagedType.LPArray)] byte[] address, int addrLen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_get_manufacturer_data_count(int index);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_get_manufacturer_data(int index, int mfgIndex,
            ref ushort manufacturerId,
            [MarshalAs(UnmanagedType.LPArray)] byte[] data, int maxLen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr bt_connect([MarshalAs(UnmanagedType.LPStr)] string address);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_disconnect(IntPtr peripheralHandle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int bt_read(IntPtr peripheralHandle,
            [MarshalAs(UnmanagedType.LPStr)] string serviceUuid,
            [MarshalAs(UnmanagedType.LPStr)] string characteristicUuid,
            [MarshalAs(UnmanagedType.LPArray)] byte[] data, int maxLen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int bt_write(IntPtr peripheralHandle,
            [MarshalAs(UnmanagedType.LPStr)] string serviceUuid,
            [MarshalAs(UnmanagedType.LPStr)] string characteristicUuid,
            [MarshalAs(UnmanagedType.LPArray)] byte[] data, int len);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int bt_notify(IntPtr peripheralHandle,
            [MarshalAs(UnmanagedType.LPStr)] string serviceUuid,
            [MarshalAs(UnmanagedType.LPStr)] string characteristicUuid,
            NotificationCallback callback);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int bt_unsubscribe(IntPtr peripheralHandle,
            [MarshalAs(UnmanagedType.LPStr)] string serviceUuid,
            [MarshalAs(UnmanagedType.LPStr)] string characteristicUuid);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int bt_is_connected(IntPtr peripheralHandle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void bt_cleanup();
    }

    // SimpleBLE-based implementations
    public class SimpleBLEUuid : IBluetoothUuid
    {
        private readonly string _uuid;

        public SimpleBLEUuid(Guid guid)
        {
            _uuid = guid.ToString();
        }

        public SimpleBLEUuid(string uuid)
        {
            _uuid = uuid;
        }

        public string Value => _uuid;
        public Guid ToGuid() => Guid.Parse(_uuid);

        public bool Equals(IBluetoothUuid? other)
        {
            return other != null && string.Equals(_uuid, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as IBluetoothUuid);
        public override int GetHashCode() => _uuid.GetHashCode();
    }

    public class SimpleBLEScanFilter : IBluetoothScanFilter
    {
        public List<IBluetoothUuid> Services { get; } = new();
        public string? DeviceName { get; set; }
        public bool AcceptAllDevices { get; set; }
    }

    public class SimpleBLEScanOptions : IBluetoothScanOptions
    {
        public List<IBluetoothScanFilter> Filters { get; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool AcceptAllAdvertisements { get; set; }
    }

    public class SimpleBLEAdvertisement : IBluetoothAdvertisement
    {
        public SimpleBLEAdvertisement(IBluetoothDevice device, string? name, List<IBluetoothUuid> uuids, int rssi, Dictionary<int, byte[]>? manufacturerData = null)
        {
            Device = device;
            Name = name;
            Uuids = uuids;
            Rssi = rssi;
            ManufacturerData = manufacturerData ?? new Dictionary<int, byte[]>();
        }

        public IBluetoothDevice Device { get; }
        public string? Name { get; }
        public List<IBluetoothUuid> Uuids { get; }
        public Dictionary<int, byte[]> ManufacturerData { get; }
        public int Rssi { get; }
    }

    public class SimpleBLECharacteristicValueChangedEventArgs : ICharacteristicValueChangedEventArgs
    {
        public SimpleBLECharacteristicValueChangedEventArgs(byte[] value)
        {
            Value = value;
        }

        public byte[] Value { get; }
    }

    public class SimpleBLECharacteristic : IBluetoothCharacteristic
    {
        private readonly IntPtr _peripheralHandle;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private bool _disposed = false;
        private NativeSimpleBLE.NotificationCallback? _nativeCallback;

        public SimpleBLECharacteristic(IntPtr peripheralHandle, string serviceUuid, string characteristicUuid)
        {
            _peripheralHandle = peripheralHandle;
            _serviceUuid = serviceUuid;
            _characteristicUuid = characteristicUuid;
        }

        public IBluetoothUuid Uuid => new SimpleBLEUuid(_characteristicUuid);

        public BluetoothCharacteristicProperties Properties =>
            BluetoothCharacteristicProperties.Read |
            BluetoothCharacteristicProperties.Write |
            BluetoothCharacteristicProperties.Notify;

        public event EventHandler<ICharacteristicValueChangedEventArgs>? CharacteristicValueChanged;

        public Task<byte[]> ReadValueAsync()
        {
            return Task.Run(() =>
            {
                byte[] buffer = new byte[512];
                int bytesRead = NativeSimpleBLE.bt_read(_peripheralHandle, _serviceUuid, _characteristicUuid, buffer, buffer.Length);
                
                if (bytesRead < 0)
                    throw new Exception($"Failed to read characteristic: error {bytesRead}");

                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            });
        }

        public Task WriteValueAsync(byte[] value, bool requireResponse = true)
        {
            return Task.Run(() =>
            {
                int result = NativeSimpleBLE.bt_write(_peripheralHandle, _serviceUuid, _characteristicUuid, value, value.Length);
                
                if (result != 0)
                    throw new Exception($"Failed to write characteristic: error {result}");
            });
        }

        public Task StartNotificationsAsync()
        {
            return Task.Run(() =>
            {
                // Keep the delegate alive to prevent garbage collection
                _nativeCallback = (peripheralHandle, dataPtr, dataLen) =>
                {
                    try
                    {
                        byte[] data = new byte[dataLen];
                        if (dataLen > 0)
                        {
                            Marshal.Copy(dataPtr, data, 0, dataLen);
                        }
                        CharacteristicValueChanged?.Invoke(this, new SimpleBLECharacteristicValueChangedEventArgs(data));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in notification callback: {ex.Message}");
                    }
                };

                int result = NativeSimpleBLE.bt_notify(_peripheralHandle, _serviceUuid, _characteristicUuid, _nativeCallback);
                
                if (result != 0)
                    throw new Exception($"Failed to start notifications: error {result}");
            });
        }

        public Task StopNotificationsAsync()
        {
            return Task.Run(() =>
            {
                int result = NativeSimpleBLE.bt_unsubscribe(_peripheralHandle, _serviceUuid, _characteristicUuid);
                _nativeCallback = null;
                
                if (result != 0)
                    throw new Exception($"Failed to stop notifications: error {result}");
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    StopNotificationsAsync().Wait(1000);
                }
                catch { }
                _disposed = true;
            }
        }
    }

    public class SimpleBLEService : IBluetoothService
    {
        private readonly IntPtr _peripheralHandle;
        private readonly string _serviceUuid;
        private bool _disposed = false;

        public SimpleBLEService(IntPtr peripheralHandle, string serviceUuid)
        {
            _peripheralHandle = peripheralHandle;
            _serviceUuid = serviceUuid;
        }

        public IBluetoothUuid Uuid => new SimpleBLEUuid(_serviceUuid);

        public Task<IBluetoothCharacteristic> GetCharacteristicAsync(IBluetoothUuid characteristicUuid)
        {
            // For SimpleBLE, we don't need to enumerate characteristics
            // Just return a wrapper that will work with the specified UUID
            var characteristic = new SimpleBLECharacteristic(_peripheralHandle, _serviceUuid, characteristicUuid.Value);
            return Task.FromResult<IBluetoothCharacteristic>(characteristic);
        }

        public Task<IReadOnlyList<IBluetoothCharacteristic>> GetCharacteristicsAsync()
        {
            // SimpleBLE doesn't provide characteristic enumeration through our wrapper
            // Return empty list - consumers should use GetCharacteristicAsync with known UUIDs
            return Task.FromResult<IReadOnlyList<IBluetoothCharacteristic>>(new List<IBluetoothCharacteristic>());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    public class SimpleBLEGattServer : IBluetoothGattServer
    {
        private readonly SimpleBLEDevice _device;
        private bool _disposed = false;
        private Task? _connectionMonitor;
        private CancellationTokenSource? _monitorCts;

        public SimpleBLEGattServer(SimpleBLEDevice device)
        {
            _device = device;
        }

        public bool IsConnected
        {
            get
            {
                var handle = _device.GetHandle();
                if (handle == IntPtr.Zero) return false;
                return NativeSimpleBLE.bt_is_connected(handle) == 1;
            }
        }

        public event EventHandler? GattServerDisconnected;

        private void StartConnectionMonitoring()
        {
            _monitorCts = new CancellationTokenSource();
            _connectionMonitor = Task.Run(async () =>
            {
                bool wasConnected = IsConnected;
                
                while (!_monitorCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        bool isConnected = IsConnected;
                        
                        if (wasConnected && !isConnected)
                        {
                            GattServerDisconnected?.Invoke(this, EventArgs.Empty);
                            wasConnected = false;
                            break; // Stop monitoring after disconnect
                        }
                        else if (!wasConnected && isConnected)
                        {
                            wasConnected = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error monitoring connection: {ex.Message}");
                    }
                    
                    await Task.Delay(1000, _monitorCts.Token);
                }
            }, _monitorCts.Token);
        }

        public Task<IBluetoothService> GetPrimaryServiceAsync(IBluetoothUuid serviceUuid)
        {
            var handle = _device.GetHandle();
            if (handle == IntPtr.Zero)
                throw new Exception("Device not connected");
                
            var service = new SimpleBLEService(handle, serviceUuid.Value);
            return Task.FromResult<IBluetoothService>(service);
        }

        public Task ConnectAsync()
        {
            return Task.Run(() =>
            {
                var handle = _device.GetHandle();
                if (handle == IntPtr.Zero)
                {
                    // Need to connect
                    IntPtr newHandle = NativeSimpleBLE.bt_connect(_device.GetAddress());
                    if (newHandle == IntPtr.Zero)
                    {
                        throw new Exception("Failed to connect to device");
                    }
                    _device.SetHandle(newHandle);
                    StartConnectionMonitoring();
                }
            });
        }

        public void Disconnect()
        {
            try
            {
                var handle = _device.GetHandle();
                if (handle != IntPtr.Zero)
                {
                    NativeSimpleBLE.bt_disconnect(handle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _monitorCts?.Cancel();
                _connectionMonitor?.Wait(1000);
                _monitorCts?.Dispose();
                _disposed = true;
            }
        }
    }

    public class SimpleBLEDevice : IBluetoothDevice
    {
        private IntPtr _peripheralHandle;
        private readonly SimpleBLEGattServer _gattServer;
        private readonly string _address;
        private bool _disposed = false;

        public SimpleBLEDevice(IntPtr peripheralHandle, string address, string? name)
        {
            _peripheralHandle = peripheralHandle;
            _address = address;
            _gattServer = new SimpleBLEGattServer(this);
            Id = address;
            Name = name;
        }

        public string Id { get; }
        public string? Name { get; }
        public IBluetoothGattServer Gatt => _gattServer;

        internal IntPtr GetHandle() => _peripheralHandle;
        
        internal string GetAddress() => _address;
        
        internal void SetHandle(IntPtr handle)
        {
            _peripheralHandle = handle;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _gattServer.Dispose();
                if (_peripheralHandle != IntPtr.Zero)
                {
                    NativeSimpleBLE.bt_disconnect(_peripheralHandle);
                }
                _disposed = true;
            }
        }
    }

    public class SimpleBLEScanResult : IBluetoothScanResult
    {
        private bool _isScanning;

        public SimpleBLEScanResult(bool isScanning)
        {
            _isScanning = isScanning;
        }

        public bool IsScanning => _isScanning;

        public Task StopAsync()
        {
            if (_isScanning)
            {
                NativeSimpleBLE.bt_scan_stop();
                _isScanning = false;
            }
            return Task.CompletedTask;
        }
    }

    public class SimpleBLEScanner : IBluetoothScanner
    {
        private bool _isScanning = false;
        private bool _continuousScanning = false;
        private readonly Dictionary<string, SimpleBLEDevice> _discoveredDevices = new();

        public bool IsScanning => _isScanning;

        public event EventHandler<IBluetoothAdvertisement>? AdvertisementReceived;
        public event EventHandler? AvailabilityChanged;

        public async Task<IBluetoothScanResult?> RequestScanAsync(IBluetoothScanOptions options)
        {
            if (_isScanning)
            {
                NativeSimpleBLE.bt_scan_stop();
                _isScanning = false;
            }
            
            // Start scan (0 = no auto-stop)
            int result = NativeSimpleBLE.bt_scan_start(0);
            if (result != 0)
            {
                throw new Exception($"Failed to start scan: error {result}");
            }

            _isScanning = true;
            _continuousScanning = true;

            // Continuous scan loop in background
            _ = Task.Run(async () =>
            {
                try
                {
                    while (_continuousScanning)
                    {
                        await Task.Delay(options.Timeout);
                        
                        if (!_continuousScanning) break;
                        
                        // Get scan results
                        int deviceCount = NativeSimpleBLE.bt_get_device_count();
                        
                        for (int i = 0; i < deviceCount; i++)
                        {
                            byte[] idBuffer = new byte[256];
                            byte[] addrBuffer = new byte[64];
                            
                            int getResult = NativeSimpleBLE.bt_get_device_info(i, idBuffer, idBuffer.Length, addrBuffer, addrBuffer.Length);
                            
                            if (getResult == 0)
                            {
                                string identifier = Encoding.UTF8.GetString(idBuffer).TrimEnd('\0');
                                string address = Encoding.UTF8.GetString(addrBuffer).TrimEnd('\0');
                                
                                // Get manufacturer data
                                Dictionary<int, byte[]> manufacturerData = new Dictionary<int, byte[]>();
                                int mfgCount = NativeSimpleBLE.bt_get_manufacturer_data_count(i);
                                
                                for (int m = 0; m < mfgCount; m++)
                                {
                                    ushort manufacturerId = 0;
                                    byte[] mfgDataBuffer = new byte[27];
                                    int mfgDataLen = NativeSimpleBLE.bt_get_manufacturer_data(i, m, ref manufacturerId, mfgDataBuffer, mfgDataBuffer.Length);
                                    
                                    if (mfgDataLen > 0)
                                    {
                                        byte[] actualData = new byte[mfgDataLen];
                                        Array.Copy(mfgDataBuffer, actualData, mfgDataLen);
                                        manufacturerData[manufacturerId] = actualData;
                                    }
                                }
                                
                                if (!string.IsNullOrEmpty(address) && !_discoveredDevices.ContainsKey(address))
                                {
                                    // Apply filters
                                    bool matches = ApplyFilters(identifier, options);
                                    
                                    if (matches)
                                    {
                                        // Create device without connecting during scan
                                        var device = new SimpleBLEDevice(IntPtr.Zero, address, identifier);
                                        _discoveredDevices[address] = device;
                                        
                                        var advertisement = new SimpleBLEAdvertisement(
                                            device, 
                                            identifier, 
                                            new List<IBluetoothUuid>(), 
                                            -50, // Default RSSI
                                            manufacturerData
                                        );
                                        
                                        AdvertisementReceived?.Invoke(this, advertisement);
                                    }
                                }
                            }
                            else
                            {
                                Program.Log($"[SimpleBLE] Failed to get device info for index {i}: error {getResult}");
                            }
                        }
                        
                        // Clear the device list for next scan cycle
                        _discoveredDevices.Clear();
                    }
                    
                    // Scan stopped
                    NativeSimpleBLE.bt_scan_stop();
                    _isScanning = false;
                }
                catch (Exception ex)
                {
                    Program.Log($"[SimpleBLE] Error during scan: {ex.Message}");
                    _isScanning = false;
                    _continuousScanning = false;
                }
            });

            return new SimpleBLEScanResult(_isScanning);
        }

        private bool ApplyFilters(string deviceName, IBluetoothScanOptions options)
        {
            if (options.AcceptAllAdvertisements)
                return true;

            var filters = options.Filters.Cast<SimpleBLEScanFilter>().ToList();
            if (!filters.Any())
                return true;

            foreach (var filter in filters)
            {
                if (filter.AcceptAllDevices)
                    return true;

                if (filter.DeviceName != null && deviceName?.Contains(filter.DeviceName) == true)
                    return true;
            }

            return false;
        }

        public async Task<IReadOnlyList<IBluetoothDevice>> ScanForDevicesAsync(IBluetoothScanOptions options, CancellationToken cancellationToken)
        {
            var devices = new List<IBluetoothDevice>();
            var scanResult = await RequestScanAsync(options);
            
            if (scanResult != null)
            {
                await Task.Delay(options.Timeout, cancellationToken);
                await scanResult.StopAsync();
            }

            return devices;
        }
        
        public Task StopScanAsync()
        {
            _continuousScanning = false;
            if (_isScanning)
            {
                NativeSimpleBLE.bt_scan_stop();
                _isScanning = false;
            }
            return Task.CompletedTask;
        }
    }

    public class SimpleBLEProvider : IBluetoothProvider
    {
        private readonly SimpleBLEScanner _scanner;
        private bool _disposed = false;
        private bool _initialized = false;

        public SimpleBLEProvider()
        {
            _scanner = new SimpleBLEScanner();
            // Initialize immediately to catch errors early
            Initialize();
        }

        private void Initialize()
        {
            if (!_initialized)
            {
                try
                {
                    int result = NativeSimpleBLE.bt_init();
                    if (result != 0)
                    {
                        throw new Exception($"Failed to initialize Bluetooth: error {result}");
                    }
                    _initialized = true;
                }
                catch (DllNotFoundException ex)
                {
                    throw new Exception("SimpleBLE native library not found. Ensure bluetooth_wrapper.dll and simpleble-c.dll are in the application directory.", ex);
                }
                catch (EntryPointNotFoundException ex)
                {
                    throw new Exception("SimpleBLE function not found. The native library may be incompatible.", ex);
                }
            }
        }

        public IBluetoothScanner Scanner => _scanner;

        public bool IsAvailable
        {
            get
            {
                try
                {
                    return _initialized;
                }
                catch
                {
                    return false;
                }
            }
        }

        public Task<bool> GetAvailabilityAsync()
        {
            return Task.FromResult(IsAvailable);
        }

        public IBluetoothUuid CreateUuid(Guid guid) => new SimpleBLEUuid(guid);
        public IBluetoothUuid CreateUuid(string uuid) => new SimpleBLEUuid(uuid);
        public IBluetoothScanOptions CreateScanOptions() => new SimpleBLEScanOptions();
        public IBluetoothScanFilter CreateScanFilter() => new SimpleBLEScanFilter();

        public void Dispose()
        {
            if (!_disposed)
            {
                _scanner.StopScanAsync().Wait(1000);
                NativeSimpleBLE.bt_cleanup();
                _disposed = true;
            }
        }
    }
}
