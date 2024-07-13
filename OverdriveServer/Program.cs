using InTheHand.Bluetooth;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

using System.Speech.Synthesis;

namespace OverdriveServer
{
    class Program
    {
        static string SysLog = "", UtilityLog = "";
        static bool printLog = true;
        public static WebInterface webInterface = new WebInterface();
        public static BluetoothInterface bluetoothInterface = new BluetoothInterface();
        public static CarSystem carSystem = new CarSystem();
        public static MessageManager messageManager = new MessageManager();
        public static TrackManager trackManager = new TrackManager();
        public static List<TrackScanner> scansInProgress = new List<TrackScanner>();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Overdrive Server By MasterAirscrach");
            Console.WriteLine("Starting server...");
            await bluetoothInterface.InitaliseBletooth(); //Start the bluetooth subsystem
            bluetoothInterface.ScanForCars(); //Start scanning for cars
            webInterface.Start(); //Start the web interface
            await Task.Delay(-1); //dont close the program
        }
        public static void Log(string message){
            SysLog += message + "\n";
            if(printLog){ Console.WriteLine(message); }
        }
        public static void UtilLog(string message){
            UtilityLog += message + "\n";
        }
        public static string GetLog(){
            string content = SysLog;
            SysLog = "";
            return content;
        }
        public static string GetUtilLog(){
            string content = UtilityLog;
            UtilityLog = "";
            return content;
        }
        public static void SetLogging(bool state){
            printLog = state;
        }
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
        public static async Task CancelScan(Car car){
            foreach(TrackScanner scanner in scansInProgress){
                if(await scanner.CancelScan(car)){
                    scansInProgress.Remove(scanner); return;
                }
            }
        }
        public static void RemoveScan(TrackScanner scanner){
            scansInProgress.Remove(scanner);
        }
        public static string IntToByteString(int number)
        { return "0x" + number.ToString("X2"); } //as 0x00
        public static string BytesToString(byte[] bytes){ 
            string content = "";
            for(int i = 0; i < bytes.Length; i++){content += IntToByteString(bytes[i]) + " ";}
            return content;
        }
    }
}