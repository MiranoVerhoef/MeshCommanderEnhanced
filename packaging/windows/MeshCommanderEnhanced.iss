#ifndef MyAppVersion
  #define MyAppVersion "0.2.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\artifacts\app"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts"
#endif

#define MyAppName "MeshCommander Enhanced"
#define MyAppExeName "MeshCommander.Enhanced.Windows.exe"

[Setup]
AppId={{6EF3ED66-C60B-49A5-95F0-490A9E995C11}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=MiranoVerhoef
AppPublisherURL=https://github.com/MiranoVerhoef/MeshCommanderEnhanced
AppSupportURL=https://github.com/MiranoVerhoef/MeshCommanderEnhanced/issues
DefaultDirName={localappdata}\Programs\MeshCommander Enhanced
DefaultGroupName=MeshCommander Enhanced
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=meshcommander-enhanced-windows-x64-setup
SetupIconFile=..\..\favicon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\MeshCommander Enhanced"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\MeshCommander Enhanced"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch MeshCommander Enhanced"; Flags: nowait postinstall skipifsilent
