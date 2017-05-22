#define Configuration GetEnv('CONFIGURATION')
#if Configuration == ""
#define Configuration "Release"
#endif

#define Version GetEnv('appveyor_build_version')
#if Version == ""
#define Version "x.x.x.x"
#endif

[Setup]
AppName=Buttplug
AppVersion={#Version}
AppPublisher=Metafetish
AppPublisherURL=www.buttplug.io
AppId={{415579bd-5399-48ef-8521-775ebcd647af}
SetupIconFile=ButtplugGUI\Resources\buttplug-icon-1.ico
WizardImageFile=ButtplugGUI\Resources\buttplug-logo-1.bmp
WizardSmallImageFile=ButtplugGUI\Resources\buttplug-logo-1.bmp
DefaultDirName={pf}\Buttplug
DefaultGroupName=Buttplug
UninstallDisplayIcon={app}\ButtplugGUI.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=buttplug-installer
OutputDir=.\installer
LicenseFile=LICENSE

[Files]
Source: "ButtplugCLI\bin\{#Configuration}\ButtplugCLI.exe"; DestDir: "{app}"
Source: "ButtplugCLI\bin\{#Configuration}\*.dll"; DestDir: "{app}"
Source: "ButtplugGUI\bin\{#Configuration}\ButtplugGUI.exe"; DestDir: "{app}"
Source: "ButtplugGUI\bin\{#Configuration}\*.dll"; DestDir: "{app}"
Source: "Readme.md"; DestDir: "{app}"; DestName: "Readme.txt"; Flags: isreadme
Source: "LICENSE"; DestDir: "{app}"; DestName: "License.txt"

[Icons]
Name: "{group}\ButtplugGUI"; Filename: "{app}\ButtplugGUI.exe"
Name: "{group}\ButtplugCLI"; Filename: "{app}\ButtplugCLI.exe"

; Windows 10 15063 Patch BLE security sadness
[Registry]
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\{{415579bd-5399-48ef-8521-775ebcd647af}"; ValueType: binary; ValueName: "AccessPermission"; ValueData: 01 00 04 80 9C 00 00 00 AC 00 00 00 00 00 00 00 14 00 00 00 02 00 88 00 06 00 00 00 00 00 14 00 07 00 00 00 01 01 00 00 00 00 00 05 0A 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 12 00 00 00 00 00 18 00 07 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 00 00 18 00 03 00 00 00 01 02 00 00 00 00 00 0F 02 00 00 00 01 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 13 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 14 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\Buttplug.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ButtplugCLI.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ButtplugGUI.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}"; Flags: uninsdeletekey
