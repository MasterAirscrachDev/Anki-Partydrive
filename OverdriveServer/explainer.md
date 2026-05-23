cli build: 

Windows (with UWP Bluetooth - Recommended):
(Run once) dotnet workload restore
dotnet publish -c Release -f net8.0-windows10.0.22621.0 -r win-x64 --self-contained true
bin\Release\net8.0-windows10.0.22621.0\win-x64\publish

Linux: (need to fix)
(Run once) dotnet workload restore
dotnet publish -c Release -f net8.0 -r linux-x64 --self-contained true
bin\Release\net8.0\linux-x64\publish

Mac:
(Run once) dotnet workload restore

M Series Macs: dotnet publish -c Release -f net10.0-macos -r osx-arm64 --self-contained true -p:EnableWindowsTargeting=true
bin\Release\net10.0-macos\osx-arm64\OverdriveServer.app

Intel Series Macs: dotnet publish -c Release -f net10.0-macos -r osx-x64 --self-contained true -p:EnableWindowsTargeting=true
bin\Release\net10.0-macos\osx-x64\OverdriveServer.app

game server folder: D:\UNITY\Anki Partydrive\BUILDS\Server