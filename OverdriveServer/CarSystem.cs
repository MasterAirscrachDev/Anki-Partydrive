using InTheHand.Bluetooth;
using Newtonsoft.Json;
using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;
using AsyncAwaitBestPractices;

namespace OverdriveServer
{
    public class CarSystem
    {
        public static readonly BluetoothUuid ServiceID = BluetoothUuid.FromGuid(new Guid("BE15BEEF-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid ReadID = BluetoothUuid.FromGuid(new Guid("BE15BEE0-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid WriteID = BluetoothUuid.FromGuid(new Guid("BE15BEE1-6186-407E-8381-0BD89C4D8DF4"));
        Dictionary<string, Car> cars = new Dictionary<string, Car>();
        Dictionary<string, (float, int)> rememberedSpeedOffset = new Dictionary<string, (float, int)>();
        public Car? GetCar(string id) { return cars.ContainsKey(id) ? cars[id] : null; }
        public Car[] GetCarsOnTrack(bool all = false)
        {
            List<Car> toSend = new List<Car>();
            foreach (Car car in cars.Values)
            {
                if (all && !car.data.charging) { toSend.Add(car); }
                else if (!all && car.data.onTrack) { toSend.Add(car); }
            }
            return toSend.ToArray();
        }
        public async Task RequestAllDisconnect()
        {
            foreach (Car car in cars.Values)
            {
                if (car.device.Gatt.IsConnected) { await car.RequestCarDisconnect(); } //request disconnect
            }
            Console.WriteLine($"Disconnected {cars.Count} cars");
        }
        public async Task ConnectToCarAsync(BluetoothDevice carDevice, int model, int rssi)
        {
            try
            {
                FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
                Save s = await fs.LoadFile($"{carDevice.Id}.dat");
                ModelName modelName = (ModelName)model;
                string name = $"{modelName} ({carDevice.Id})";
                int speedBalance = 0;
                bool hadConfig = false;
                if (s != null)
                {
                    hadConfig = true;
                    name = s.GetVar("name", name);
                    speedBalance = s.GetVar("speedBalance", 0);
                }

                await carDevice.Gatt.ConnectAsync();
                if (carDevice.Gatt.IsConnected)
                {
                    Program.Log($"[0] Connected to car [{modelName}] {name}, Rssi: {rssi}");
                    Car car = new Car(name, carDevice.Id, carDevice, speedBalance, model);
                    if (rememberedSpeedOffset.ContainsKey(carDevice.Id))
                    {
                        (float offset, int speed) = rememberedSpeedOffset[carDevice.Id];
                        car.SetSpeedOffset(offset, speed); //set the speed and offset to the remembered values
                    }
                    if (!hadConfig)
                    {
                        s = new Save();
                        s.SetVar("name", name);
                        s.SetVar("speedBalance", 0);
                    }
                    await fs.SaveFile($"{carDevice.Id}.dat", s); //update/create config file
                    cars.Add(carDevice.Id, car);
                    Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
                    await Task.Delay(500);
                    Program.UtilLog($"{MSG_CAR_CONNECTED}:{car.id}:{car.name}");
                }
                else
                {
                    Program.Log($"[0] Failed to connect to car {name} [{modelName}]");
                    Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
                }
            }
            catch (Exception e)
            {
                Program.Log($"[0] Error connecting to car [{model}] {carDevice.Name}  {e}");
                return;
            }
        }

        internal void CarCharacteristicChanged(object sender, GattCharacteristicValueChangedEventArgs args, Car car)
        {
            Program.messageManager.ParseMessage(args.Value, car);
        }
        internal void RemoveCar(string id, (float offset, int speed) speedOffset)
        {
            if (cars.ContainsKey(id))
            {
                cars.Remove(id);
                if (speedOffset != default)
                {
                    rememberedSpeedOffset[id] = speedOffset; //remember the speed and offset
                }
            }
        }

        public string CarDataAsJson()
        {
            CarData[] carData = new CarData[cars.Count];
            int i = 0;
            foreach (Car car in cars.Values) { carData[i] = car.data; i++; }
            return JsonConvert.SerializeObject(carData);
        }
        public int CarCount() { return cars.Count; }
        public async Task UpdateConfigs()
        {
            FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
            foreach (Car car in cars.Values)
            {
                Save s = await fs.LoadFile($"{car.device.Id}.dat");
                if (s != null)
                {
                    string name = s.GetVar("name", $"Unknown Car({car.device.Id})");
                    int speedBalance = s.GetVar("speedBalance", 0);
                    car.UpdateConfigs(name, speedBalance);
                }
            }
        }
    }
    public class Car
    {
        public string name, id;
        public BluetoothDevice device;
        int speedBalance = 0, requestedSpeedMMPS = 0;
        float requestedOffsetMM = 0;
        //used by custom tracking =======
        public int lastPositionID = 0; public bool lastReversed = false;
        //==============================
        bool V4_MODE = false, hasCharacteristics = false;
        public bool hasRoadNetwork = false, transmitLocked = false; //prevent other data when flashing
        public CarData data;
        GattCharacteristic writeCharacteristic, readCharacteristic;
        byte[] firmwareData;
        public Car(string name, string id, BluetoothDevice device, int speedBalance, int model)
        {
            this.name = name;
            this.id = id;
            this.device = device;
            this.speedBalance = speedBalance;
            this.data = new CarData(name, id, model);
            OnCarCreated().SafeFireAndForget(); //create the car and setup the characteristics
        }
        public void SetSpeedOffset(float offset, int speed)
        {
            requestedOffsetMM = offset; requestedSpeedMMPS = speed;
        }
        public void UpdateConfigs(string name, int speedBalance)
        {
            this.name = name;
            this.speedBalance = speedBalance;
        }
        async Task OnCarCreated()
        {
            await SetupCharacteristics();
            await EnableSDKMode(true);
            await RequestCarVersion();
            await RequestCarBattery();
            device.GattServerDisconnected += (sender, args) => { Dispose(true); };
            //What if it was ourple
            LightData[] lights = [
                new LightData{ channel = NetStructures.LightChannel.RED, effect = NetStructures.LightEffect.THROB, startStrength = 0, endStrength = 100, cyclesPer10Seconds = 3 },
                new LightData{ channel = NetStructures.LightChannel.GREEN, effect = NetStructures.LightEffect.STEADY, startStrength = 0, endStrength = 0, cyclesPer10Seconds = 0 },
                new LightData{ channel = NetStructures.LightChannel.BLUE, effect = NetStructures.LightEffect.THROB, startStrength = 0, endStrength = 100, cyclesPer10Seconds = 3 },
            ];
            await SetLights(lights); //set the lights to a default pattern
        }
        async Task SetupCharacteristics()
        {
            var service = await device.Gatt.GetPrimaryServiceAsync(CarSystem.ServiceID);
            writeCharacteristic = await service.GetCharacteristicAsync(CarSystem.WriteID);
            readCharacteristic = await service.GetCharacteristicAsync(CarSystem.ReadID);

            if (readCharacteristic == null)
            {
                IReadOnlyList<GattCharacteristic> chars = await service.GetCharacteristicsAsync();
                foreach (GattCharacteristic c in chars)
                {
                    bool isRead = c.Properties.HasFlag(GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify);
                    if (isRead) { Console.WriteLine($"Standard read characteristic missing, trying: {c.Uuid}"); readCharacteristic = c; break; } //found the read characteristic
                }
                if (readCharacteristic == null)
                {
                    Program.Log($"[0] No read characteristic found for {name}");
                    Dispose(); //dispose of the car
                    return;
                }
            }
            readCharacteristic.CharacteristicValueChanged += (sender, args) => { Program.carSystem.CarCharacteristicChanged(sender, args, this); };
            await readCharacteristic.StartNotificationsAsync(); //start notifications

            hasCharacteristics = true;
        }
        public async Task RequestCarDisconnect()
        {
            await Task.Delay(50);
            Dispose();
        }
        /// <summary>
        /// Atttempts to disconnect the car and dispose of it.
        /// </summary>
        public void Dispose(bool alertImplicit = false)
        {
            if (alertImplicit)
            {
                try
                {
                    Program.Log($"[0] Car disconnected {name}");
                    Program.UtilLog($"{MSG_CAR_DISCONNECTED}:{id}");
                }
                catch (Exception e)
                {
                    Program.Log($"Error alerting of car disconnect: {e}");
                }
            }
            if (device != null)
            {
                device.GattServerDisconnected -= (sender, args) => { Dispose(true); }; //remove the event handler
                if (!alertImplicit) { device.Gatt.Disconnect(); } //disconnect the car
                readCharacteristic.CharacteristicValueChanged -= (sender, args) => { Program.carSystem.CarCharacteristicChanged(sender, args, this); }; //remove the event handler
                device.Dispose(); //dispose of the device
            }
            Program.carSystem.RemoveCar(id, (requestedOffsetMM, requestedSpeedMMPS)); //remove the car from the system
        }
        public async Task EnableSDKMode(bool enable = true)
        {
            //Console.WriteLine($"[0] Enabling SDK mode {enable} for {name}");
            //4 bytes 0x03 0x90 0x01 0x01
            byte enabled = enable ? (byte)0x01 : (byte)0x00;
            byte[] data = [0x03, (byte)MSG.SEND_SDK_MODE, enabled, 0x01];
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarSpeed(int speed, int accel = 1000, bool internalCall = true, bool requiresTrack = true)
        {
            if (requiresTrack && !data.onTrack) { return; } //if not on track, dont set speed
            //only balance speed if not 0
            if (speedBalance != 0 && speed != 0) { speed = Math.Clamp(speed + speedBalance, 0, 3000); }
            await WriteToCarAsync(Formatter.SEND_SetCarSpeed((short)speed, (short)accel, true, V4_MODE), true);
            data.speedMMPS = speed;
            requestedSpeedMMPS = speed;
            if (!internalCall)
            {
                Program.UtilLog($"{MSG_CAR_SPEED_UPDATE}:{id}:{speed}:{speed}");
            }
        }
        public void GoIfNotGoing(bool onTrack)
        {
            if (!data.onTrack && onTrack && requestedSpeedMMPS != 0)
            {
                SetCarSpeed(requestedSpeedMMPS, 1000, true, false);
            }
        }
        public async Task TriggerStopOnTransition()
        {
            await WriteToCarAsync(Formatter.SEND_StopOnTranstion(), true);
        }
        public async Task SetCarTrackCenter(float offset, bool adjustment = false)
        {
            await WriteToCarAsync(Formatter.SEND_SetOffsetFromRoadCenter(offset, adjustment), true);
        }
        public async Task SetCarLane(float lane, int horizontalSpeedmm = 100, int horizontalAccelmm = 1000)
        {
            // Find the closest lane using LINQ - more concise and efficient
            float laneValue = Tracks.Lanes.OrderBy(l => Math.Abs(l - lane)).First();

            await WriteToCarAsync(Formatter.SEND_LaneChange((ushort)horizontalSpeedmm, (ushort)horizontalAccelmm, laneValue, 0x00, 0x00), true);
            requestedOffsetMM = lane;
        }
        public void UpdateValues(int locationID, int segmentID, float offsetMM, int bits, bool reversed)
        {
            lastPositionID = segmentID; lastReversed = reversed;

            float correctedOffset = Program.location.CorrectOffsetFast(segmentID, locationID, offsetMM, reversed);
            if (Math.Abs(offsetMM - correctedOffset) > 4) { SetCarTrackCenter(correctedOffset, false); }
            if (Math.Abs(correctedOffset - requestedOffsetMM) > 4) { SetCarLane(requestedOffsetMM, 100, 500); } //true if we are on the wrong lane
            data.offsetMM = correctedOffset;
        }
        public async Task RequestCarBattery()
        {
            await WriteToCarAsync(Formatter.SEND_BatteryVoltageRequest(), true);
        }
        public async Task SetEngineRGB(int r, int g, int b)
        { //Simple Call
            byte[] data = new byte[18];
            data[0] = 0x11; //size
            data[1] = (byte)MSG.SEND_SET_LIGHTS_PATTERN; //id (set lights) 51
            data[2] = 0x03;//Channel Count
            for (int i = 0; i < 3; i++)
            {
                LightChannel channel = i == 0 ? LightChannel.RED : i == 1 ? LightChannel.GREEN : LightChannel.BLUE;
                int col = i == 0 ? r : i == 1 ? g : b;
                data[(i * 5) + 3] = (byte)channel; //channel
                data[(i * 5) + 4] = (byte)LightEffect.STEADY; //effect
                data[(i * 5) + 5] = (byte)col;
                data[(i * 5) + 6] = 0x00; //end strength
                data[(i * 5) + 7] = 0x00; //cycles_per_10_sec
            }
            await WriteToCarAsync(data, true);
        }
        public async Task SetLights(LightData[] lights)
        { //Complex Call
            if (lights.Length == 0 || lights.Length > 3) { return; } //if no lights, dont send anything
            byte[] data = new byte[3 + (lights.Length * 5)];
            data[0] = (byte)(2 + (lights.Length * 5)); //size
            data[1] = (byte)MSG.SEND_SET_LIGHTS_PATTERN; //id (set lights) 51
            data[2] = (byte)lights.Length; //channel count
            for (int i = 0; i < lights.Length; i++)
            {
                //convert the lightdata light channel index to the CarProtocolV2.LightChannel enum
                data[(i * 5) + 3] = (byte)(LightChannel)lights[i].channel; //channel
                data[(i * 5) + 4] = (byte)(LightEffect)lights[i].effect; //effect
                data[(i * 5) + 5] = (byte)lights[i].startStrength; //start strength
                data[(i * 5) + 6] = (byte)lights[i].endStrength; //end strength
                data[(i * 5) + 7] = (byte)lights[i].cyclesPer10Seconds; //cycles per 10 sec
            }
            await WriteToCarAsync(data, true);
        }
        public async Task UTurn()
        {
            await WriteToCarAsync(Formatter.SEND_OpenLoopTurn(OpenLoopTurnType.U_TURN, OpenLoopTriggerCondition.WAIT_FOR_TRANSISSION), true);
        }
        public async Task RequestCarVersion()
        {
            byte[] data = [0x01, (byte)MSG.SEND_REQUEST_CAR_VERSION]; //12385 and higher should use V4 mode
            await WriteToCarAsync(data, true);
        }
        public void SetCarSoftwareVersion(short version)
        {
            if (version >= 12385) { V4_MODE = true; Program.Log($"V4 Compatibility Enabled for {name}"); } //v4 mode
        }
        async Task WriteToCarAsync(byte[] data, bool response = false, bool bypassLock = false)
        {
            if (!hasCharacteristics || (transmitLocked && !bypassLock)) { return; }
            try
            { //send someth
                if (response) { await writeCharacteristic.WriteValueWithResponseAsync(data); }
                else { await writeCharacteristic.WriteValueWithoutResponseAsync(data); }
            }
            catch { }
        }
    }
}