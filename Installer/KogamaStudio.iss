[Setup]
AppName=KogamaStudio
AppVersion=0.2.0
DefaultDirName={localappdata}\KogamaStudio
OutputDir=.
OutputBaseFilename=KogamaStudio-Installer
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\Uninstall.exe
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "files_for_appdata\KogamaStudio\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "files_for_launcher\*"; DestDir: "{code:GetLauncherPath}"; Flags: ignoreversion recursesubdirs createallsubdirs uninsneveruninstall

[Code]
var
  ServerPage: TInputOptionWizardPage;
  CustomPathPage: TInputDirWizardPage;

procedure InitializeWizard;
begin
  ServerPage := CreateInputOptionPage(wpWelcome,
    'Select KogamaLauncher Server', '',
    'Choose where to install launcher files:',
    True, False);
  ServerPage.Add('KogamaLauncher-BR');
  ServerPage.Add('KogamaLauncher-Friends');
  ServerPage.Add('KogamaLauncher-WWW');
  ServerPage.Add('Custom folder');
  ServerPage.SelectedValueIndex := 2;
  
  CustomPathPage := CreateInputDirPage(ServerPage.ID,
    'Custom Folder', '',
    'Select custom launcher folder:',
    False, '');
  CustomPathPage.Add('');
  CustomPathPage.Values[0] := ExpandConstant('{localappdata}\KogamaLauncher-WWW\Launcher\Standalone');
end;

function GetLauncherPath(Param: String): String;
begin
  case ServerPage.SelectedValueIndex of
    0: Result := ExpandConstant('{localappdata}\KogamaLauncher-BR\Launcher\Standalone');
    1: Result := ExpandConstant('{localappdata}\KogamaLauncher-Friends\Launcher\Standalone');
    2: Result := ExpandConstant('{localappdata}\KogamaLauncher-WWW\Launcher\Standalone');
    3: Result := CustomPathPage.Values[0];
  end;
  RegWriteStringValue(HKCU, 'Software\KogamaStudio', 'LauncherPath', Result);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = CustomPathPage.ID) and (ServerPage.SelectedValueIndex <> 3);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  LauncherPath: String;
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Pytaj o usuniêcie danych u¿ytkownika
    if MsgBox('Do you want to remove user data from KogamaStudio folder?' + #13#10 + 
              '(Settings, saves, etc. will be deleted)', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{localappdata}\KogamaStudio'), True, True, True);
    end
    else
    begin
      // Usuñ tylko uninstaller, zostaw dane
      DeleteFile(ExpandConstant('{localappdata}\KogamaStudio\Uninstall.exe'));
    end;
    
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\KogamaStudio');
  end;
end;