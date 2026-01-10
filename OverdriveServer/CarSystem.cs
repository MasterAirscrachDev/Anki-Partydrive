using OverdriveServer.Bluetooth;
using Newtonsoft.Json;
using static OverdriveServer.NetStructures;
using OverdriveBLEProtocol;
using static OverdriveServer.NetStructures.UtilityMessages;
using AsyncAwaitBestPractices;
using System.Collections.Concurrent;

namespace OverdriveServer {
    public class CarSystem {
#region Variables
        private readonly IBluetoothProvider _bluetoothProvider;
        public readonly IBluetoothUuid ServiceID;
        public readonly IBluetoothUuid ReadID;
        public readonly IBluetoothUuid WriteID;
        
        public CarSystem(IBluetoothProvider bluetoothProvider)
        {
            _bluetoothProvider = bluetoothProvider ?? throw new ArgumentNullException(nameof(bluetoothProvider));
            ServiceID = _bluetoothProvider.CreateUuid(new Guid("BE15BEEF-6186-407E-8381-0BD89C4D8DF4"));
            ReadID = _bluetoothProvider.CreateUuid(new Guid("BE15BEE0-6186-407E-8381-0BD89C4D8DF4"));
            WriteID = _bluetoothProvider.CreateUuid(new Guid("BE15BEE1-6186-407E-8381-0BD89C4D8DF4"));
        }
        ConcurrentDictionary<string, Car> cars = new ConcurrentDictionary<string, Car>();
        ConcurrentDictionary<string, AvailableCar> availableCars = new ConcurrentDictionary<string, AvailableCar>();
        ConcurrentDictionary<string, (float,int)> rememberedSpeedOffset = new ConcurrentDictionary<string, (float,int)>();
        ConcurrentDictionary<string, byte> desiredCars = new ConcurrentDictionary<string, byte>(); // Cars that should auto-reconnect if found (using dict as a set)
        public Car? GetCar(string id){ return cars.ContainsKey(id) ? cars[id] : null; }
        public AvailableCarData[] GetAvailableCars(){ return availableCars.Values.Select(car => new AvailableCarData(car.id, car.model, car.lastSeen)).ToArray(); }
#endregion
#region  Available Cars
        public void AddAvailableCar(IBluetoothDevice device, uint model){
            ModelName modelName = (ModelName)model;
            //are we connected to this car already?
            if(cars.ContainsKey(device.Id)){
                //Program.Log($"WTF {modelName} ({device.Id}) already connected, ignoring available car", true);
                if(!desiredCars.ContainsKey(device.Id)){
                    if(cars.TryGetValue(device.Id, out var car)){
                        car.Dispose(); //shouldnt happen but just in case, disconnect the car
                    }
                }
                return; //already connected
            }
            
            if(availableCars.TryGetValue(device.Id, out var existingCar)) { // Update existing available car
                existingCar.lastSeen = DateTime.Now;
                existingCar.device = device; // Update device reference
                existingCar.model = model; // Update model in case it changed
            } else { // Add new available car
                availableCars[device.Id] = new AvailableCar(device.Id, model, device);
                Program.Log($"BLE: found {modelName} ({device.Id})", true);
            }
            if(desiredCars.ContainsKey(device.Id)) { // Check if this car is in our desired cars list for auto-reconnect
                Program.Log($"BLE: Reconnecting to {modelName} ({device.Id})");
                ConnectToAvailableCar(device.Id).SafeFireAndForget();
                availableCars.TryRemove(device.Id, out _); // Remove from available cars if present
            }
            //if we have a connected car with this id, remove it from available cars
            if(cars.ContainsKey(device.Id) && availableCars.ContainsKey(device.Id)){
                availableCars.TryRemove(device.Id, out _);
            }
        }
        void DropOldAvalibleCars()
        {
            //check if any cars are older than 15 seconds and remove them
            List<string> toRemove = new List<string>();
            foreach(var car in availableCars.Values){
                if(car == null) { continue; }
                if((DateTime.Now - car.lastSeen).TotalSeconds > 15){
                    toRemove.Add(car.id);
                }
            }
            foreach(string id in toRemove){
                if(availableCars.TryRemove(id, out AvailableCar? availableCar) && availableCar != null){
                    Program.Log($"BLE: dropped {(ModelName)availableCar.model} ({id})", true);
                }
            }
        }
        public void ClearAvailableCars(){
            availableCars.Clear();
        }
        public async Task<bool> ConnectToAvailableCar(string carId){
            DropOldAvalibleCars();
            if(!availableCars.TryGetValue(carId, out AvailableCar? availableCar) || availableCar == null) { 
                Program.Log($"Car {carId} not found in available cars list");
                return false; 
            }
            try {
                bool connnected = await ConnectToCarAsync(availableCar.device, availableCar.model);
                return connnected;
            } catch (Exception ex) {
                Program.Log($"Exception during connection to {carId}: {ex.Message}");
                return false;
            }
        }
#endregion
#region Connections to Cars
        public async Task<bool> DisconnectCar(string carId){
            if(!cars.TryGetValue(carId, out Car? car) || car == null) {
                Program.Log($"[13] Car {carId} not found in connected cars list");
                return false;
            }
            
            RemoveFromDesiredCars(carId); // Remove from desired cars list - this was an intentional disconnect
            
            await car.RequestCarDisconnect();
            Program.Log($"[13] Disconnection requested for car {car.name}");
            availableCars.TryRemove(carId, out _); //remove from available cars if present (really shouldnt be)
            return true;
        }
        public Car[] GetCarsOnTrack(bool all = false){
            List<Car> toSend = new List<Car>();
            foreach(Car car in cars.Values){
                if(all && !car.data.charging){ toSend.Add(car); }
                else if(!all && car.data.onTrack){ toSend.Add(car); }
            }
            return toSend.ToArray();
        }
        public async Task RequestAllDisconnect(){
            foreach(Car car in cars.Values){
                if(car.device != null && car.device.Gatt.IsConnected){ await car.RequestCarDisconnect(); } //request disconnect
            }
            Console.WriteLine($"BLE: droppped {cars.Count} cars");
        }
        public async Task<bool> ConnectToCarAsync(IBluetoothDevice carDevice, uint model){
            try{
                if(carDevice == null || string.IsNullOrEmpty(carDevice.Id)){
                    Program.Log($"Invalid car device or ID");
                    return false;
                }
                FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
                Save s = await fs.LoadFile($"{carDevice.Id}.dat");
                ModelName modelName = (ModelName)model;
                string name = $"{modelName} ({carDevice.Id.Substring(0, 3)})";
                int speedBalance = 0;
                bool hadConfig = false;
                if(s != null) {
                    hadConfig = true;
                    name = s.GetVar("name", name); 
                    speedBalance = s.GetVar("speedBalance", 0);
                }

                await carDevice.Gatt.ConnectAsync();
                if(carDevice.Gatt.IsConnected){
                    Program.Log($"Connected to car [{modelName}] {name}");

                    Car car = new Car(name, carDevice.Id, carDevice, speedBalance, model);
                    if(rememberedSpeedOffset.ContainsKey(carDevice.Id)){
                        (float offset, int speed) = rememberedSpeedOffset[carDevice.Id];
                        car.SetSpeedOffset(offset, speed); //set the speed and offset to the remembered values
                    }
                    if(!hadConfig){
                        s = new Save();
                        s.SetVar("name", name);
                        s.SetVar("speedBalance", 0);
                    }
                    cars.TryAdd(carDevice.Id, car);
                    availableCars.TryRemove(carDevice.Id, out _); //remove from available cars if present
                    desiredCars.TryAdd(carDevice.Id, 0); // Add to desired cars for auto-reconnect
                    await fs.SaveFile($"{carDevice.Id}.dat", s); //update/create config file
                    await Task.Delay(500);
                    Program.UtilLog($"{MSG_CAR_CONNECTED}:{car.id}:{car.name}");
                    return true;
                }
                else{
                    Program.Log($"Failed to connect to car {name} [{modelName}]");
                    return false;
                }
            }catch(Exception e){
                Program.Log($"Error connecting to car [{model}] {carDevice.Name}  {e}");
                return false;
            }
        }
        
