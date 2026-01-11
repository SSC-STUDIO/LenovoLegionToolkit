#include "InnoDependencies\install_dotnet.iss"

#define MyAppName "Lenovo Legion Toolkit"
#define MyAppNameCompact "LenovoLegionToolkit"
#define MyAppPublisher "Bartosz Cichecki"
#define MyAppURL "https://github.com/BartoszCichecki/LenovoLegionToolkit"
#define MyAppExeName "Lenovo Legion Toolkit.exe"

#ifndef MyAppVersion
  #define MyAppVersion "0.0.1"
#endif

[Setup]
UsedUserAreasWarning=false
AppId={{0C37B9AC-9C3D-4302-8ABB-125C7C7D83D5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userpf}\{#MyAppNameCompact}
DisableProgramGroupPage=yes
LicenseFile=LICENSE
PrivilegesRequired=admin
OutputBaseFilename=LenovoLegionToolkitSetup
Compression=lzma2/ultra64  
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=build_installer
ArchitecturesInstallIn64BitMode=x64compatible

[Code]
function InitializeSetup: Boolean;
begin
  InstallDotNet6DesktopRuntime;
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
  ShellExePath := ExpandConstant('{app}\shell.exe');
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

[Languages]
Name: "en";      MessagesFile: "compiler:Default.isl"
Name: "ptbr";    MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "bg";      MessagesFile: "compiler:Languages\Bulgarian.isl" 
Name: "cs";      MessagesFile: "compiler:Languages\Czech.isl" 
Name: "nlnl";    MessagesFile: "compiler:Languages\Dutch.isl"
Name: "fr";      MessagesFile: "compiler:Languages\French.isl"
Name: "de";      MessagesFile: "compiler:Languages\German.isl"
Name: "hu";      MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "it";      MessagesFile: "compiler:Languages\Italian.isl"
Name: "ja";      MessagesFile: "compiler:Languages\Japanese.isl"
Name: "pl";      MessagesFile: "compiler:Languages\Polish.isl"
Name: "pt";      MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "ru";      MessagesFile: "compiler:Languages\Russian.isl"
Name: "sk";      MessagesFile: "compiler:Languages\Slovak.isl"
Name: "es";      MessagesFile: "compiler:Languages\Spanish.isl"
Name: "tr";      MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukr";     MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "ar";      MessagesFile: "InnoDependencies\Arabic.isl"
Name: "lv";      MessagesFile: "InnoDependencies\Latvian.isl"
Name: "zhhans";  MessagesFile: "InnoDependencies\ChineseSimplified.isl"
Name: "zhhant";  MessagesFile: "InnoDependencies\ChineseTraditional.isl"
Name: "el";      MessagesFile: "InnoDependencies\Greek.isl"
Name: "ro";      MessagesFile: "InnoDependencies\Romanian.isl"
Name: "vi";      MessagesFile: "InnoDependencies\Vietnamese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[InstallDelete]
Type: filesandordirs; Name: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: runascurrentuser nowait postinstall

[UninstallDelete]
Type: files; Name: "{app}\shell.exe"
Type: files; Name: "{app}\shell.dll" 
Type: files; Name: "{app}\shell.nss"
Type: filesandordirs; Name: "{localappdata}\{#MyAppNameCompact}"

[UninstallRun]
; Delete scheduled task
RunOnceId: "DelAutorun"; Filename: "schtasks"; Parameters: "/Delete /TN ""LenovoLegionToolkit_Autorun_6efcc882-924c-4cbc-8fec-f45c25696f98"" /F"; Flags: runhidden 
