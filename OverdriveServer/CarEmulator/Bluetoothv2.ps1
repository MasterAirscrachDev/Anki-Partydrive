# Create a timestamp for the log file name
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "bluetooth_log_$timestamp.txt"

Write-Host "Logging Bluetooth data to $logFile"
Write-Host "Press Ctrl+C to stop logging"

try {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "adb"
    $psi.Arguments = "logcat"
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    $process.Start() | Out-Null

    while (-not $process.HasExited) {
        $line = $process.StandardOutput.ReadLine()
        if ($line -match "bluetooth") {
            Write-Host $line
            Add-Content -Path $logFile -Value $line
        }
    }
}
catch {
    Write-Host "An error occurred: $_"
}
finally {
    # Clean up
    if ($null -ne $process -and !$process.HasExited) {
        $process.Kill()
    }
}

Write-Host "Logging complete. Output saved to $logFile"
Read-Host "Press Enter to exit"