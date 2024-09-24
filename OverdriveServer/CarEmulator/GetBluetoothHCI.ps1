# Function to run ADB commands
function Run-AdbCommand {
    param([string]$command)
    $output = & adb $command.Split(" ")
    return $output
}

# Function to find HCI log file
function Find-HciLogFile {
    $possibleLocations = @(
        "/sdcard/btsnoop_hci.log",
        "/data/misc/bluetooth/logs/btsnoop_hci.log",
        "/data/log/bt/btsnoop_hci.log"
    )
    
    foreach ($location in $possibleLocations) {
        $result = Run-AdbCommand "shell ls $location"
        if ($result -notmatch "No such file") {
            return $location
        }
    }
    return $null
}

# Enable HCI snoop log
Write-Host "Enabling Bluetooth HCI snoop log..."
Run-AdbCommand "shell settings put secure bluetooth_hci_log 1"

# Wait for user to perform Bluetooth actions
Write-Host "Perform the Bluetooth actions you want to capture."
Write-Host "Press Enter when you're done to retrieve the log file."
Read-Host

# Disable HCI snoop log
Write-Host "Disabling Bluetooth HCI snoop log..."
Run-AdbCommand "shell settings put secure bluetooth_hci_log 0"

# Find and retrieve the log file
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "bluetooth_hci_log_$timestamp.log"
Write-Host "Searching for HCI snoop log..."
$remoteLogFile = Find-HciLogFile
if ($remoteLogFile) {
    Write-Host "Found log file at: $remoteLogFile"
    Write-Host "Retrieving HCI snoop log..."
    Run-AdbCommand "pull $remoteLogFile $logFile"
} else {
    Write-Host "Could not find HCI snoop log file. Make sure Bluetooth HCI logging is supported on your device."
    exit
}

# Convert binary log to readable format
Write-Host "Converting log to readable format..."
$readableLogFile = "readable_$logFile"
# Note: You'll need to install and configure Wireshark for this step
$tsharkPath = "C:\Program Files\Wireshark\tshark.exe"
if (Test-Path $tsharkPath) {
    & $tsharkPath -r $logFile -T fields -e frame.number -e frame.time -e btatt.opcode -e btatt.handle -e btatt.value -E header=y -E separator=, -E quote=d -E occurrence=f > $readableLogFile
    Write-Host "Readable log saved as: $readableLogFile"
} else {
    Write-Host "Wireshark not found. Please install Wireshark or specify the correct path to tshark.exe"
    Write-Host "Binary log saved as: $logFile"
}

Write-Host "Log retrieval complete."
Read-Host "Press Enter to exit"