        internal void CarCharacteristicChanged(object sender, ICharacteristicValueChangedEventArgs args, Car car){
            Program.messageManager.ParseMessage(args.Value, car);
        }
        internal void RemoveCar(string id, (float offset, int speed) speedOffset){
            if(speedOffset != default){
                rememberedSpeedOffset[id] = speedOffset; //remember the speed and offset
            }
            cars.TryRemove(id, out _);
        }
        
        public void RemoveFromDesiredCars(string carId){
            desiredCars.TryRemove(carId, out _);
            //Program.Log($"Removed {carId} from desired cars list", true);
        }
#endregion
#region Data Export
        public string CarDataAsJson(){
            //if there are any null cars, remove them (shouldnt happen but just in case)
            if(cars.Any(c => c.Value == null)){
                List<string> toRemove = new List<string>();
                foreach(var car in cars){
                    if(car.Value == null){ toRemove.Add(car.Key); }
                }
                foreach(string id in toRemove){
                    cars.TryRemove(id, out _);
                }
            }
            CarData[] carData = new CarData[cars.Count];
            int i = 0;
            foreach(Car car in cars.Values){ 
                    carData[i] = car.data; i++; 
            }
            return JsonConvert.SerializeObject(carData);
        }
        public string AvailableCarDataAsJson(){
            DropOldAvalibleCars(); //remove old cars properly
            AvailableCarData[] availableCarData = new AvailableCarData[availableCars.Count];
            int i = 0;
            foreach(AvailableCar car in availableCars.Values){
                availableCarData[i] = new AvailableCarData(car.id, car.model, car.lastSeen);
                i++; 
            }
            return JsonConvert.SerializeObject(availableCarData);
        }
        public int CarCount(){ return cars.Count; }
        public int GetAvailableCarsCount(){ return availableCars.Count; }
#endregion
#region Configuration Management
        public async Task UpdateConfigs(){
            FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
            foreach(Car car in cars.Values){
                if(car.device != null){
                    Save s = await fs.LoadFile($"{car.device.Id}.dat");
                    if(s != null){
                        string name = s.GetVar("name", $"Unknown Car({car.device.Id})");
                        int speedBalance = s.GetVar("speedBalance", 0);
                        car.UpdateConfigs(name, speedBalance);
                    }
                }
            }
        }
        public async Task UpdateConfigForCar(string id, string newName, int speedBalanceChange)
        {
            if(cars.TryGetValue(id, out Car? car) && car != null){
                FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
                Save s = await fs.LoadFile($"{id}.dat");
                if(s != null){
                    s.SetVar("name", newName);
                    int currentSpeedBalance = s.GetVar("speedBalance", 0);
                    int newSpeedBalance = currentSpeedBalance + speedBalanceChange;
                    s.SetVar("speedBalance", newSpeedBalance);
                    await fs.SaveFile($"{id}.dat", s);
                    car.UpdateConfigs(newName, newSpeedBalance);
                    Program.Log($"Updated config for car {car.name}: new name '{newName}', speed balance change {speedBalanceChange}", true);
                }
            } else {
                Program.Log($"Car {id} not found for config update", true);
            }
        }
    }
#endregion
    #region Car
    public class Car{
    #region Variables
        public string name, id;
        public IBluetoothDevice? device;
        int speedBalance = 0, requestedSpeedMMPS = 0;
        float requestedOffsetMM = 0;
        //used by custom tracking =======
        public int lastSegmentID = 0; public bool lastReversed = false;
        //==============================
        bool hasCharacteristics = false;
        SoftwareVersion softwareVersion = SoftwareVersion.Legacy;
        public bool hasRoadNetwork = false, transmitLocked = false; //prevent other data when flashing
        public CarData data;
        IBluetoothCharacteristic? writeCharacteristic, readCharacteristic;
        private Queue<(uint length, uint offset)> chunkQueue = new Queue<(uint, uint)>(); // Queue for chunk requests
        private bool isProcessingChunks = false; // Flag to track if we're currently processing chunks
        public Car(string name, string id, IBluetoothDevice device, int speedBalance, uint model){
            this.name = name;
            this.id = id;
            this.device = device;
            this.speedBalance = speedBalance;
            this.data = new CarData(name, id, model);
            OnCarCreated().SafeFireAndForget(); //create the car and setup the characteristics
        }
#endregion
#region Initialization
        async Task OnCarCreated(){
            await SetupCharacteristics();
            if(softwareVersion != SoftwareVersion.Dev || softwareVersion != SoftwareVersion.Party1){
                byte[] data = Formatter.SEND_EnableSDKMode();
                await WriteToCarAsync(data, true); //enable SDK mode
            }else{
                byte[] message = new byte[3];
                message[0] = 2;
                message[1] = 31;
                message[2] = 4;
                await WriteToCarAsync(message, true); //set the car system state to normal
            }
            await RequestCarVersion();
            await RequestCarBattery();  
            if(device != null){
                device.Gatt.GattServerDisconnected += (sender, args) => { Dispose(true); };
            }
            //What if it was ourple
            LightData[] lights = [
                new LightData{ channel = NetStructures.LightChannel.RED, effect = NetStructures.LightEffect.THROB, startStrength = 3, endStrength = 14, cyclesPer10Seconds = 5 },
                new LightData{ channel = NetStructures.LightChannel.GREEN, effect = NetStructures.LightEffect.STEADY, startStrength = 0, endStrength = 0, cyclesPer10Seconds = 0 },
                new LightData{ channel = NetStructures.LightChannel.BLUE, effect = NetStructures.LightEffect.THROB, startStrength = 3, endStrength = 14, cyclesPer10Seconds = 5 },
            ];
            await SetLights(lights); //set the lights to a default pattern
            //Testing stuff below

            //End of testing
        }
        async Task SetupCharacteristics(){
            if(device == null) return;
            
            var service = await device.Gatt.GetPrimaryServiceAsync(Program.carSystem.ServiceID);
            writeCharacteristic = await service.GetCharacteristicAsync(Program.carSystem.WriteID);
            readCharacteristic = await service.GetCharacteristicAsync(Program.carSystem.ReadID);
            
            if(readCharacteristic == null){ 
                IReadOnlyList<IBluetoothCharacteristic> chars = await service.GetCharacteristicsAsync();
                foreach(IBluetoothCharacteristic c in chars){
                    bool isRead = c.Properties.HasFlag(BluetoothCharacteristicProperties.Read | BluetoothCharacteristicProperties.Notify);
                    if(isRead){ Console.WriteLine($"Standard read characteristic missing, trying: {c.Uuid.Value}"); readCharacteristic = c; break;  } //found the read characteristic
                }
                if(readCharacteristic == null){ 
                    Program.Log($"No read characteristic found for {name}"); 
                    Dispose(); //dispose of the car
                    return;
                }
            }
            if(readCharacteristic != null){
                readCharacteristic.CharacteristicValueChanged += (sender, args) => { Program.carSystem.CarCharacteristicChanged(sender!, args, this); };
                await readCharacteristic.StartNotificationsAsync(); //start notifications
            }
            hasCharacteristics = true;
        }
#endregion
        public void SetSpeedOffset(float offset, int speed){
            requestedOffsetMM = offset; requestedSpeedMMPS = speed;
        }
        public void UpdateConfigs(string name, int speedBalance){
            this.name = name;
            this.speedBalance = speedBalance;
        }

