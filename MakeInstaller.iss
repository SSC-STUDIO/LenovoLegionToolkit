#include "InnoDependencies\Install_dotnet.iss"

#define MyAppName "Universal Device Toolkit"
#define MyAppNameCompact "UniversalDeviceToolkit"
#define MyAppNameCompactLegacy "LenovoLegionToolkit"
#define MyAppPublisher "SSC-STUDIO"
#define MyAppURL "https://github.com/SSC-STUDIO/UniversalDeviceToolkit"
#define MyAppExeName "Universal Device Toolkit.exe"

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#ifndef MyAppSourceDir
  #define MyAppSourceDir "Build"
#endif

#ifndef MyAppOutputBaseFilename
  #define MyAppOutputBaseFilename "UniversalDeviceToolkitSetup-Full"
#endif

#ifndef MyAppId
  #define MyAppId "{{0C37B9AC-9C3D-4302-8ABB-125C7C7D83D5}}"
#endif

#ifndef MyAppPrivilegesRequired
  #define MyAppPrivilegesRequired "admin"
#endif

[Setup]
UsedUserAreasWarning=false
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userpf}\{#MyAppNameCompact}
AppendDefaultDirName=no
DisableProgramGroupPage=yes
LicenseFile=LICENSE
PrivilegesRequired={#MyAppPrivilegesRequired}
OutputBaseFilename={#MyAppOutputBaseFilename}
Compression=lzma2/ultra64  
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=BuildInstaller
ArchitecturesInstallIn64BitMode=x64compatible

[Code]
function InitializeSetup: Boolean;
begin
  InstallDotNetDesktopRuntime;
  Result := True;
end;

function InitializeUninstall(): Boolean;
var
  ShellExePath: String;
  ResultCode: Integer;
begin
  Result := True;
  
  // Uninstall Nilesoft Shell before uninstalling the main application
  // This releases file locks so files can be deleted
  ShellExePath := ExpandConstant('{app}\Shell.exe');
  if FileExists(ShellExePath) then
  begin
    try
      // Unregister Nilesoft Shell (this will restart Explorer)
      // Use -silent flag to avoid showing any message boxes
      if Exec(ShellExePath, '-unregister -treat -restart -silent', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        // Wait for Explorer to restart and release file locks
        // Explorer restart can take a few seconds
        Sleep(2000); // Initial wait for Explorer to start restarting
        Sleep(5000); // Additional wait to ensure Explorer has fully restarted and released locks
        // Total wait time: ~7 seconds, which should be enough for Explorer restart
      end
      else
      begin
        // If execution failed, still wait a bit in case Explorer is restarting
        Sleep(3000);
      end;
    except
      // If uninstall fails, wait a bit anyway - files may still be locked
      Sleep(3000);
    end;
  end;
end;

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "NOTICE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
#ifndef MyAppSkipIcons
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
#endif

[InstallDelete]
Type: filesandordirs; Name: "{app}"

[Run]
#ifndef MyAppSkipPostInstallRun
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: runascurrentuser nowait postinstall skipifsilent
#endif

[UninstallDelete]
Type: files; Name: "{app}\Shell.exe"
Type: files; Name: "{app}\Shell.dll"
Type: files; Name: "{app}\Shell.nss"
Type: filesandordirs; Name: "{app}\ar"
Type: filesandordirs; Name: "{app}\bg"
Type: filesandordirs; Name: "{app}\bs"
Type: filesandordirs; Name: "{app}\ca"
Type: filesandordirs; Name: "{app}\cs"
Type: filesandordirs; Name: "{app}\de"
Type: filesandordirs; Name: "{app}\el"
Type: filesandordirs; Name: "{app}\en"
Type: filesandordirs; Name: "{app}\es"
Type: filesandordirs; Name: "{app}\fr"
Type: filesandordirs; Name: "{app}\hu"
Type: filesandordirs; Name: "{app}\it"
Type: filesandordirs; Name: "{app}\ja"
Type: filesandordirs; Name: "{app}\ko"
Type: filesandordirs; Name: "{app}\lv"
Type: filesandordirs; Name: "{app}\nl"
Type: filesandordirs; Name: "{app}\nl-nl"
Type: filesandordirs; Name: "{app}\nl-NL"
Type: filesandordirs; Name: "{app}\no"
Type: filesandordirs; Name: "{app}\pl"
Type: filesandordirs; Name: "{app}\pt"
Type: filesandordirs; Name: "{app}\pt-br"
Type: filesandordirs; Name: "{app}\pt-BR"
Type: filesandordirs; Name: "{app}\ro"
Type: filesandordirs; Name: "{app}\ru"
Type: filesandordirs; Name: "{app}\sk"
Type: filesandordirs; Name: "{app}\tr"
Type: filesandordirs; Name: "{app}\uk"
Type: filesandordirs; Name: "{app}\uz"
Type: filesandordirs; Name: "{app}\uz-latn-uz"
Type: filesandordirs; Name: "{app}\uz-Latn-UZ"
Type: filesandordirs; Name: "{app}\vi"
Type: filesandordirs; Name: "{app}\zh"
Type: filesandordirs; Name: "{app}\zh-hans"
Type: filesandordirs; Name: "{app}\zh-Hans"
Type: filesandordirs; Name: "{app}\zh-hant"
Type: filesandordirs; Name: "{app}\zh-Hant"
#ifdef MyAppDeleteLocalAppData
Type: filesandordirs; Name: "{localappdata}\{#MyAppNameCompact}"
#endif

[UninstallRun]
; Delete scheduled task
#ifndef MyAppSkipUninstallTasks
RunOnceId: "DelAutorunNew"; Filename: "schtasks"; Parameters: "/Delete /TN ""UniversalDeviceToolkit_Autorun_6efcc882-924c-4cbc-8fec-f45c25696f98"" /F"; Flags: runhidden
RunOnceId: "DelAutorun"; Filename: "schtasks"; Parameters: "/Delete /TN ""LenovoLegionToolkit_Autorun_6efcc882-924c-4cbc-8fec-f45c25696f98"" /F"; Flags: runhidden
#endif
