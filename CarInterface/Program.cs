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
        static async Task Main(string[] args)
        {
            Console.WriteLine("Overdrive Server By MasterAirscrach");
            Console.WriteLine("Starting server...");
            await bluetoothInterface.InitaliseBletooth();
            bluetoothInterface.ScanForCars();
            webInterface.Start();
            await Task.Delay(-1);
        }
        public static void Log(string message){
            SysLog += message + "\n";
            if(printLog){ Console.WriteLine(message); }
        }
        public static string UtilLog(string message){
            UtilityLog += message + "\n";
            return message;
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
        public static string IntToByteString(int number)
        { return "0x" + number.ToString("X2"); } //as 0x00
        public static string BytesToString(byte[] bytes)
        { return BitConverter.ToString(bytes).Replace("-", ""); }
    }
}