        public bool IsConnected(){
            return device != null && device.Gatt.IsConnected && hasCharacteristics;
        }
        /// <summary>
        /// Reverses the effect of speed balancing to get the unbalanced speed (Match O with I)
        /// </summary>
        /// <param name="speed">The balanced speed (Car Unique)</param>
        /// <returns>The unbalanced speed</returns>
        public int GetUnbalancedSpeed(int speed){
            return Math.Clamp(speed - speedBalance, 0, 2400);
        }
        public async Task RequestCarBattery(){
            await WriteToCarAsync(Formatter.SEND_BatteryVoltageRequest(), true);
        }
#region Disconnection
        public async Task RequestCarDisconnect(){
            await WriteToCarAsync(Formatter.SEND_BLEDisconnect(), false);
            await Task.Delay(100);
            Dispose();
        }
        /// <summary>
        /// Atttempts to disconnect the car and dispose of it.
        /// </summary>
        public void Dispose(bool alertImplicit = false){
            if(alertImplicit){
                try{
                    Program.Log($"[13] Car disconnected {name}");
                    Program.UtilLog($"{MSG_CAR_DISCONNECTED}:{id}");
                }catch(Exception e){
                    Program.Log($"Error alerting of car disconnect: {e}");
                }
            }
            CleanupDevice();
            if(!alertImplicit){ 
                if(device != null && device.Gatt.IsConnected){ 
                    device.Gatt.Disconnect(); 
                } 
            } //disconnect the car
            Program.carSystem.RemoveCar(id, (requestedOffsetMM, requestedSpeedMMPS)); //remove the car from the system
        }

