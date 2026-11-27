#ifndef AppBuildDir
  #define AppBuildDir "bin\x64\Release\net11.0-windows10.0.17763.0\win-x64"
#endif

[Files]
Source: "{#AppBuildDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
#define MyAppName "Creations Mod Manager"
#define MyAppPublisher "hst"
#define MyAppURL "https://github.com/hst12/Starfield-Creations-Mod-Manager-and-Catalog-Fixer"
#define MyAppExeName "Creations Mod Manager.exe"
#define VerFileNum FileOpen("Common\App Version.txt")
#define MyAppVersion FileRead(VerFileNum)
#expr FileClose(VerFileNum)
#define MyAppUpdatesUrl "https://github.com/hst12/Starfield-Creations-Mod-Manager-and-Catalog-Fixer/releases"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{0BA2760B-81E9-49B8-8EAB-AEB9A36CAAF4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppUpdatesUrl}
UsePreviousAppDir=yes
DisableDirPage=no
AlwaysShowDirOnReadyPage=yes
DefaultDirName={commonpf64}\{#MyAppName}
MinVersion=10.0.17763
UninstallDisplayIcon={app}\{#MyAppExeName}
; "ArchitecturesAllowed=x64compatible" specifies that Setup cannot run
; on anything but x64 and Windows 11 on Arm.
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" requests that the
; install be done in "64-bit mode" on x64 or Windows 11 on Arm,
; meaning it should use the native 64-bit Program Files directory and
; the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64compatible
; Remove the following line to run in administrative install mode (install for all users).
; PrivilegesRequired=lowest
SetupLogging=yes
OutputBaseFilename=hstCMM Installer
SolidCompression=no
Compression=zip
WizardStyle=modern
SetupIconFile=SpaceShip.ico
WizardSmallImageFile=SpaceShip.png

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  RequiredRuntimeSeries = '11.0.';
  RuntimeDownloadPage = 'https://dotnet.microsoft.com/download/dotnet/11.0';

function HasRuntime(const DotNetRoot, Framework: String): Boolean;
var
  FrameworkDir, Patch: String;
  Runtime: TFindRec;
begin
  Result := False;
  FrameworkDir := AddBackslash(DotNetRoot) + 'shared\' + Framework + '\';
  if FindFirst(FrameworkDir + RequiredRuntimeSeries + '*', Runtime) then
  begin
    try
      repeat
        if (Runtime.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Patch := Copy(Runtime.Name, Length(RequiredRuntimeSeries) + 1, Length(Runtime.Name));
          if StrToIntDef(Patch, -1) >= 0 then
            if FileExists(FrameworkDir + Runtime.Name + '\' + Framework + '.deps.json') then
            begin
              Result := True;
              Exit;
            end;
        end;
      until not FindNext(Runtime);
    finally
      FindClose(Runtime);
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  DotNetRoot, MessageText: String;
  ErrorCode: Integer;
begin
  if not RegQueryStringValue(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64',
    'InstallLocation', DotNetRoot) or (DotNetRoot = '') then
  begin
    DotNetRoot := ExpandConstant('{commonpf64}\dotnet');
    if IsArm64 then
      DotNetRoot := DotNetRoot + '\x64';
  end;

  Result := HasRuntime(DotNetRoot, 'Microsoft.NETCore.App') and
    HasRuntime(DotNetRoot, 'Microsoft.WindowsDesktop.App');
  if not Result then
  begin
    Log('The .NET 10 Desktop Runtime (x64) is required. Download it from ' + RuntimeDownloadPage);
    if WizardSilent then
      Exit;

    MessageText := 'Creations Mod Manager requires the .NET 10 Desktop Runtime (x64).' + #13#10#13#10 +
      'On the Microsoft download page, choose the Windows x64 installer under .NET Desktop Runtime.' + #13#10 +
      'Install it, then run Setup again.' + #13#10#13#10 + RuntimeDownloadPage;
    if MsgBox(MessageText + #13#10#13#10 + 'Open the Microsoft download page now?',
      mbConfirmation, MB_YESNO) = IDYES then
      if not ShellExecAsOriginalUser('open', RuntimeDownloadPage, '', '',
        SW_SHOWNORMAL, ewNoWait, ErrorCode) then
      begin
        Log('Could not open the .NET download page: ' + SysErrorMessage(ErrorCode));
        MsgBox('Setup could not open your browser. Open this address manually:' + #13#10#13#10 +
          RuntimeDownloadPage, mbError, MB_OK);
      end;
  end;
end;
