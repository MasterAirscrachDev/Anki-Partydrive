using OverdriveServer.Tracking;
using OverdriveServer.Bluetooth;
using static OverdriveServer.NetStructures;

namespace OverdriveServer {
    class Program {
        public static bool requireClient = false, clientClosedGracefully = false, 
        // configurables
        autoConnect = false, sendExtraTracking = false;
        public static WebInterface webInterface = new WebInterface();
        public static WebsocketManager socketMan = new WebsocketManager();
        
        // Initialize Bluetooth provider and dependent services
        public static IBluetoothProvider bluetoothProvider = BluetoothProviderFactory.CreateProvider();
        public static BluetoothInterface bluetoothInterface = new BluetoothInterface(bluetoothProvider);
        public static CarSystem carSystem = new CarSystem(bluetoothProvider);
        public static MessageManager messageManager = new MessageManager();
        public static TrackManager trackManager = new TrackManager();
        public static Location location = new Location();
        public static TrackScanner trackScanner = new TrackScanner();
        static async Task Main(string[] args) {
            // Parse command line arguments
            bool helpRequested = false;
            foreach(string arg in args) {
                if(arg == "-client") { requireClient = true; }
                else if(arg == "-autoconnect") { autoConnect = true; }
                else if(arg == "-tracking") { sendExtraTracking = true; }
                else if(arg == "-help" || arg == "-h") {
                    helpRequested = true;
                }
            }
            Console.WriteLine("[Public] Overdrive Server By MasterAirscrach (Derived from: 3.2.8)");
            if(helpRequested) { DoHelp(); return; }
            if(requireClient) { Console.WriteLine("\"-client\" Server will auto-terminate on client disconnect"); }
            if(sendExtraTracking) {
                Console.WriteLine("\"--tracking\"Extra tracking data and debugging enabled");
                Console.WriteLine(BluetoothProviderFactory.GetProviderInfo());
            }
            if(autoConnect) { Console.WriteLine("\"--autoconnect\" Auto-connecting to cars enabled"); }
            webInterface.Start(); //Start the web interface
            await bluetoothInterface.InitaliseBletooth(); //Start the bluetooth subsystem

            Console.CancelKeyPress += async (s, e) => {
                e.Cancel = true;
                await CleanupAndQuit();
            };

            await new TaskCompletionSource<bool>().Task; //Wait indefinitely
        }
        static void DoHelp() {
            Console.WriteLine("Usage: OverdriveServer.exe [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -client         Server will auto-terminate on all clients disconnected");
            Console.WriteLine("  -autoconnect    Auto-connecting to cars enabled");
            Console.WriteLine("  -tracking       Extra tracking data and debugging enabled");
            Console.WriteLine("  -help, -h       Display this help message");
            Environment.Exit(0);
        }
        public static void Log(string message, bool consoleAnyway = false){ 
            socketMan.Notify(EVENT_SYSTEM_LOG, message);
            if(!socketMan.HasClients() || consoleAnyway){ Console.WriteLine(message); } 
        }
        public static void UtilLog(string message){ socketMan.Notify(EVENT_UTILITY_LOG, message); }
        public static void CheckCurrentTrack(){ trackManager.AlertIfTrackIsValid(); }
        public static string IntToByteString(int number) => $"0x{number:X2}"; // More concise using expression body

        public static string BytesToString(byte[] bytes) { 
            if (bytes == null || bytes.Length == 0) {return string.Empty;  }
            var sb = new System.Text.StringBuilder(bytes.Length * 5);
            foreach (var b in bytes) { sb.Append(IntToByteString(b)).Append(" "); }
            if (sb.Length > 0){ sb.Length -= 1; } // Remove the last space
            return sb.ToString();
        }
        static async Task CleanupAndQuit(){
            try{
                await carSystem.RequestAllDisconnect(); //request all cars to disconnect

                Console.WriteLine("Cleaning up..."); //cleanup
                await messageManager.DisposeAsync(); //dispose of the message manager
                await trackManager.DisposeAsync(); //dispose of the track manager
                socketMan.Cleanup();
                bluetoothProvider.Dispose(); //dispose of the bluetooth provider
                Console.WriteLine("Cleanup complete, quitting..."); //cleanup complete
                await Task.Delay(1000); //wait for 1 second
            }catch(Exception e){
                Console.WriteLine($"Error during cleanup: {e.Message}");
            }
            finally{
                Environment.Exit(0); //exit the program
            }
        }
    }
}