        private void CleanupDevice(bool keepFlash = false){
            if(device != null){ 
                hasCharacteristics = false;
                device.Gatt.GattServerDisconnected -= (sender, args) => { Dispose(true); }; //remove the event handler
                if(readCharacteristic != null){
                    readCharacteristic.CharacteristicValueChanged -= (sender, args) => { Program.carSystem.CarCharacteristicChanged(sender!, args, this); }; //remove the event handler
                }
                device.Dispose(); //dispose of the device
                device = null;
            }
            if(!keepFlash){
                // Clear the chunk queue when device is cleaned up
                isProcessingChunks = false;
                chunkQueue.Clear();
            }
        }
#endregion
#region Movement
        public async Task SetCarSpeed(int speed, int accel = 1000, bool internalCall = true, bool requiresTrack = true){
            if(requiresTrack && !data.onTrack){ return; } //if not on track, dont set speed
            //only balance speed if not 0, and skip if Party4 firmware with party signature
            //bool skipBalancing = (softwareVersion == SoftwareVersion.Party4 && gameFlashInfo != null && await gameFlashInfo.HasPartySignature());
            bool skipBalancing = false; //disable this logic until gameflash reading is more reliable
            if(speedBalance != 0 && speed != 0 && !skipBalancing){ speed = Math.Clamp(speed + speedBalance, 0, 2400); }
            await WriteToCarAsync(Formatter.SEND_SetCarSpeed((short)speed, (short)accel, true, softwareVersion == SoftwareVersion.V4), true);
            data.speedMMPS = speed;
            requestedSpeedMMPS = speed;
            //Program.Log($"Car {name} speed set to {speed} mm/s with accel {accel} mm/s²", true);
            if(!internalCall){
                Program.UtilLog($"{MSG_CAR_SPEED_UPDATE}:{id}:{speed}:{speed}");
            }
        }
        public async Task UTurn(){
            await WriteToCarAsync(Formatter.SEND_OpenLoopTurn(OpenLoopTurnType.U_TURN, OpenLoopTrigger.WAIT_FOR_SEGMENT), true);
        }
        public async Task SetCarLane(float lane, int horizontalSpeedmm = 100, int horizontalAccelmm = 1000){
            // Find the closest lane using LINQ - more concise and efficient
            float laneValue = Tracks.Lanes.OrderBy(l => Math.Abs(l - lane)).First();
            
            await WriteToCarAsync(Formatter.SEND_LaneChange((ushort)horizontalSpeedmm, (ushort)horizontalAccelmm, laneValue, 0x00, 0x00), true);
            requestedOffsetMM = lane;
        }
#endregion
#region Movement Extentions
        public void UpdateTrackingValues(int locationID, int segmentID, float offsetMM, int bits, bool reversed){
            // This is ONLY triggered if a segment was correctly identified
            lastSegmentID = segmentID; 
            lastReversed = reversed;


            float correctedOffset = Program.location.CorrectOffsetFast(segmentID, locationID, offsetMM, reversed);
            if(Math.Abs(offsetMM - correctedOffset) > 7){
                SetCarTrackCenter(correctedOffset, false);
                //Console.WriteLine($"[0] || Car {name} correcting offset to {correctedOffset}mm on segment {segmentID}, thought: {offsetMM}mm, target: {requestedOffsetMM}mm");
            }
            if(Math.Abs(correctedOffset - requestedOffsetMM) > 7){ 
                SetCarLane(requestedOffsetMM, 100, 500); 
                // if(Math.Abs(correctedOffset - requestedOffsetMM) > 10){
                //     Console.WriteLine($"[0] | Car {name} adjusting to lane {requestedOffsetMM}mm on segment {segmentID}, thought: {offsetMM}mm, target: {requestedOffsetMM}mm");
                // }
            } //true if we are on the wrong lane
            data.offsetMM = correctedOffset;
        }
        public void GoIfNotGoing(bool onTrack){
            if(!data.onTrack && onTrack && requestedSpeedMMPS != 0){
                SetCarSpeed(requestedSpeedMMPS, 1000, true, false);
            }
        }
        public async Task TriggerStopOnTransition(){
            await WriteToCarAsync(Formatter.SEND_StopOnTranstion(), true);
        }
        public async Task SetCarTrackCenter(float offset, bool adjustment = false){
            await WriteToCarAsync(Formatter.SEND_SetOffsetFromRoadCenter(offset, adjustment), true);
        }
#endregion
#region Lights
        public async Task SetEngineRGB(int r, int g, int b){ //Simple Call
            byte[] data = new byte[18];
            data[0] = (byte)(2 + (3 * 5)); //size
            data[1] = (byte)MSG.SEND_SET_LIGHTS_PATTERN; //id (set lights) 51
            data[2] = 0x03;//Channel Count
            for(int i = 0; i < 3; i++){
                OverdriveBLEProtocol.LightChannel channel = i == 0 ? OverdriveBLEProtocol.LightChannel.RED : i == 1 ? OverdriveBLEProtocol.LightChannel.GREEN : OverdriveBLEProtocol.LightChannel.BLUE;
                int col = i == 0 ? r : i == 1 ? g : b;
                data[(i * 5) + 3] = (byte)channel; //channel
                data[(i * 5) + 4] = (byte)OverdriveBLEProtocol.LightEffect.FADE; //effect
                data[(i * 5) + 5] = (byte)col;
                data[(i * 5) + 6] = (byte)col; //end strength
                data[(i * 5) + 7] = 1; //cycles_per_10_sec
            }
            await WriteToCarAsync(data, true);
        }
        public async Task SetLights(LightData[] lights){ //Complex Call
            if(lights.Length == 0 || lights.Length > 3){ return; } //if no lights, dont send anything
            byte[] data = new byte[3 + (lights.Length * 5)];
            data[0] = (byte)(2 + (lights.Length * 5)); //size
            data[1] = (byte)MSG.SEND_SET_LIGHTS_PATTERN; //id (set lights) 51
            data[2] = (byte)lights.Length; //channel count
            for(int i = 0; i < lights.Length; i++){
                //convert the lightdata light channel index to the CarProtocolV2.LightChannel enum
                data[(i * 5) + 3] = (byte)(OverdriveBLEProtocol.LightChannel)lights[i].channel; //channel
                data[(i * 5) + 4] = (byte)(OverdriveBLEProtocol.LightEffect)lights[i].effect; //effect
                data[(i * 5) + 5] = (byte)lights[i].startStrength; //start strength
                data[(i * 5) + 6] = (byte)lights[i].endStrength; //end strength
                data[(i * 5) + 7] = (byte)lights[i].cyclesPer10Seconds; //cycles per 10 sec
            }
            await WriteToCarAsync(data, true);
        }
#endregion
#region Car Version & Model
        public async Task RequestCarVersion(){
            byte[] data = [0x01, (byte)MSG.SEND_REQUEST_CAR_VERSION];
            await WriteToCarAsync(data, true);
        }
        public void SetCarSoftwareVersion(short version){
            if(version == (short)SoftwareVersion.V4 || version == (short)SoftwareVersion.V4alt){
                softwareVersion = SoftwareVersion.V4;
                Program.Log($"V4 Compatibility Enabled for {name}");
            } else if(version == (short)SoftwareVersion.Dev){
                softwareVersion = SoftwareVersion.Dev;
                Program.Log($"Dev V6P Compatibility Enabled for {name}");
            } else if(version == (short)SoftwareVersion.Party1){
                softwareVersion = SoftwareVersion.Party1;
                Program.Log($"Partydrive Features Enabled for {name}");
                data.hasPFeatures = true;
            } else if(version == (short)SoftwareVersion.Party4){
                softwareVersion = SoftwareVersion.Party4;
                data.hasPFeatures = true;
                Program.Log($"BETA Partydrive Features Enabled for {name}");
            } else{
                softwareVersion = SoftwareVersion.Legacy;
                Program.Log($"Legacy Compatibility Enabled for {name} ({version})");
            }
        }
        public async Task UpdateCarModel(uint model){
            if(this.softwareVersion != SoftwareVersion.Dev && this.softwareVersion != SoftwareVersion.Party1 && this.softwareVersion != SoftwareVersion.Party4){
                Program.Log($"[UpdateCarModel] This operation requires newer firmware", true);
                return;
            }
            if(model == data.model){ return; } //if the model is the same, dont update
            if(model > 255){ //1byte model max
                Program.Log($"[UpdateCarModel] Invalid model {model}", true);
                return;
            }
            await WriteToCarAsync(Formatter.SEND_DEV_UpdateModel(model), true);
        }
#endregion
        public async Task WriteToCarAsync(byte[] data, bool response = false, bool bypassLock = false){
            if(!hasCharacteristics || (transmitLocked && !bypassLock) || writeCharacteristic == null){ return; }
            try{ //send someth
                if(response){ await writeCharacteristic.WriteValueAsync(data, true); }
                else{ await writeCharacteristic.WriteValueAsync(data, false); }
            }catch{}
        }

        enum SoftwareVersion{
            Legacy,
            V4 = 12385, V4alt = 12386,
            Party1 = 12411, Party4 = 12412,
            Dev = 3387
        }
    }
#endregion
    public class AvailableCar{
        public string id;
        public uint model;
        public IBluetoothDevice device;
        public DateTime lastSeen;
        public AvailableCar(string id, uint model, IBluetoothDevice device){
            this.id = id;
            this.model = model;
            this.device = device;
            this.lastSeen = DateTime.Now;
        }
    }
}