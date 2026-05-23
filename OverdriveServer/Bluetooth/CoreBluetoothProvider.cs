#if MACOS
using CoreBluetooth;
using CoreFoundation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OverdriveServer.Bluetooth
{
	public class CoreBluetoothCentralManagerDelegate : CBCentralManagerDelegate {
		public event EventHandler? AvailabilityChanged;
		public event EventHandler<CoreBluetoothDiscoveredEventArgs>? PeripheralDiscovered;
		public event EventHandler<CBPeripheral>? PeripheralConnected;
		public event EventHandler<CBPeripheral>? PeripheralDisconnected;
		public event EventHandler<(CBPeripheral Peripheral, NSError? Error)>? PeripheralConnectionFailed;

		public override void UpdatedState(CBCentralManager central)
		{
			AvailabilityChanged?.Invoke(this, EventArgs.Empty);
		}

		public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
		{
			PeripheralDiscovered?.Invoke(this, new CoreBluetoothDiscoveredEventArgs(peripheral, advertisementData, RSSI));
		}

		public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
		{
			PeripheralConnected?.Invoke(this, peripheral);
		}

		public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
		{
			PeripheralConnectionFailed?.Invoke(this, (peripheral, error));
		}

		public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
		{
			PeripheralDisconnected?.Invoke(this, peripheral);
		}
	}

	public class CoreBluetoothPeripheralDelegate : CBPeripheralDelegate {
		public event EventHandler<(CBService Service, NSError? Error)>? ServicesDiscovered;
		public event EventHandler<(CBService Service, NSError? Error)>? CharacteristicsDiscovered;
		public event EventHandler<(CBCharacteristic Characteristic, NSError? Error)>? ValueUpdated;
		public event EventHandler<(CBCharacteristic Characteristic, NSError? Error)>? WriteCompleted;
		public event EventHandler<(CBCharacteristic Characteristic, NSError? Error)>? NotificationStateUpdated;

		public override void DiscoveredService(CBPeripheral peripheral, NSError? error)
		{
			if (peripheral.Services == null)
				return;

			foreach (var service in peripheral.Services)
			{
				ServicesDiscovered?.Invoke(this, (service, error));
			}
		}

		public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
		{
			CharacteristicsDiscovered?.Invoke(this, (service, error));
		}

		public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
		{
			ValueUpdated?.Invoke(this, (characteristic, error));
		}

		public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
		{
			WriteCompleted?.Invoke(this, (characteristic, error));
		}

		public override void UpdatedNotificationState(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
		{
			NotificationStateUpdated?.Invoke(this, (characteristic, error));
		}
	}

	public class CoreBluetoothDiscoveredEventArgs : EventArgs {
		public CoreBluetoothDiscoveredEventArgs(CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
		{
			Peripheral = peripheral;
			AdvertisementData = advertisementData;
			Rssi = rssi;
		}

		public CBPeripheral Peripheral { get; }
		public NSDictionary AdvertisementData { get; }
		public NSNumber Rssi { get; }
	}

	public class CoreBluetoothUuid : IBluetoothUuid {
		private readonly string _uuid;

		public CoreBluetoothUuid(Guid guid)
		{
			_uuid = guid.ToString();
		}

		public CoreBluetoothUuid(string uuid)
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

	public class CoreBluetoothScanFilter : IBluetoothScanFilter {
		public List<IBluetoothUuid> Services { get; } = new();
		public string? DeviceName { get; set; }
		public bool AcceptAllDevices { get; set; }
	}

	public class CoreBluetoothScanOptions : IBluetoothScanOptions {
		public List<IBluetoothScanFilter> Filters { get; } = new();
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
		public bool AcceptAllAdvertisements { get; set; }
	}

	public class CoreBluetoothAdvertisement : IBluetoothAdvertisement {
		public CoreBluetoothAdvertisement(IBluetoothDevice device, string? name, List<IBluetoothUuid> uuids, Dictionary<int, byte[]> manufacturerData, int rssi)
		{
			Device = device;
			Name = name;
			Uuids = uuids;
			ManufacturerData = manufacturerData;
			Rssi = rssi;
		}

		public IBluetoothDevice Device { get; }
		public string? Name { get; }
		public List<IBluetoothUuid> Uuids { get; }
		public Dictionary<int, byte[]> ManufacturerData { get; }
		public int Rssi { get; }
	}

	public class CoreBluetoothCharacteristicValueChangedEventArgs : ICharacteristicValueChangedEventArgs {
		public CoreBluetoothCharacteristicValueChangedEventArgs(byte[] value)
		{
			Value = value;
		}

		public byte[] Value { get; }
	}

	public class CoreBluetoothCharacteristic : IBluetoothCharacteristic {
		private readonly CBPeripheral _peripheral;
		private readonly CBCharacteristic _characteristic;
		private readonly CoreBluetoothPeripheralDelegate _delegate;
		private bool _notificationsEnabled;
		private bool _disposed;

		public CoreBluetoothCharacteristic(CBPeripheral peripheral, CBCharacteristic characteristic, CoreBluetoothPeripheralDelegate peripheralDelegate)
		{
			_peripheral = peripheral;
			_characteristic = characteristic;
			_delegate = peripheralDelegate;
			Uuid = new CoreBluetoothUuid(characteristic.UUID?.ToString() ?? string.Empty);
			Properties = ConvertProperties(characteristic.Properties);

			_delegate.ValueUpdated += OnValueUpdated;
		}

		public IBluetoothUuid Uuid { get; }
		public BluetoothCharacteristicProperties Properties { get; }

		public event EventHandler<ICharacteristicValueChangedEventArgs>? CharacteristicValueChanged;

		private void OnValueUpdated(object? sender, (CBCharacteristic Characteristic, NSError? Error) args)
		{
			if (args.Characteristic != _characteristic)
				return;

			if (!_notificationsEnabled)
				return;

			if (args.Error != null)
				return;

			var data = DataToBytes(args.Characteristic.Value);
			CharacteristicValueChanged?.Invoke(this, new CoreBluetoothCharacteristicValueChangedEventArgs(data));
		}

		public Task<byte[]> ReadValueAsync()
		{
			var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Handler(object? sender, (CBCharacteristic Characteristic, NSError? Error) args)
			{
				if (args.Characteristic != _characteristic)
					return;

				_delegate.ValueUpdated -= Handler;

				if (args.Error != null)
				{
					tcs.TrySetException(new InvalidOperationException($"Failed to read characteristic: {args.Error.LocalizedDescription}"));
					return;
				}

				var data = DataToBytes(args.Characteristic.Value);
				tcs.TrySetResult(data);
			}

			_delegate.ValueUpdated += Handler;
			_peripheral.ReadValue(_characteristic);

			return tcs.Task;
		}

		public Task WriteValueAsync(byte[] value, bool requireResponse = true)
		{
			if (!requireResponse)
			{
				_peripheral.WriteValue(NSData.FromArray(value), _characteristic, CBCharacteristicWriteType.WithoutResponse);
				return Task.CompletedTask;
			}

			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Handler(object? sender, (CBCharacteristic Characteristic, NSError? Error) args)
			{
				if (args.Characteristic != _characteristic)
					return;

				_delegate.WriteCompleted -= Handler;

				if (args.Error != null)
				{
					tcs.TrySetException(new InvalidOperationException($"Failed to write characteristic: {args.Error.LocalizedDescription}"));
					return;
				}

				tcs.TrySetResult(true);
			}

			_delegate.WriteCompleted += Handler;
			_peripheral.WriteValue(NSData.FromArray(value), _characteristic, CBCharacteristicWriteType.WithResponse);

			return tcs.Task;
		}

		public Task StartNotificationsAsync()
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Handler(object? sender, (CBCharacteristic Characteristic, NSError? Error) args)
			{
				if (args.Characteristic != _characteristic)
					return;

				_delegate.NotificationStateUpdated -= Handler;

				if (args.Error != null)
				{
					tcs.TrySetException(new InvalidOperationException($"Failed to start notifications: {args.Error.LocalizedDescription}"));
					return;
				}

				_notificationsEnabled = true;
				tcs.TrySetResult(true);
			}

			_delegate.NotificationStateUpdated += Handler;
			_peripheral.SetNotifyValue(true, _characteristic);

			return tcs.Task;
		}

		public Task StopNotificationsAsync()
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Handler(object? sender, (CBCharacteristic Characteristic, NSError? Error) args)
			{
				if (args.Characteristic != _characteristic)
					return;

				_delegate.NotificationStateUpdated -= Handler;

				if (args.Error != null)
				{
					tcs.TrySetException(new InvalidOperationException($"Failed to stop notifications: {args.Error.LocalizedDescription}"));
					return;
				}

				_notificationsEnabled = false;
				tcs.TrySetResult(true);
			}

			_delegate.NotificationStateUpdated += Handler;
			_peripheral.SetNotifyValue(false, _characteristic);

			return tcs.Task;
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_delegate.ValueUpdated -= OnValueUpdated;
			_disposed = true;
		}

		private static BluetoothCharacteristicProperties ConvertProperties(CBCharacteristicProperties properties)
		{
			var result = BluetoothCharacteristicProperties.None;

			if (properties.HasFlag(CBCharacteristicProperties.Read))
				result |= BluetoothCharacteristicProperties.Read;
			if (properties.HasFlag(CBCharacteristicProperties.Write))
				result |= BluetoothCharacteristicProperties.Write;
			if (properties.HasFlag(CBCharacteristicProperties.WriteWithoutResponse))
				result |= BluetoothCharacteristicProperties.WriteWithoutResponse;
			if (properties.HasFlag(CBCharacteristicProperties.Notify))
				result |= BluetoothCharacteristicProperties.Notify;
			if (properties.HasFlag(CBCharacteristicProperties.Indicate))
				result |= BluetoothCharacteristicProperties.Indicate;

			return result;
		}

		private static byte[] DataToBytes(NSData? data)
		{
			if (data == null || data.Length == 0)
				return Array.Empty<byte>();

			var bytes = new byte[data.Length];
			Marshal.Copy(data.Bytes, bytes, 0, bytes.Length);
			return bytes;
		}
	}

	public class CoreBluetoothService : IBluetoothService {
		private readonly CBPeripheral _peripheral;
		private readonly CBService _service;
		private readonly CoreBluetoothPeripheralDelegate _delegate;
		private readonly Dictionary<string, IBluetoothCharacteristic> _characteristics = new(StringComparer.OrdinalIgnoreCase);
		private bool _disposed;

		public CoreBluetoothService(CBPeripheral peripheral, CBService service, CoreBluetoothPeripheralDelegate peripheralDelegate)
		{
			_peripheral = peripheral;
			_service = service;
			_delegate = peripheralDelegate;
			Uuid = new CoreBluetoothUuid(service.UUID?.ToString() ?? string.Empty);
		}

		public IBluetoothUuid Uuid { get; }

		public async Task<IBluetoothCharacteristic> GetCharacteristicAsync(IBluetoothUuid characteristicUuid)
		{
			if (_characteristics.TryGetValue(characteristicUuid.Value, out var cached))
				return cached;

			var uuid = CBUUID.FromString(characteristicUuid.Value);
			await DiscoverCharacteristicsAsync(new[] { uuid });

			var characteristic = _service.Characteristics?.FirstOrDefault(c => string.Equals(c.UUID?.ToString(), characteristicUuid.Value, StringComparison.OrdinalIgnoreCase));
			if (characteristic == null)
				throw new InvalidOperationException($"Characteristic {characteristicUuid.Value} not found");

			var wrapper = new CoreBluetoothCharacteristic(_peripheral, characteristic, _delegate);
			_characteristics[characteristicUuid.Value] = wrapper;
			return wrapper;
		}

		public async Task<IReadOnlyList<IBluetoothCharacteristic>> GetCharacteristicsAsync()
		{
			await DiscoverCharacteristicsAsync(null);

			var results = new List<IBluetoothCharacteristic>();
			if (_service.Characteristics == null)
				return results.AsReadOnly();

			foreach (var characteristic in _service.Characteristics)
			{
				var key = characteristic.UUID?.ToString() ?? string.Empty;
				if (!_characteristics.TryGetValue(key, out var cached))
				{
					cached = new CoreBluetoothCharacteristic(_peripheral, characteristic, _delegate);
					_characteristics[key] = cached;
				}
				results.Add(cached);
			}

			return results.AsReadOnly();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			foreach (var characteristic in _characteristics.Values)
			{
				characteristic.Dispose();
			}
			_characteristics.Clear();
			_disposed = true;
		}

		private Task DiscoverCharacteristicsAsync(CBUUID[]? uuids)
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Handler(object? sender, (CBService Service, NSError? Error) args)
			{
				if (args.Service != _service)
					return;

				_delegate.CharacteristicsDiscovered -= Handler;

				if (args.Error != null)
				{
					tcs.TrySetException(new InvalidOperationException($"Failed to discover characteristics: {args.Error.LocalizedDescription}"));
					return;
				}

				tcs.TrySetResult(true);
			}

			_delegate.CharacteristicsDiscovered += Handler;
			_peripheral.DiscoverCharacteristics(uuids, _service);

			return tcs.Task;
		}
	}

	public class CoreBluetoothGattServer : IBluetoothGattServer {
		private readonly CBPeripheral _peripheral;
		private readonly CBCentralManager _centralManager;
		private readonly CoreBluetoothCentralManagerDelegate _centralDelegate;
		private readonly CoreBluetoothPeripheralDelegate _peripheralDelegate;
		private readonly Dictionary<string, IBluetoothService> _services = new(StringComparer.OrdinalIgnoreCase);
		private bool _disposed;

		public CoreBluetoothGattServer(CBPeripheral peripheral, CBCentralManager centralManager, CoreBluetoothCentralManagerDelegate centralDelegate, CoreBluetoothPeripheralDelegate peripheralDelegate)
		{
			_peripheral = peripheral;
			_centralManager = centralManager;
			_centralDelegate = centralDelegate;
			_peripheralDelegate = peripheralDelegate;

			_centralDelegate.PeripheralDisconnected += OnPeripheralDisconnected;
		}

		public bool IsConnected => _peripheral.State == CBPeripheralState.Connected;

		public event EventHandler? GattServerDisconnected;

		public async Task<IBluetoothService> GetPrimaryServiceAsync(IBluetoothUuid serviceUuid)
		{
			if (_services.TryGetValue(serviceUuid.Value, out var cached))
				return cached;

			var uuid = CBUUID.FromString(serviceUuid.Value);
			await DiscoverServicesAsync(new[] { uuid });

			var service = _peripheral.Services?.FirstOrDefault(s => string.Equals(s.UUID?.ToString(), serviceUuid.Value, StringComparison.OrdinalIgnoreCase));
			if (service == null)
				throw new InvalidOperationException($"Service {serviceUuid.Value} not found");

			var wrapper = new CoreBluetoothService(_peripheral, service, _peripheralDelegate);
			_services[serviceUuid.Value] = wrapper;
			return wrapper;
		}

		public Task ConnectAsync()
		{
			if (IsConnected)
				return Task.CompletedTask;

			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void ConnectedHandler(object? sender, CBPeripheral peripheral)
			{
				if (peripheral != _peripheral)
					return;

				_centralDelegate.PeripheralConnected -= ConnectedHandler;
				_centralDelegate.PeripheralConnectionFailed -= FailedHandler;
				tcs.TrySetResult(true);
			}

			void FailedHandler(object? sender, (CBPeripheral Peripheral, NSError? Error) args)
			{
				if (args.Peripheral != _peripheral)
					return;

				_centralDelegate.PeripheralConnected -= ConnectedHandler;
				_centralDelegate.PeripheralConnectionFailed -= FailedHandler;

				var message = args.Error?.LocalizedDescription ?? "Unknown connection error";
				tcs.TrySetException(new InvalidOperationException($"Failed to connect: {message}"));
			}

			_centralDelegate.PeripheralConnected += ConnectedHandler;
			_centralDelegate.PeripheralConnectionFailed += FailedHandler;
			_centralManager.ConnectPeripheral(_peripheral);

			return tcs.Task;
		}

		public void Disconnect()
		{
			if (_peripheral.State == CBPeripheralState.Connected)
			{
				_centralManager.CancelPeripheralConnection(_peripheral);
			}
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_centralDelegate.PeripheralDisconnected -= OnPeripheralDisconnected;
			foreach (var service in _services.Values)
			{
				service.Dispose();
			}
			_services.Clear();
			_disposed = true;
		}

		private void OnPeripheralDisconnected(object? sender, CBPeripheral peripheral)
		{
			if (peripheral != _peripheral)
				return;

			GattServerDisconnected?.Invoke(this, EventArgs.Empty);
		}

		private Task DiscoverServicesAsync(CBUUID[]? uuids)
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Handler(object? sender, (CBService Service, NSError? Error) args)
			{
				if (args.Error != null)
				{
					_peripheralDelegate.ServicesDiscovered -= Handler;
					tcs.TrySetException(new InvalidOperationException($"Failed to discover services: {args.Error.LocalizedDescription}"));
					return;
				}

				if (args.Service != null && (uuids == null || uuids.Any(u => args.Service.UUID.Equals(u))))
				{
					_peripheralDelegate.ServicesDiscovered -= Handler;
					tcs.TrySetResult(true);
				}
			}

			_peripheralDelegate.ServicesDiscovered += Handler;
			_peripheral.DiscoverServices(uuids);

			return tcs.Task;
		}
	}

	public class CoreBluetoothDevice : IBluetoothDevice {
		private readonly CBCentralManager _centralManager;
		private readonly CoreBluetoothCentralManagerDelegate _centralDelegate;
		private readonly CoreBluetoothGattServer _gattServer;
		private bool _disposed;

		public CoreBluetoothPeripheralDelegate PeripheralDelegate { get; }

		public CoreBluetoothDevice(CBPeripheral peripheral, CBCentralManager centralManager, CoreBluetoothCentralManagerDelegate centralDelegate)
		{
			Peripheral = peripheral;
			_centralManager = centralManager;
			_centralDelegate = centralDelegate;

			PeripheralDelegate = new CoreBluetoothPeripheralDelegate();
			Peripheral.Delegate = PeripheralDelegate;

			_gattServer = new CoreBluetoothGattServer(Peripheral, _centralManager, _centralDelegate, PeripheralDelegate);
			Id = Peripheral.Identifier?.AsString() ?? Guid.NewGuid().ToString();
			Name = Peripheral.Name;
		}

		public CBPeripheral Peripheral { get; }

		public string Id { get; }
		public string? Name { get; }
		public IBluetoothGattServer Gatt => _gattServer;

		public void Dispose()
		{
			if (_disposed)
				return;

			_gattServer.Dispose();
			_disposed = true;
		}
	}

	public class CoreBluetoothScanResult : IBluetoothScanResult {
		private readonly CoreBluetoothScanner _scanner;
		private bool _isScanning;

		public CoreBluetoothScanResult(CoreBluetoothScanner scanner)
		{
			_scanner = scanner;
			_isScanning = true;
		}

		public bool IsScanning => _isScanning;

		public Task StopAsync()
		{
			if (_isScanning)
			{
				_scanner.StopScanpublic();
				_isScanning = false;
			}

			return Task.CompletedTask;
		}
	}

	public class CoreBluetoothScanner : IBluetoothScanner {
		private readonly CBCentralManager _centralManager;
		private readonly CoreBluetoothCentralManagerDelegate _centralDelegate;
		private readonly DispatchQueue _centralQueue;
		private readonly Dictionary<NSUuid, CoreBluetoothDevice> _devices = new();
		private bool _isScanning;
		private IBluetoothScanOptions? _activeOptions;

		public CoreBluetoothScanner()
		{
			_centralDelegate = new CoreBluetoothCentralManagerDelegate();
			_centralQueue = new DispatchQueue("OverdriveServer.CoreBluetooth");
			_centralManager = new CBCentralManager(_centralDelegate, _centralQueue);

			_centralDelegate.PeripheralDiscovered += OnPeripheralDiscovered;
			_centralDelegate.AvailabilityChanged += (s, e) => AvailabilityChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool IsScanning => _isScanning;
		public CBManagerState CurrentState => _centralManager.State;

		public event EventHandler<IBluetoothAdvertisement>? AdvertisementReceived;
		public event EventHandler? AvailabilityChanged;

		public async Task<IBluetoothScanResult?> RequestScanAsync(IBluetoothScanOptions options)
		{
			if (_isScanning)
				StopScanpublic();

			var poweredOn = await WaitForPoweredOnAsync(options.Timeout);
			if (!poweredOn)
			{
				Program.Log("[CoreBluetooth] Bluetooth not powered on yet; starting scan anyway.", true);
			}
			_activeOptions = options;
			var serviceUuids = BuildServiceUuids(options);
			var allowDuplicates = options.AcceptAllAdvertisements;
			NSDictionary? scanOptions = null;

			if (allowDuplicates)
			{
				scanOptions = NSDictionary.FromObjectAndKey(NSNumber.FromBoolean(true), CBCentralManager.ScanOptionAllowDuplicatesKey);
			}

			_centralManager.ScanForPeripherals(serviceUuids, scanOptions);
			_isScanning = true;

			return new CoreBluetoothScanResult(this);
		}

		public async Task<IReadOnlyList<IBluetoothDevice>> ScanForDevicesAsync(IBluetoothScanOptions options, CancellationToken cancellationToken)
		{
			var devices = new List<IBluetoothDevice>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			void Handler(object? sender, IBluetoothAdvertisement advertisement)
			{
				if (advertisement.Device == null)
					return;

				if (seen.Add(advertisement.Device.Id))
					devices.Add(advertisement.Device);
			}

			AdvertisementReceived += Handler;

			try
			{
				await RequestScanAsync(options);
				using var timeout = new CancellationTokenSource(options.Timeout);
				using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
				await Task.Delay(options.Timeout, linked.Token);
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				AdvertisementReceived -= Handler;
				await StopScanAsync();
			}

			return devices.AsReadOnly();
		}

		public Task StopScanAsync()
		{
			StopScanpublic();
			return Task.CompletedTask;
		}

		public void StopScanpublic()
		{
			if (_isScanning)
			{
				_centralManager.StopScan();
				_isScanning = false;
			}
		}

		private void OnPeripheralDiscovered(object? sender, CoreBluetoothDiscoveredEventArgs args)
		{
			var peripheral = args.Peripheral;
			if (peripheral == null)
				return;

			if (!_devices.TryGetValue(peripheral.Identifier, out var device))
			{
				device = new CoreBluetoothDevice(peripheral, _centralManager, _centralDelegate);
				_devices[peripheral.Identifier] = device;
			}

			var (name, uuids, manufacturerData) = ParseAdvertisement(args.AdvertisementData);

			if (!MatchesFilters(name, uuids, _activeOptions))
				return;

			var advertisement = new CoreBluetoothAdvertisement(
				device,
				name,
				uuids,
				manufacturerData,
				args.Rssi?.Int32Value ?? 0
			);

			AdvertisementReceived?.Invoke(this, advertisement);
		}

		private static CBUUID[]? BuildServiceUuids(IBluetoothScanOptions options)
		{
			if (options.AcceptAllAdvertisements)
				return null;

			var uuids = new List<CBUUID>();
			foreach (var filter in options.Filters)
			{
				foreach (var service in filter.Services)
				{
					uuids.Add(CBUUID.FromString(service.Value));
				}
			}

			return uuids.Count == 0 ? null : uuids.ToArray();
		}

		private static bool MatchesFilters(string? name, List<IBluetoothUuid> serviceUuids, IBluetoothScanOptions? options)
		{
			if (options == null || options.AcceptAllAdvertisements)
				return true;

			if (options.Filters.Count == 0)
				return true;

			foreach (var filter in options.Filters)
			{
				if (filter.AcceptAllDevices)
					return true;

				if (!string.IsNullOrEmpty(filter.DeviceName) && name?.Contains(filter.DeviceName) == true)
					return true;

				if (filter.Services.Count > 0 && serviceUuids.Any(u => filter.Services.Any(f => f.Equals(u))))
					return true;
			}

			return false;
		}

		private static (string? Name, List<IBluetoothUuid> ServiceUuids, Dictionary<int, byte[]> ManufacturerData) ParseAdvertisement(NSDictionary advertisementData)
		{
			string? name = null;
			var serviceUuids = new List<IBluetoothUuid>();
			var manufacturerData = new Dictionary<int, byte[]>();

			if (advertisementData.ContainsKey(CBAdvertisement.DataLocalNameKey))
			{
				name = advertisementData[CBAdvertisement.DataLocalNameKey]?.ToString();
			}

			if (advertisementData.ContainsKey(CBAdvertisement.DataServiceUUIDsKey) && advertisementData[CBAdvertisement.DataServiceUUIDsKey] is NSArray uuidArray)
			{
				for (nuint i = 0; i < uuidArray.Count; i++)
				{
					if (uuidArray.GetItem<CBUUID>(i) is CBUUID uuid)
					{
						serviceUuids.Add(new CoreBluetoothUuid(uuid.ToString()));
					}
				}
			}

			if (advertisementData.ContainsKey(CBAdvertisement.DataManufacturerDataKey) && advertisementData[CBAdvertisement.DataManufacturerDataKey] is NSData mfgData)
			{
				var bytes = DataToBytes(mfgData);
				if (bytes.Length >= 2)
				{
					int companyId = bytes[0] | (bytes[1] << 8);
					var payload = new byte[bytes.Length - 2];
					Array.Copy(bytes, 2, payload, 0, payload.Length);
					manufacturerData[companyId] = payload;
				}
			}

			return (name, serviceUuids, manufacturerData);
		}

		private static byte[] DataToBytes(NSData data)
		{
			if (data.Length == 0)
				return Array.Empty<byte>();

			var bytes = new byte[data.Length];
			Marshal.Copy(data.Bytes, bytes, 0, bytes.Length);
			return bytes;
		}


		private Task<bool> WaitForPoweredOnAsync(TimeSpan timeout)
		{
			if (_centralManager.State == CBManagerState.PoweredOn)
				return Task.FromResult(true);

			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var cts = new CancellationTokenSource(timeout);

			void Handler(object? sender, EventArgs args)
			{
				if (_centralManager.State != CBManagerState.PoweredOn)
					return;

				AvailabilityChanged -= Handler;
				tcs.TrySetResult(true);
			}

			AvailabilityChanged += Handler;
			cts.Token.Register(() => {
				AvailabilityChanged -= Handler;
				tcs.TrySetResult(false);
			});

			return tcs.Task.ContinueWith(task => {
				cts.Dispose();
				return task;
			}).Unwrap();
		}
	}

	public class CoreBluetoothProvider : IBluetoothProvider {
		private readonly CoreBluetoothScanner _scanner;
		private bool _disposed;

		public CoreBluetoothProvider()
		{
			_scanner = new CoreBluetoothScanner();
		}

		public IBluetoothScanner Scanner => _scanner;

		public bool IsAvailable => _scanner.CurrentState == CBManagerState.PoweredOn;

		public Task<bool> GetAvailabilityAsync()
		{
			var state = _scanner.CurrentState;
			if (state != CBManagerState.Unknown && state != CBManagerState.Resetting)
				return Task.FromResult(state == CBManagerState.PoweredOn);

			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Handler(object? sender, EventArgs args)
			{
				var current = _scanner.CurrentState;
				if (current == CBManagerState.Unknown || current == CBManagerState.Resetting)
					return;

				_scanner.AvailabilityChanged -= Handler;
				tcs.TrySetResult(current == CBManagerState.PoweredOn);
			}

			_scanner.AvailabilityChanged += Handler;
			return tcs.Task;
		}

		public IBluetoothUuid CreateUuid(Guid guid) => new CoreBluetoothUuid(guid);
		public IBluetoothUuid CreateUuid(string uuid) => new CoreBluetoothUuid(uuid);
		public IBluetoothScanOptions CreateScanOptions() => new CoreBluetoothScanOptions();
		public IBluetoothScanFilter CreateScanFilter() => new CoreBluetoothScanFilter();

		public void Dispose()
		{
			if (_disposed)
				return;

			_scanner.StopScanAsync().Wait(500);
			_disposed = true;
		}
	}
}
#endif