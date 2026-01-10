cli build: 

Windows (with UWP Bluetooth - Recommended):
dotnet publish -c Release -f net8.0-windows10.0.22621.0 -r win-x64 --self-contained true
bin\Release\net8.0-windows10.0.22621.0\win-x64\publish

Linux: (need to fix)
dotnet publish -c Release -f net8.0 -r linux-x64 --self-contained true
bin\Release\net8.0\linux-x64\publish

Mac: (maybe oneday)
dotnet publish -c Release -f net8.0 -r osx-x64 --self-contained true
bin\Release\net8.0\osx-x64\publish


game server folder: D:\UNITY\Anki Partydrive\BUILDS\Server