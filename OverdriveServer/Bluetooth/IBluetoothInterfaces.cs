using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OverdriveServer.Bluetooth
{
    // Base Bluetooth UUID abstraction
    public interface IBluetoothUuid : IEquatable<IBluetoothUuid>
    {
        string Value { get; }
        Guid ToGuid();
    }

    // Scan filter options
    public interface IBluetoothScanFilter
    {
        List<IBluetoothUuid> Services { get; }
        string? DeviceName { get; set; }
        bool AcceptAllDevices { get; set; }
    }

    // Scan request options
    public interface IBluetoothScanOptions
    {
        List<IBluetoothScanFilter> Filters { get; }
        TimeSpan Timeout { get; set; }
        bool AcceptAllAdvertisements { get; set; }
    }

    // Characteristic properties
    [Flags]
    public enum BluetoothCharacteristicProperties
    {
        None = 0,
        Read = 1,
        Write = 2,
        WriteWithoutResponse = 4,
        Notify = 8,
        Indicate = 16
    }

    // Advertisement event data
    public interface IBluetoothAdvertisement
    {
        IBluetoothDevice Device { get; }
        string? Name { get; }
        List<IBluetoothUuid> Uuids { get; }
        Dictionary<int, byte[]> ManufacturerData { get; }
        int Rssi { get; }
    }

    // Characteristic value changed event args
    public interface ICharacteristicValueChangedEventArgs
    {
        byte[] Value { get; }
    }

    // GATT Characteristic
    public interface IBluetoothCharacteristic : IDisposable
    {
        IBluetoothUuid Uuid { get; }
        BluetoothCharacteristicProperties Properties { get; }
        
        event EventHandler<ICharacteristicValueChangedEventArgs>? CharacteristicValueChanged;
        
        Task<byte[]> ReadValueAsync();
        Task WriteValueAsync(byte[] value, bool requireResponse = true);
        Task StartNotificationsAsync();
        Task StopNotificationsAsync();
    }

    // GATT Service
    public interface IBluetoothService : IDisposable
    {
        IBluetoothUuid Uuid { get; }
        
        Task<IBluetoothCharacteristic> GetCharacteristicAsync(IBluetoothUuid characteristicUuid);
        Task<IReadOnlyList<IBluetoothCharacteristic>> GetCharacteristicsAsync();
    }

    // GATT Server
    public interface IBluetoothGattServer : IDisposable
    {
        bool IsConnected { get; }
        
        event EventHandler? GattServerDisconnected;
        
        Task<IBluetoothService> GetPrimaryServiceAsync(IBluetoothUuid serviceUuid);
        Task ConnectAsync();
        void Disconnect();
    }

    // Bluetooth Device
    public interface IBluetoothDevice : IDisposable
    {
        string Id { get; }
        string? Name { get; }
        IBluetoothGattServer Gatt { get; }
    }

    // Scanner interface
    public interface IBluetoothScanner
    {
        bool IsScanning { get; }
        
        event EventHandler<IBluetoothAdvertisement>? AdvertisementReceived;
        event EventHandler? AvailabilityChanged;
        
        Task<IBluetoothScanResult?> RequestScanAsync(IBluetoothScanOptions options);
        Task<IReadOnlyList<IBluetoothDevice>> ScanForDevicesAsync(IBluetoothScanOptions options, CancellationToken cancellationToken);
        Task StopScanAsync();
    }

    // Scan result
    public interface IBluetoothScanResult
    {
        bool IsScanning { get; }
        Task StopAsync();
    }

    // Main Bluetooth Provider interface
    public interface IBluetoothProvider : IDisposable
    {
        IBluetoothScanner Scanner { get; }
        
        // Factory methods for creating abstraction objects
        IBluetoothUuid CreateUuid(Guid guid);
        IBluetoothUuid CreateUuid(string uuid);
        IBluetoothScanOptions CreateScanOptions();
        IBluetoothScanFilter CreateScanFilter();
        
        // Check availability
        bool IsAvailable { get; }
        Task<bool> GetAvailabilityAsync();
    }
}