using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Bluetooth;
using Newtonsoft.Json;
using static OverdriveServer.Definitions;
using static OverdriveServer.NetStructures;

namespace OverdriveServer {
    class CarSystem {
        public static readonly BluetoothUuid ServiceID = BluetoothUuid.FromGuid(new Guid("BE15BEEF-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid ReadID = BluetoothUuid.FromGuid(new Guid("BE15BEE0-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid WriteID = BluetoothUuid.FromGuid(new Guid("BE15BEE1-6186-407E-8381-0BD89C4D8DF4"));
        List<Car> cars = new List<Car>();
        public Car? GetCar(string id){ return cars.Find(car => car.id == id); }
        public Car[] GetCarsOffCharge(){
            List<Car> carsOffCharge = new List<Car>();
            foreach(Car car in cars){
                if(!car.data.charging){ carsOffCharge.Add(car); }
            }
            return carsOffCharge.ToArray();
        }
        public async Task ConnectToCarAsync(BluetoothDevice carDevice){
            FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
            Save s = await fs.LoadFile($"{carDevice.Id}.dat");
            string name = $"Unknown Car({carDevice.Id})";
            int speedBalance = 0;
            bool hadConfig = false;
            if(s != null) {
                hadConfig = true;
                name = s.GetString("name");
                speedBalance = (int)s.GetInt("speedBalance");
            }
            Program.Log($"[0] Connecting to car {name}");

            await carDevice.Gatt.ConnectAsync();
            if(carDevice.Gatt.IsConnected){
                Program.Log($"[0] Connected to car {name}");
                Car car = new Car(name, carDevice.Id, carDevice, speedBalance);
                if(!hadConfig){
                    s = new Save();
                    s.SetString("name", name);
                    s.SetInt("speedBalance", 0);
                    await fs.SaveFile($"{carDevice.Id}.dat", s);
                }
                GattService service = await car.device.Gatt.GetPrimaryServiceAsync(ServiceID);
                if(service == null){ return; }
                //subscribe to characteristic changed event on read characteristic
                var characteristic = await service.GetCharacteristicAsync(ReadID);
                if(characteristic == null){ return; }
                cars.Add(car);
                Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
                CheckCarConnection(car);
                characteristic.CharacteristicValueChanged += (sender, args) => { CarCharacteristicChanged(sender, args, car); };
                await characteristic.StartNotificationsAsync();
                await car.EnableSDKMode(true);
                await Task.Delay(500);
                Program.UtilLog($"-1:{car.id}:{car.name}");
            }
            else{
                Program.Log($"[0] Failed to connect to car {name}");
                Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
            }
        }
        async Task CheckCarConnection(Car car){ //check if car is still connected every 5 seconds
            while(car.device.Gatt.IsConnected){ await Task.Delay(5000);  }
            Program.Log($"[0] Car disconnected {car.name}");
            Program.UtilLog($"-2:{car.id}");
            cars.Remove(car);
        }
        
        static void CarCharacteristicChanged(object sender, GattCharacteristicValueChangedEventArgs args, Car car){
            Program.messageManager.ParseMessage(args.Value, car);
        }
        public string CarDataAsJson(){
            CarData[] carData = new CarData[cars.Count];
            for(int i = 0; i < cars.Count; i++){ carData[i] = cars[i].data; }
            return JsonConvert.SerializeObject(carData);
        }
        public void ClearCarData(){
            for(int i = 0; i < cars.Count; i++){ cars[i].data = new CarData(cars[i].name, cars[i].id); }
        }
        public int CarCount(){ return cars.Count; }
        public Car GetCar(int index){ return cars[index]; }
    }
    public class Car{
        public string name;
        public string id;
        public BluetoothDevice device;
        public int speedBalance = 0;
        float requestedOffset = 0;
        public CarData data;
        GattCharacteristic writeCharacteristic;
        bool hasWriteCharacteristic = false;
        public Car(string name, string id, BluetoothDevice device, int speedBalance = 0){
            this.name = name;
            this.id = id;
            this.device = device;
            this.speedBalance = speedBalance;
            this.data = new CarData(name, id);
            GetWriteCharacteristic();
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
            byte[] data = Program.DEV_V4 ? new byte[8] : new byte[7];
            data[0] = Program.DEV_V4 ? (byte)0x07 : (byte)0x06;
            data[1] = SEND_CAR_SPEED_UPDATE;
            //speed as int16
            data[2] = (byte)(speed & 0xFF);
            data[3] = (byte)((speed >> 8) & 0xFF);
            //accel as int16
            data[4] = (byte)(accel & 0xFF);
            data[5] = (byte)((accel >> 8) & 0xFF);
            //respect track piece speed limits
            data[6] = 0x01;
            if(Program.DEV_V4){ data[7] = 0x01; } //drive without scanned track? v4 only
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
        public void LaneCheck(){
            if((Math.Abs(requestedOffset - data.laneOffset) > 0.3) && data.speed > 0){
                SetCarLane(requestedOffset);
            }
        }
        public async Task RequestCarBattery(){
            byte[] data = new byte[]{0x01, SEND_BATTERY_REQUEST};
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarLightsPattern(float r, float g, float b){
            byte[] data = new byte[18];
            data[0] = 0x11; //size
            data[1] = SEND_LIGHTS_PATTERN_UPDATE; //id (set lights) 51
            data[2] = 0x03; //ENGINE maybe ??? Headlights, Brakelights, Frontlights, Enginelights ??
            data[3] = 0x00; //???
            data[4] = 0x00; //???
            int rByte =  r == 1 ? 0x01 : 0x00;
            int gByte =  g == 1 ? 0x01 : 0x00;
            int bByte =  b == 1 ? 0x01 : 0x00;
            data[5] = (byte)rByte; //red    maybe start 
            data[6] = (byte)rByte; //red2?? maybe end
            data[7] = 0x00; //??
            data[8] = 0x03; //Also maybe engine ??
            data[9] = 0x00; //??
            data[10] = (byte)gByte; //green    maybe start
            data[11] = (byte)gByte; //green2?? maybe end
            data[12] = 0x00; //??
            data[13] = 0x02; //Effect ??  Steady,Fade,Throb,Flash,Random,Count ??
            data[14] = 0x00; //??
            data[15] = (byte)bByte; //blue    maybe start
            data[16] = (byte)bByte; //blue2?? maybe end
            data[17] = 0x00; //?? 
            await WriteToCarAsync(data, true);
        }
        public async Task UTurn(){
            byte[] data = new byte[2];
            data[0] = 0x02; //size
            data[1] = SEND_CAR_UTURN; //id 50
            await WriteToCarAsync(data, true);
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