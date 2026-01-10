using System;
using System.Runtime.InteropServices;

#if NET8_0_OR_GREATER && WINDOWS10_0_22621_0_OR_GREATER
using OverdriveServer.Bluetooth.UWP;
#endif

namespace OverdriveServer.Bluetooth
{
    /// <summary>
    /// Factory for creating the appropriate Bluetooth provider based on the target platform
    /// </summary>
    public static class BluetoothProviderFactory
    {
        /// <summary>
        /// Creates the appropriate Bluetooth provider for the current platform
        /// </summary>
        /// <returns>Platform-specific Bluetooth provider</returns>
        public static IBluetoothProvider CreateProvider()
        {
#if NET8_0_OR_GREATER && WINDOWS10_0_22621_0_OR_GREATER
            // Use UWP provider on Windows when targeting Windows-specific framework
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Console.WriteLine("BLE: INIT_UWP_WIN");
                    return new UWPBluetoothProvider();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize UWP Bluetooth provider: {ex.Message}");
                    Console.WriteLine("Falling back to SimpleBLE provider");
                    return new SimpleBLEProvider();
                }
            }
#endif
            
            // Use SimpleBLE provider as default for all platforms (Linux, macOS, non-UWP Windows)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("BLE: INIT_SIMPLEBLE_LINUX");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("BLE: INIT_SIMPLEBLE_WIN");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("BLE: INIT_SIMPLEBLE_MAC");
            }
            else
            {
                Console.WriteLine("BLE: INIT_SIMPLEBLE_UNKNOWN_OS");
            }
            
            return new SimpleBLEProvider();
        }
        
        /// <summary>
        /// Gets information about the current Bluetooth provider capabilities
        /// </summary>
        /// <returns>String describing the provider and platform</returns>
        public static string GetProviderInfo()
        {
            var os = "Unknown";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                os = "Windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                os = "macOS";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                os = "Linux";

#if NET8_0_OR_GREATER && WINDOWS10_0_22621_0_OR_GREATER
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"Platform: {os}, Provider: UWP (Native Windows), Framework: {RuntimeInformation.FrameworkDescription}";
            }
#endif
            
            return $"Platform: {os}, Provider: SimpleBLE (Native via P/Invoke), Framework: {RuntimeInformation.FrameworkDescription}";
        }
    }
}