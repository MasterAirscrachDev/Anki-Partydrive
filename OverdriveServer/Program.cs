using System.Speech.Synthesis;
using static OverdriveServer.NetStructures;
using OverdriveServer.Tracking;
namespace OverdriveServer {
    class Program {
        public static WebInterface webInterface = new WebInterface();
        public static WebsocketManager socketMan = new WebsocketManager();
        public static BluetoothInterface bluetoothInterface = new BluetoothInterface();
        public static CarSystem carSystem = new CarSystem();
        public static MessageManager messageManager = new MessageManager();
        public static TrackManager trackManager = new TrackManager();
        public static List<TrackScanner> scansInProgress = new List<TrackScanner>();
        public static Location location = new Location();
        static async Task Main(string[] args) {
            if (args.Length == 1 && args[0] == "-snoop") { Snooper snoop = new Snooper(); await snoop.Start(); }
            else{
                Console.WriteLine("Overdrive Server By MasterAirscrach");
                Console.WriteLine("Starting server...");
                await bluetoothInterface.InitaliseBletooth(); //Start the bluetooth subsystem
                bluetoothInterface.ScanForCars(); //Start scanning for cars
                webInterface.Start(); //Start the web interface
                await Task.Delay(-1); //dont close the program
            }
        }
        public static async Task Log(string message){ 
            socketMan.Notify(EVENT_SYSTEM_LOG, message);
            if(!socketMan.HasClients()){ Console.WriteLine(message); } 
        }
        public static async Task UtilLog(string message){ socketMan.Notify(EVENT_UTILITY_LOG, message); }
        public static void CheckCurrentTrack(){ trackManager.AlertIfTrackIsValid(); }
        public static void TTS(string message){
            SpeechSynthesizer synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();
            synth.Speak(message);
        }
        public static async Task ScanTrack(Car car){
            TrackScanner scanner = new TrackScanner();
            scansInProgress.Add(scanner);
            await scanner.ScanTrack(car);
        }
        public static async Task CancelScan(Car car) {
            foreach(TrackScanner scanner in scansInProgress) {
                if(await scanner.CancelScan(car)){ scansInProgress.Remove(scanner); return; }
            }
        }
        public static string IntToByteString(int number) { return "0x" + number.ToString("X2"); } //as 0x00
        public static string BytesToString(byte[] bytes){ 
            string content = "";
            for(int i = 0; i < bytes.Length; i++){content += IntToByteString(bytes[i]) + " ";}
            return content;
        }
    }
}