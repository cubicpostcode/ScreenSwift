; Build ScreenSwift first, then compile this script with Inno Setup 6.
; It creates a normal installed Windows application with Start-menu entry
; and uninstaller, rather than asking the user to run a loose executable.

#define MyAppName "ScreenSwift"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Cubic Postcode"
#define MyAppExeName "ScreenSwift.exe"

[Setup]
AppId={{A8ED4FAA-4426-462C-859A-FF42D5C05545}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
SetupIconFile=..\ScreenSwift\Assets\ScreenSwift.ico
DefaultDirName={autopf}\ScreenSwift
DefaultGroupName=ScreenSwift
UninstallDisplayName=ScreenSwift
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Output
OutputBaseFilename=ScreenSwift-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Files]
Source: "..\ScreenSwift\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\ScreenSwift"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\ScreenSwift"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startup"; Description: "Start ScreenSwift automatically when I sign in"; GroupDescription: "Startup:"; Flags: checkedonce

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ScreenSwift"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch ScreenSwift"; Flags: nowait postinstall skipifsilent
