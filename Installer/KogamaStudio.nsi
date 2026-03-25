!include "MUI2.nsh"
!include "LogicLib.nsh"

Name "KogamaStudio Installer"
OutFile "KogamaStudio-Installer.exe"
InstallDir "$LOCALAPPDATA"
!define MUI_ICON "icon.ico"
!define MUI_UNICON "icon.ico"
BrandingText " "
RequestExecutionLevel user

Var selectedServer
Var hwndCustomRadio
Var hwndBrowseBtn
Var installMode
Var modeInstallBtn
Var modeUninstallBtn

!insertmacro MUI_LANGUAGE "English"

LangString ChooseAction ${LANG_ENGLISH} "Choose action:"
LangString InstallBtn ${LANG_ENGLISH} "Install KogamaStudio"
LangString UninstallBtn ${LANG_ENGLISH} "Uninstall KogamaStudio"
LangString SelectServerInstall ${LANG_ENGLISH} "Select KogamaLauncher Server:"
LangString SelectServerUninstall ${LANG_ENGLISH} "Select KogamaLauncher Server to remove files from:"
LangString CustomFolder ${LANG_ENGLISH} "Custom folder"
LangString RemoveUserData ${LANG_ENGLISH} "Do you want to remove user data from KogamaStudio folder?$\n(Settings, saves, etc. will be deleted)"
LangString UninstallSuccess ${LANG_ENGLISH} "KogamaStudio uninstalled successfully!"
LangString InstallSuccess ${LANG_ENGLISH} "KogamaStudio installed successfully!"

Page Custom ModePage LeaveModeSelectPage
Page Custom ServerSelectPage LeaveServerSelectPage
!insertmacro MUI_PAGE_INSTFILES

Function ModePage
  nsDialogs::Create 1018
  Pop $0
  
  ${NSD_CreateLabel} 0 0 100% 20u "$(ChooseAction)"
  Pop $1
  
  ${NSD_CreateRadioButton} 10u 30u 200u 15u "$(InstallBtn)"
  Pop $modeInstallBtn
  ${NSD_Check} $modeInstallBtn
  
  ${NSD_CreateRadioButton} 10u 50u 200u 15u "$(UninstallBtn)"
  Pop $modeUninstallBtn
  
  nsDialogs::Show
FunctionEnd

Function LeaveModeSelectPage
  ${NSD_GetState} $modeInstallBtn $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $installMode "install"
  ${Else}
    StrCpy $installMode "uninstall"
  ${EndIf}
FunctionEnd

Function ServerSelectPage
  nsDialogs::Create 1018
  Pop $0
  
  ${If} $installMode == "install"
    ${NSD_CreateLabel} 0 0 100% 20u "$(SelectServerInstall)"
  ${Else}
    ${NSD_CreateLabel} 0 0 100% 20u "$(SelectServerUninstall)"
  ${EndIf}
  Pop $1
  
  ${NSD_CreateRadioButton} 10u 30u 200u 15u "KogamaLauncher-BR"
  Pop $3
  
  ${NSD_CreateRadioButton} 10u 50u 200u 15u "KogamaLauncher-Friends"
  Pop $5
  
  ${NSD_CreateRadioButton} 10u 70u 200u 15u "KogamaLauncher-WWW"
  Pop $7
  ${NSD_Check} $7
  
  ${NSD_CreateRadioButton} 10u 90u 200u 15u "$(CustomFolder)"
  Pop $hwndCustomRadio
  
  ${NSD_CreateText} 10u 110u 290u 15u "$LOCALAPPDATA\KogamaLauncher-WWW\Launcher\Standalone"
  Pop $hwndBrowseBtn
  
  nsDialogs::Show
FunctionEnd

Function LeaveServerSelectPage
  ${NSD_GetState} $3 $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $selectedServer "$LOCALAPPDATA\KogamaLauncher-BR\Launcher\Standalone"
    Return
  ${EndIf}
  
  ${NSD_GetState} $5 $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $selectedServer "$LOCALAPPDATA\KogamaLauncher-Friends\Launcher\Standalone"
    Return
  ${EndIf}
  
  ${NSD_GetState} $7 $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $selectedServer "$LOCALAPPDATA\KogamaLauncher-WWW\Launcher\Standalone"
    Return
  ${EndIf}
  
  ${NSD_GetState} $hwndCustomRadio $0
  ${If} $0 == ${BST_CHECKED}
    ${NSD_GetText} $hwndBrowseBtn $selectedServer
    Return
  ${EndIf}
FunctionEnd

Section "Main"
  ${If} $installMode == "uninstall"
    RMDir /r "$selectedServer\BepInEx"
    RMDir /r "$selectedServer\dotnet"
    Delete "$selectedServer\winhttp.dll"
    Delete "$selectedServer\doorstop_config.ini"
    Delete "$selectedServer\.doorstop_version"
    
    MessageBox MB_YESNO "$(RemoveUserData)" IDYES removeData IDNO keepData
    
    removeData:
      RMDir /r "$LOCALAPPDATA\KogamaStudio"
      Goto uninstallDone
    
    keepData:
      Delete "$LOCALAPPDATA\KogamaStudio\Uninstall.exe"
      Goto uninstallDone
    
    uninstallDone:
      DeleteRegKey HKCU "Software\KogamaStudio"
      MessageBox MB_OK "$(UninstallSuccess)"
      Quit
  ${EndIf}
  
  SetOutPath "$LOCALAPPDATA\KogamaStudio"
  File /r "files_for_appdata\KogamaStudio\*.*"
  
  CreateDirectory "$selectedServer"
  SetOutPath "$selectedServer"
  File /r "files_for_launcher\*.*"
  
  WriteUninstaller "$LOCALAPPDATA\KogamaStudio\Uninstall.exe"
  WriteRegStr HKCU "Software\KogamaStudio" "LauncherPath" "$selectedServer"
  
  MessageBox MB_OK "$(InstallSuccess)"
SectionEnd

Section "Uninstall"
  ReadRegStr $0 HKCU "Software\KogamaStudio" "LauncherPath"
  ${If} $0 != ""
    RMDir /r "$0\BepInEx"
    RMDir /r "$0\dotnet"
    Delete "$0\winhttp.dll"
    Delete "$0\doorstop_config.ini"
    Delete "$0\.doorstop_version"
  ${EndIf}
  
  MessageBox MB_YESNO "$(RemoveUserData)" IDYES removeData IDNO keepData
  
  removeData:
    RMDir /r "$LOCALAPPDATA\KogamaStudio"
    Goto done
  
  keepData:
    Delete "$LOCALAPPDATA\KogamaStudio\Uninstall.exe"
    Goto done
  
  done:
    DeleteRegKey HKCU "Software\KogamaStudio"
    MessageBox MB_OK "$(UninstallSuccess)"
SectionEnd