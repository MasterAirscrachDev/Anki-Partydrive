using System.Speech.Synthesis;
using OverdriveServer.Tracking;
using static OverdriveServer.NetStructures;
namespace OverdriveServer
{
    class Program
    {
        public static bool requireClient = false, clientClosedGracefully = false;
        public static WebInterface webInterface = new WebInterface();
        public static WebsocketManager socketMan = new WebsocketManager();
        public static BluetoothInterface bluetoothInterface = new BluetoothInterface();
        public static CarSystem carSystem = new CarSystem();
        public static MessageManager messageManager = new MessageManager();
        public static TrackManager trackManager = new TrackManager();
        public static Location location = new Location();
        public static TrackScanner trackScanner = new TrackScanner();
        static readonly SpeechSynthesizer synth = new SpeechSynthesizer();
        static async Task Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "-client") { requireClient = true; }
            Console.WriteLine("Overdrive Server By MasterAirscrach \nStarting server...");
            webInterface.Start(); //Start the web interface
            await bluetoothInterface.InitaliseBletooth(); //Start the bluetooth subsystem
            bluetoothInterface.ScanForCars(); //Start scanning for cars
            Console.WriteLine("Bluetooth scanning started");

            Console.CancelKeyPress += async (s, e) => {
                e.Cancel = true;
                await CleanupAndQuit();
            };

            await new TaskCompletionSource<bool>().Task; //Wait indefinitely
        }
        public static void Log(string message, bool consoleAnyway = false)
        {
            socketMan.Notify(EVENT_SYSTEM_LOG, message);
            if (!socketMan.HasClients() || consoleAnyway) { Console.WriteLine(message); }
        }
        public static void UtilLog(string message) { socketMan.Notify(EVENT_UTILITY_LOG, message); }
        public static void CheckCurrentTrack() { trackManager.AlertIfTrackIsValid(); }
        public static void TTS(string message)
        {
            synth.SetOutputToDefaultAudioDevice();
            synth.Speak(message);
        }
        public static string IntToByteString(int number) => $"0x{number:X2}"; // More concise using expression body

        public static string BytesToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) { return string.Empty; }
            var sb = new System.Text.StringBuilder(bytes.Length * 5);
            foreach (var b in bytes) { sb.Append(IntToByteString(b)).Append(" "); }
            if (sb.Length > 0) { sb.Length -= 1; } // Remove the last space
            return sb.ToString();
        }
        static async Task CleanupAndQuit()
        {
            try
            {
                await carSystem.RequestAllDisconnect(); //request all cars to disconnect

                Console.WriteLine("Cleaning up..."); //cleanup
                socketMan.Cleanup();
                Console.WriteLine("Cleanup complete, quitting..."); //cleanup complete
                await Task.Delay(1000); //wait for 1 second
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during cleanup: {e.Message}");
            }
            finally
            {
                Environment.Exit(0); //exit the program
            }
        }
    }
}