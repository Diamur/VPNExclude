; VPNExclude installer bootstrap (Inno Setup)
;
; Build flavor options:
;   - Framework-dependent (default): requires .NET Desktop Runtime 8 x64 prerequisite.
;   - Self-contained: includes runtime in app publish output; .NET prerequisite step is skipped.
;
; Usage (examples):
;   iscc /DBuildFlavor=framework Install\VPNExclude.Setup.iss
;   iscc /DBuildFlavor=selfcontained Install\VPNExclude.Setup.iss

#ifndef BuildFlavor
  #define BuildFlavor "framework"
#endif

#if BuildFlavor != "framework" && BuildFlavor != "selfcontained"
  #error Invalid BuildFlavor. Use "framework" or "selfcontained".
#endif

#define AppName "VPNExclude"
#define AppVersion "1.0.0"
#define AppPublisher "VPNExclude Project"
#define AppExeName "VPNExclude.exe"

#define PrereqDir "Prerequisites"
#define DotnetDesktopInstaller "windowsdesktop-runtime-8.0.x-win-x64.exe"
#define WireGuardInstaller "wireguard-installer-x64.exe"

#if BuildFlavor == "framework"
  #define PublishDir "Output\\App\\framework-dependent"
#else
  #define PublishDir "Output\\App\\self-contained-win-x64"
#endif

[Setup]
AppId={{6FA842DA-4953-4D86-84D5-EE18BC6B4A16}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=VPNExcludeSetup-{#BuildFlavor}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "installwireguard"; Description: "Установить WireGuard (из локального файла prerequisites)"; Flags: unchecked
Name: "installdotnetdesktop"; Description: "Установить .NET 8 Windows Desktop Runtime x64"; Flags: unchecked; Check: IsFrameworkBuild

[Files]
Source: "{#PublishDir}\\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#PrereqDir}\\{#WireGuardInstaller}"; DestDir: "{tmp}"; Flags: external skipifsourcedoesntexist
Source: "{#PrereqDir}\\{#DotnetDesktopInstaller}"; DestDir: "{tmp}"; Flags: external skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Dirs]
Name: "{localappdata}\VPNExclude"

[Run]
Filename: "{tmp}\{#WireGuardInstaller}"; Description: "Установка WireGuard"; Flags: shellexec waituntilterminated skipifsilent; Tasks: installwireguard; Check: IsWireGuardInstallerAvailable
; ВАЖНО: Если нужна тихая установка .NET, сначала подтвердите актуальные silent-аргументы у Microsoft.
Filename: "{tmp}\{#DotnetDesktopInstaller}"; Description: "Установка .NET 8 Windows Desktop Runtime x64"; Flags: shellexec waituntilterminated skipifsilent; Tasks: installdotnetdesktop; Check: IsDotnetInstallerAvailable
Filename: "{app}\{#AppExeName}"; Description: "Запустить {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
function IsFrameworkBuild(): Boolean;
begin
  Result := '{#BuildFlavor}' = 'framework';
end;

function IsWireGuardInstallerAvailable(): Boolean;
begin
  Result := FileExists(ExpandConstant('{tmp}\\{#WireGuardInstaller}'));
end;

function IsDotnetInstallerAvailable(): Boolean;
begin
  Result := FileExists(ExpandConstant('{tmp}\\{#DotnetDesktopInstaller}'));
end;
