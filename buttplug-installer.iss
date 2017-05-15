[Setup]
AppName=Buttplug
AppVersion=0.0.1
DefaultDirName={pf}\Buttplug
DefaultGroupName=Buttplug
UninstallDisplayIcon={app}\ButtplugGUI.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=buttplug-installer
OutputDir=.\installer

#define Configuration GetEnv('CONFIGURATION')
#if Configuration == ""
#define Configuration = "Release"
#endif

[Files]
Source: "ButtplugGUI\bin\{#Configuration}\ButtplugGUI.exe"; DestDir: "{app}"
Source: "ButtplugGUI\bin\{#Configuration}\*.dll"; DestDir: "{app}"
Source: "Readme.md"; DestDir: "{app}"; DestName: "Readme.txt"; Flags: isreadme

[Icons]
Name: "{group}\Buttplug"; Filename: "{app}\ButtplugGUI.exe"

[Registry]
; Windows 10 15063 Patch BLE security sadness
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\{{415579bd-5399-48ef-8521-775ebcd647af}}"; ValueType: binary; ValueName: "ApplicationPermission"; ValueData: 01 00 04 80 9C 00 00 00 AC 00 00 00 00 00 00 00 14 00 00 00 02 00 88 00 06 00 00 00 00 00 14 00 07 00 00 00 01 01 00 00 00 00 00 05 0A 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 12 00 00 00 00 00 18 00 07 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 00 00 18 00 03 00 00 00 01 02 00 00 00 00 00 0F 02 00 00 00 01 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 13 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 14 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\Buttplug.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ButtplugCLI.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ButtplugGUI.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}}"; Flags: uninsdeletekey
