using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Bluetooth;
using Newtonsoft.Json;
using static OverdriveServer.Definitions;

namespace OverdriveServer
{
    class CarSystem
    {
        public static readonly BluetoothUuid ServiceID = BluetoothUuid.FromGuid(new Guid("BE15BEEF-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid ReadID = BluetoothUuid.FromGuid(new Guid("BE15BEE0-6186-407E-8381-0BD89C4D8DF4"));
        public static readonly BluetoothUuid WriteID = BluetoothUuid.FromGuid(new Guid("BE15BEE1-6186-407E-8381-0BD89C4D8DF4"));
        List<Car> cars = new List<Car>();
        public bool CarExists(string id){
            return cars.Exists(car => car.id == id);
        }
        public Car GetCar(string id){
            return cars.Find(car => car.id == id);
        }
        public async Task ConnectToCarAsync(BluetoothDevice carDevice){
            FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
            Save s = await fs.LoadFile($"{carDevice.Id}.dat");
            string name = "Unknown Car";
            int speedBalance = 0;
            bool hadConfig = false; 
            if(s != null){
                hadConfig = true;
                name = s.GetString("name");
                speedBalance = (int)s.GetInt("speedBalance");
            }
            Program.Log($"Connecting to car {name}");

            await carDevice.Gatt.ConnectAsync();
            if(carDevice.Gatt.IsConnected){
                Program.Log($"Connected to car {name}");
                Car car = new Car(name, carDevice.Id, carDevice, speedBalance);
                if(!hadConfig){
                    s = new Save();
                    s.SetString("name", carDevice.Name);
                    s.SetInt("speedBalance", 0);
                    await fs.SaveFile($"{carDevice.Id}.dat", s);
                }
                //Expected
                //Service              "BE15BEEF-6186-407E-8381-0BD89C4D8DF4"
                //Characteristic Read  "BE15BEE0-6186-407E-8381-0BD89C4D8DF4"
                //Characteristic Write "BE15BEE1-6186-407E-8381-0BD89C4D8DF4"
                var service = await car.device.Gatt.GetPrimaryServiceAsync(ServiceID);
                if(service == null){ return; }
                //subscribe to characteristic changed event on read characteristic
                var characteristic = await service.GetCharacteristicAsync(ReadID);
                if(characteristic == null){ return; }
                cars.Add(car);
                Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
                CheckCarConnection(car);
                characteristic.CharacteristicValueChanged += (sender, args) => {
                    CarCharacteristicChanged(sender, args, car);
                };
                await characteristic.StartNotificationsAsync();
                await car.EnableSDKMode(true);
                await Task.Delay(500);
                Program.UtilLog($"-1:{car.id}:{car.name}");
            }
            else{
                Program.Log($"Failed to connect to car {name}");
                Program.bluetoothInterface.RemoveCarCheck(carDevice.Id);
            }
        }
        async Task CheckCarConnection(Car car){
            while(car.device.Gatt.IsConnected){
                await Task.Delay(5000);
            }
            Program.Log($"Car disconnected {car.name}");
            Program.UtilLog($"-2:{car.id}");
            cars.Remove(car);
        }
        
        static void CarCharacteristicChanged(object sender, GattCharacteristicValueChangedEventArgs args, Car car){
            Program.messageManager.ParseMessage(args.Value, car);
        }
        public string CarDataAsJson(){
            CarData[] carData = new CarData[cars.Count];
            for(int i = 0; i < cars.Count; i++){
                carData[i] = cars[i].data;
            }
            return JsonConvert.SerializeObject(carData);
        }
        public void ClearCarData(){
            for(int i = 0; i < cars.Count; i++){
                cars[i].data = new CarData(cars[i].name, cars[i].id);
            }
        }
        public int CarCount(){
            return cars.Count;
        }
        public Car GetCar(int index){
            return cars[index];
        }
    }
    public class Car{
        public string name;
        public string id;
        //bluetooth connection
        public BluetoothDevice device;
        public int speedBalance = 0;
        public CarData data;
        public Car(string name, string id, BluetoothDevice device, int speedBalance = 0){
            this.name = name;
            this.id = id;
            this.device = device;
            this.speedBalance = speedBalance;
            this.data = new CarData(name, id);
        }
        public async Task EnableSDKMode(bool enable = true){
            //4 bytes 0x03 0x90 0x01 0x01
            byte enabled = enable ? (byte)0x01 : (byte)0x00;
            byte[] data = new byte[]{0x03, SEND_SDK_MODE, enabled, 0x01};
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarSpeed(int speed, int accel = 1000){
            if(speedBalance != 0){
                speed = Math.Clamp(speed + speedBalance, 0, 1200);
            }
            byte[] data = new byte[7];
            data[0] = 0x06;
            data[1] = SEND_CAR_SPEED_UPDATE;
            //speed as int16
            data[2] = (byte)(speed & 0xFF);
            data[3] = (byte)((speed >> 8) & 0xFF);
            //accel as int16
            data[4] = (byte)(accel & 0xFF);
            data[5] = (byte)((accel >> 8) & 0xFF);
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarTrackCenter(float offset){
            byte[] data = new byte[6];
            data[0] = 0x05;
            data[1] = SEND_TRACK_CENTER_UPDATE;
            BitConverter.GetBytes((float)offset).CopyTo(data, 2); 
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarLane(float lane){ // Offset value (?? 68,23,-23,68 seem to be lane values 1-4)
            byte[] data = new byte[12];
            data[0] = 11;
            data[1] = 0x25;
            int horizontalSpeedmm = 100, horizontalAccelmm = 1000;
            //horizontal speed as int16
            data[2] = (byte)(horizontalSpeedmm & 0xFF);
            data[3] = (byte)((horizontalSpeedmm >> 8) & 0xFF);
            //horizontal accel as int16
            data[4] = (byte)(horizontalAccelmm & 0xFF);
            data[5] = (byte)((horizontalAccelmm >> 8) & 0xFF);
            //lane as float
            BitConverter.GetBytes((float)lane).CopyTo(data, 6);
            await WriteToCarAsync(data, true);
        }
        public async Task RequestCarBattery(){
            byte[] data = new byte[]{0x01, 0x1a};
            await WriteToCarAsync(data, true);
        }
        public async Task SetCarLights(float r, float g, float b){
            byte[] data = new byte[18];
            data[0] = 0x11; //size
            data[1] = 0x33; //id (set lights) 51
            data[2] = 0x03; //???
            data[3] = 0x00; //???
            data[4] = 0x00; //???
            int rByte =  r == 1 ? 0x01 : 0x00;
            int gByte =  g == 1 ? 0x01 : 0x00;
            int bByte =  b == 1 ? 0x01 : 0x00;
            data[5] = (byte)rByte; //red
            data[6] = (byte)rByte; //red2??
            data[7] = 0x00; //??
            data[8] = 0x03; //??
            data[9] = 0x00; //??
            data[10] = (byte)gByte; //green
            data[11] = (byte)gByte; //green2??
            data[12] = 0x00; //??
            data[13] = 0x02; // Solid ??
            data[14] = 0x00; //??
            data[15] = (byte)bByte; //blue
            data[16] = (byte)bByte; //blue2??
            data[17] = 0x00; //??
            await WriteToCarAsync(data, true);
        }

        async Task WriteToCarAsync(byte[] data, bool response = false){
            var service = await device.Gatt.GetPrimaryServiceAsync(CarSystem.ServiceID);
            var characteristic = await service.GetCharacteristicAsync(CarSystem.WriteID);
            //send someth
            if(response){ await characteristic.WriteValueWithResponseAsync(data); }
            else{ await characteristic.WriteValueWithoutResponseAsync(data); }
        }
    }
    [System.Serializable]
    public class CarData{
        public string name;
        public string id;
        public int trackPosition;
        public int trackID;
        public float laneOffset;
        public int speed;
        public int battery;
        public bool charging;
        public CarData(string name, string id){
            this.name = name;
            this.id = id;
        }
    }
}
