using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Bluetooth;
using Newtonsoft.Json;
using static OverdriveServer.Definitions;
using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;
using AsyncAwaitBestPractices;

namespace OverdriveServer {
    class CarSystem {
        public static readonly BluetoothUuid ServiceID = BluetoothUuid.FromGuid(new Guid("BE15BEEF-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid ReadID = BluetoothUuid.FromGuid(new Guid("BE15BEE0-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid WriteID = BluetoothUuid.FromGuid(new Guid("BE15BEE1-6186-407E-8381-0BD89C4D8DF4"));
        Dictionary<string, Car> cars = new Dictionary<string, Car>();
        public Car? GetCar(string id){ return cars.ContainsKey(id) ? cars[id] : null; }
        public Car[] GetCarsOnTrack(bool all = false){
            List<Car> toSend = new List<Car>();
            foreach(Car car in cars.Values){
                if(all && !car.data.charging){ toSend.Add(car); }
                else if(!all && car.data.onTrack){ toSend.Add(car); }
            }
            return toSend.ToArray();
        }
        public async Task ConnectToCarAsync(BluetoothDevice carDevice, int model, int rssi){
            try{
                FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
                Save s = await fs.LoadFile($"{carDevice.Id}.dat");
                ModelName modelName = (ModelName)model;
                string name = $"{modelName} ({carDevice.Id})";
                int speedBalance = 0;
                bool hadConfig = false;
                if(s != null) {
                    hadConfig = true;
                    name = s.GetVar("name", name); 
                    speedBalance = s.GetVar("speedBalance", 0);
                }

                await carDevice.Gatt.ConnectAsync();
                if(carDevice.Gatt.IsConnected){
                    Program.Log($"[0] Connected to car [{modelName}] {name}, Rssi: {rssi}");
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
                    await fs.SaveFile($"{carDevice.Id}.dat", s); //update/create config file
                    GattService service = await car.device.Gatt.GetPrimaryServiceAsync(ServiceID);
                    if(service == null){ return; }
                    //subscribe to characteristic changed event on read characteristic
                    
                    var readCharacteristic = await service.GetCharacteristicAsync(ReadID);
                    if(readCharacteristic == null){ 
                        IReadOnlyList<GattCharacteristic> chars = await service.GetCharacteristicsAsync();
                        
                        foreach(GattCharacteristic c in chars){
                            bool isRead = c.Properties.HasFlag(GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify);
                            if(isRead){
                                Console.WriteLine($"Standard read characteristic missing, trying: {c.Uuid}");
                                readCharacteristic = c; break; //found the read characteristic
                            }
                        }
                        if(readCharacteristic == null){ 
                            Program.Log($"[0] No read characteristic found for {name}"); 
                            Program.bluetoothInterface.RemoveCarCheck(carDevice.Id); 
                            carDevice.Gatt.Disconnect();
                            car.Dispose(); //dispose of the car
                            return;
                        }
                    }
                    cars.Add(carDevice.Id, car);
                    Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
                    CheckCarConnection(car);
                    readCharacteristic.CharacteristicValueChanged += (sender, args) => { CarCharacteristicChanged(sender, args, car); };
                    await readCharacteristic.StartNotificationsAsync();
                    await car.EnableSDKMode(true);
                    await car.RequestCarVersion();
                    await car.RequestCarBattery();
                    await Task.Delay(500);
                    Program.UtilLog($"{MSG_CAR_CONNECTED}:{car.id}:{car.name}");
                }
                else{
                    Program.Log($"[0] Failed to connect to car {name} [{modelName}]");
                    Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
                }
            }catch(Exception e){
                Program.Log($"[0] Error connecting to car [{model}] {carDevice.Name}  {e}");
                return;
            }
        }
        async Task CheckCarConnection(Car car){ //check if car is still connected every 5 seconds
            while(car.device.Gatt.IsConnected){ await Task.Delay(5000);  }
            Program.Log($"[0] Car disconnected {car.name}");
            Program.UtilLog($"-2:{car.id}");
            cars.Remove(car.device.Id);
        }
        
        static void CarCharacteristicChanged(object sender, GattCharacteristicValueChangedEventArgs args, Car car){
            Program.messageManager.ParseMessage(args.Value, car);
        }
        public string CarDataAsJson(){
            CarData[] carData = new CarData[cars.Count];
            int i = 0;
            foreach(Car car in cars.Values){
                carData[i] = car.data; i++;
            }
            return JsonConvert.SerializeObject(carData);
        }
        public int CarCount(){ return cars.Count; }
        public async Task UpdateConfigs(){
            FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
            foreach(Car car in cars.Values){
                Save s = await fs.LoadFile($"{car.device.Id}.dat");
                if(s != null){
                    string name = s.GetVar("name", $"Unknown Car({car.device.Id})");
                    int speedBalance = s.GetVar("speedBalance", 0);
                    car.UpdateConfigs(name, speedBalance);
                }
            }
        }
    }
    public class Car{
        public string name;
        public string id;
        public BluetoothDevice device;
        int speedBalance = 0;
        float requestedOffset = 0;
        //used by custom tracking
        public int lastPositionID = 0; public bool lastReversed = false;
        //
        bool V4_MODE = false;
        public CarData data;
        GattCharacteristic writeCharacteristic;
        bool hasWriteCharacteristic = false;
        public Car(string name, string id, BluetoothDevice device, int speedBalance = 0){
            this.name = name;
            this.id = id;
            this.device = device;
            this.speedBalance = speedBalance;
            this.data = new CarData(name, id, 0);
            GetWriteCharacteristic();
        }
        public void UpdateConfigs(string name, int speedBalance){
            this.name = name;
            this.speedBalance = speedBalance;
        }
        async Task GetWriteCharacteristic(){
            var service = await device.Gatt.GetPrimaryServiceAsync(CarSystem.ServiceID);
            writeCharacteristic = await service.GetCharacteristicAsync(CarSystem.WriteID);
            hasWriteCharacteristic = true;
        }
        public async Task RequestCarDisconnect(){
            byte[] data = new byte[]{0x01, SEND_CAR_DISCONNECT};
            await WriteToCarAsync(data, true);
        }
        public async Task EnableSDKMode(bool enable = true){
            //4 bytes 0x03 0x90 0x01 0x01
            byte enabled = enable ? (byte)0x01 : (byte)0x00;
            byte[] data = new byte[]{0x03, SEND_SDK_MODE, enabled, 0x01};
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarSpeed(int speed, int accel = 1000){
            //only balance speed if not 0
            if(speedBalance != 0 && speed != 0){  speed = Math.Clamp(speed + speedBalance, 0, 1200); }
            byte[] data = V4_MODE ? new byte[8] : new byte[7];
            data[0] = V4_MODE ? (byte)0x07 : (byte)0x06;
            data[1] = SEND_CAR_SPEED_UPDATE;
            //speed as int16
            data[2] = (byte)(speed & 0xFF);
            data[3] = (byte)((speed >> 8) & 0xFF);
            //accel as int16
            data[4] = (byte)(accel & 0xFF);
            data[5] = (byte)((accel >> 8) & 0xFF);
            //respect track piece speed limits
            data[6] = 0x01;
            if(V4_MODE){ data[7] = 0x01; } //drive without scanned track? v4 only
            await WriteToCarAsync(data);
            this.data.speed = speed;
        }
        public async Task SetCarTrackCenter(float offset){
            byte[] data = new byte[6];
            data[0] = 0x05;
            data[1] = SEND_TRACK_CENTER_UPDATE;
            BitConverter.GetBytes((float)offset).CopyTo(data, 2); 
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarLane(float lane, int horizontalSpeedmm = 100, int horizontalAccelmm = 1000){ // Offset value (?? 68,23,-23,68 seem to be lane values 1-4)
            byte[] data = new byte[12];
            data[0] = 0x0B;
            data[1] = SEND_CAR_LANE_CHANGE;
            //horizontal speed as int16
            data[2] = (byte)(horizontalSpeedmm & 0xFF);
            data[3] = (byte)((horizontalSpeedmm >> 8) & 0xFF);
            //horizontal accel as int16
            data[4] = (byte)(horizontalAccelmm & 0xFF);
            data[5] = (byte)((horizontalAccelmm >> 8) & 0xFF);
            //lane as float
            BitConverter.GetBytes((float)lane).CopyTo(data, 6);
            await WriteToCarAsync(data, true);
            requestedOffset = (int)lane;
        }
        public void UpdateValues(int locationID, int segmentID, float offset, int bits, bool reversed){
            lastPositionID = segmentID; lastReversed = reversed;
            data.offset = offset;

            float correctedOffset = Program.location.CorrectOffset(segmentID, locationID, offset, bits, reversed);
            if(Math.Abs(offset - correctedOffset) > 4){
                SetCarTrackCenter(correctedOffset);
                offset = correctedOffset;
            }
            if(Math.Abs(offset - requestedOffset) > 4){
                SetCarLane(requestedOffset, 100, 500);
            }
        }
        public async Task RequestCarBattery(){
            byte[] data = new byte[]{0x01, SEND_BATTERY_REQUEST};
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarLightsPattern(float r, float g, float b){
            byte[] data = new byte[18];

            int rByte =  r == 1 ? 0x01 : 0x00;
            int gByte =  g == 1 ? 0x01 : 0x00;
            int bByte =  b == 1 ? 0x01 : 0x00;
            
            data[0] = 0x11; //size
            data[1] = SEND_LIGHTS_PATTERN_UPDATE; //id (set lights) 51

            data[2] = 0x03;//  channel_count c  ENGINE maybe ??? Headlights, Brakelights, Frontlights, Enginelights ??

            data[3] = 0x00; // start of anki_vehicle_light_config num.1 of chNEL LIGHT_RED
            data[4] = 0x02; //??? EFFECT_STEADY
            data[5] = 1; //red    maybe start 
            data[6] = 30; //red2?? maybe end
            data[7] = 0x01; //?? cycles_per_10_sec

            data[8] = 0x03; // start of anki_vehicle_light_config num.2 of chNEL LIGHT_GREEN
            data[9] = 0x02; // EFFECT_STEADY
            data[10] = 1; //green    maybe start
            data[11] = 15; //green2?? maybe end
            data[12] = 0x01; //cycles_per_10_sec

            data[13] = 0x02; //start of anki_vehicle_light_config num.3 of chNEL LIGHT_BLUE
            data[14] = 0x02; //EFFECT_STEADY
            data[15] = 1; //blue    maybe start
            data[16] = 10; //blue2?? maybe end
            data[17] = 0x01; //cycles_per_10_sec
            await WriteToCarAsync(data, true);
        }
        public async Task UTurn(){
            byte[] data = new byte[4];
            data[0] = 0x03; //size
            data[1] = SEND_CAR_UTURN; //id 50
            data[2] = 0x03; //0x00 Not Turn, 0x01 Left, 0x02 Right, 0x03 U-Turn, 0x04 Jump U-turn
            data[3] = 0x01; //0x00 Turn Now, 0x01 Turn at new Track Piece
            await WriteToCarAsync(data, true);
        }
        public async Task RequestCarVersion(){
            byte[] data = new byte[]{0x01, SEND_VERSION_REQUEST}; //12385 and higher should use V4 mode
            await WriteToCarAsync(data, true);
        }
        public void SetCarSoftwareVersion(short version){
            if(version >= 12385){ V4_MODE = true; } //v4 mode
        }

        async Task WriteToCarAsync(byte[] data, bool response = false){
            if(!hasWriteCharacteristic){ return; }
            try{ //send someth
                if(response){ await writeCharacteristic.WriteValueWithResponseAsync(data); }
                else{ await writeCharacteristic.WriteValueWithoutResponseAsync(data); }
            }catch{}
        }
    }
}