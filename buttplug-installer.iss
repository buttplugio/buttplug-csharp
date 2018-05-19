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
SetupIconFile=Buttplug.Components.Controls\Resources\buttplug-icon-1.ico
WizardImageFile=Buttplug.Components.Controls\Resources\buttplug-logo-1.bmp
WizardSmallImageFile=Buttplug.Components.Controls\Resources\buttplug-logo-1.bmp
DefaultDirName={pf}\Buttplug
DefaultGroupName=Buttplug
UninstallDisplayIcon={app}\ButtplugServerGUI.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=buttplug-installer
OutputDir=.\installer
LicenseFile=LICENSE

[Files]
Source: "Buttplug.Apps.KiirooEmulatorGUI\bin\{#Configuration}\*.exe"; DestDir: "{app}"
Source: "Buttplug.Apps.KiirooEmulatorGUI\bin\{#Configuration}\*.dll"; DestDir: "{app}"
Source: "Buttplug.Apps.KiirooEmulatorGUI\bin\{#Configuration}\*.config"; DestDir: "{app}"
Source: "Buttplug.Apps.ServerGUI\bin\{#Configuration}\*.exe"; DestDir: "{app}"
Source: "Buttplug.Apps.ServerGUI\bin\{#Configuration}\*.dll"; DestDir: "{app}"
Source: "Buttplug.Apps.ServerGUI\bin\{#Configuration}\*.config"; DestDir: "{app}"
Source: "Buttplug.Apps.GameVibrationRouter.GUI\bin\{#Configuration}\*.exe"; DestDir: "{app}"
Source: "Buttplug.Apps.GameVibrationRouter.GUI\bin\{#Configuration}\*.dll"; DestDir: "{app}"
Source: "Buttplug.Apps.GameVibrationRouter.GUI\bin\{#Configuration}\*.config"; DestDir: "{app}"
Source: "Readme.md"; DestDir: "{app}"; DestName: "Readme.txt"
Source: "LICENSE"; DestDir: "{app}"; DestName: "License.txt"

[Run]
Filename: "{app}\Readme.txt"; Description: "View the README file"; Flags: postinstall shellexec unchecked

[Icons]
Name: "{commonprograms}\Buttplug Server"; Filename: "{app}\ButtplugServerGUI.exe"
Name: "{commonprograms}\Buttplug Kiiroo Emulator"; Filename: "{app}\ButtplugKiirooEmulatorGUI.exe"
Name: "{commonprograms}\Buttplug Game Vibration Router"; Filename: "{app}\ButtplugGameVibrationRouterGUI.exe"

; Windows 10 15063 Patch BLE security sadness
[Registry]
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\{{415579bd-5399-48ef-8521-775ebcd647af}"; ValueType: binary; ValueName: "AccessPermission"; ValueData: 01 00 04 80 9C 00 00 00 AC 00 00 00 00 00 00 00 14 00 00 00 02 00 88 00 06 00 00 00 00 00 14 00 07 00 00 00 01 01 00 00 00 00 00 05 0A 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 12 00 00 00 00 00 18 00 07 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 00 00 18 00 03 00 00 00 01 02 00 00 00 00 00 0F 02 00 00 00 01 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 13 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 14 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ButtplugKiirooEmulatorGUI.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ButtplugServerGUI.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ButtplugGameVibrationRouterGUI.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{415579bd-5399-48ef-8521-775ebcd647af}"; Flags: uninsdeletekey

[Code]

// Uninstall on install code taken from https://stackoverflow.com/a/2099805/4040754
////////////////////////////////////////////////////////////////////
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;


/////////////////////////////////////////////////////////////////////
function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


/////////////////////////////////////////////////////////////////////
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
// Return Values:
// 1 - uninstall string is empty
// 2 - error executing the UnInstallString
// 3 - successfully executed the UnInstallString

  // default return value
  Result := 0;

  // get the uninstall string of the old app
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;
