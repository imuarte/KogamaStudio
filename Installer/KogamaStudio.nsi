!include "MUI2.nsh"
!include "LogicLib.nsh"

Name "KogamaStudio Installer"
OutFile "KogamaStudio-Installer.exe"
InstallDir "$LOCALAPPDATA"
Icon "icon.ico"
BrandingText " "
RequestExecutionLevel user

Var selectedServer
Var hwndCustomRadio
Var hwndBrowseBtn
Var installMode
Var modeInstallBtn
Var modeUninstallBtn

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "PortugueseBR"
!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "Polish"
!insertmacro MUI_LANGUAGE "Italian"

; Tłumaczenia
LangString ChooseAction ${LANG_ENGLISH} "Choose action:"
LangString ChooseAction ${LANG_SPANISH} "Elige una acción:"
LangString ChooseAction ${LANG_PORTUGUESEBR} "Escolha uma ação:"
LangString ChooseAction ${LANG_RUSSIAN} "Выберите действие:"
LangString ChooseAction ${LANG_POLISH} "Wybierz akcję:"
LangString ChooseAction ${LANG_ITALIAN} "Scegli un'azione:"

LangString InstallBtn ${LANG_ENGLISH} "Install KogamaStudio"
LangString InstallBtn ${LANG_SPANISH} "Instalar KogamaStudio"
LangString InstallBtn ${LANG_PORTUGUESEBR} "Instalar KogamaStudio"
LangString InstallBtn ${LANG_RUSSIAN} "Установить KogamaStudio"
LangString InstallBtn ${LANG_POLISH} "Zainstaluj KogamaStudio"
LangString InstallBtn ${LANG_ITALIAN} "Installa KogamaStudio"

LangString UninstallBtn ${LANG_ENGLISH} "Uninstall KogamaStudio"
LangString UninstallBtn ${LANG_SPANISH} "Desinstalar KogamaStudio"
LangString UninstallBtn ${LANG_PORTUGUESEBR} "Desinstalar KogamaStudio"
LangString UninstallBtn ${LANG_RUSSIAN} "Удалить KogamaStudio"
LangString UninstallBtn ${LANG_POLISH} "Odinstaluj KogamaStudio"
LangString UninstallBtn ${LANG_ITALIAN} "Disinstalla KogamaStudio"

LangString SelectServerInstall ${LANG_ENGLISH} "Select KogamaLauncher Server:"
LangString SelectServerInstall ${LANG_SPANISH} "Selecciona el servidor de KogamaLauncher:"
LangString SelectServerInstall ${LANG_PORTUGUESEBR} "Selecione o servidor KogamaLauncher:"
LangString SelectServerInstall ${LANG_RUSSIAN} "Выберите сервер KogamaLauncher:"
LangString SelectServerInstall ${LANG_POLISH} "Wybierz serwer KogamaLauncher:"
LangString SelectServerInstall ${LANG_ITALIAN} "Seleziona il server KogamaLauncher:"

LangString SelectServerUninstall ${LANG_ENGLISH} "Select KogamaLauncher Server to remove files from:"
LangString SelectServerUninstall ${LANG_SPANISH} "Selecciona el servidor de KogamaLauncher para eliminar archivos:"
LangString SelectServerUninstall ${LANG_PORTUGUESEBR} "Selecione o servidor KogamaLauncher para remover arquivos:"
LangString SelectServerUninstall ${LANG_RUSSIAN} "Выберите сервер KogamaLauncher для удаления файлов:"
LangString SelectServerUninstall ${LANG_POLISH} "Wybierz serwer KogamaLauncher, z którego usunąć pliki:"
LangString SelectServerUninstall ${LANG_ITALIAN} "Seleziona il server KogamaLauncher da cui rimuovere i file:"

LangString CustomFolder ${LANG_ENGLISH} "Custom folder"
LangString CustomFolder ${LANG_SPANISH} "Carpeta personalizada"
LangString CustomFolder ${LANG_PORTUGUESEBR} "Pasta personalizada"
LangString CustomFolder ${LANG_RUSSIAN} "Другая папка"
LangString CustomFolder ${LANG_POLISH} "Własny folder"
LangString CustomFolder ${LANG_ITALIAN} "Cartella personalizzata"

LangString RemoveUserData ${LANG_ENGLISH} "Do you want to remove user data from KogamaStudio folder?$\n(Settings, saves, etc. will be deleted)"
LangString RemoveUserData ${LANG_SPANISH} "¿Quieres eliminar los datos de usuario de la carpeta KogamaStudio?$\n(Se eliminarán configuraciones, guardados, etc.)"
LangString RemoveUserData ${LANG_PORTUGUESEBR} "Deseja remover os dados do usuário da pasta KogamaStudio?$\n(Configurações, salvamentos, etc. serão excluídos)"
LangString RemoveUserData ${LANG_RUSSIAN} "Удалить пользовательские данные из папки KogamaStudio?$\n(Настройки, сохранения и т.д. будут удалены)"
LangString RemoveUserData ${LANG_POLISH} "Czy chcesz usunąć dane użytkownika z folderu KogamaStudio?$\n(Ustawienia, zapisy itp. zostaną usunięte)"
LangString RemoveUserData ${LANG_ITALIAN} "Vuoi rimuovere i dati utente dalla cartella KogamaStudio?$\n(Impostazioni, salvataggi, ecc. verranno eliminati)"

LangString UninstallSuccess ${LANG_ENGLISH} "KogamaStudio uninstalled successfully!"
LangString UninstallSuccess ${LANG_SPANISH} "¡KogamaStudio desinstalado correctamente!"
LangString UninstallSuccess ${LANG_PORTUGUESEBR} "KogamaStudio desinstalado com sucesso!"
LangString UninstallSuccess ${LANG_RUSSIAN} "KogamaStudio успешно удалён!"
LangString UninstallSuccess ${LANG_POLISH} "KogamaStudio odinstalowany pomyślnie!"
LangString UninstallSuccess ${LANG_ITALIAN} "KogamaStudio disinstallato con successo!"

LangString InstallSuccess ${LANG_ENGLISH} "KogamaStudio v0.2.0 installed successfully!"
LangString InstallSuccess ${LANG_SPANISH} "¡KogamaStudio v0.2.0 instalado correctamente!"
LangString InstallSuccess ${LANG_PORTUGUESEBR} "KogamaStudio v0.2.0 instalado com sucesso!"
LangString InstallSuccess ${LANG_RUSSIAN} "KogamaStudio v0.2.0 успешно установлен!"
LangString InstallSuccess ${LANG_POLISH} "KogamaStudio v0.2.0 zainstalowany pomyślnie!"
LangString InstallSuccess ${LANG_ITALIAN} "KogamaStudio v0.2.0 installato con successo!"

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
    ; Usuń zainstalowane pliki z launchera
    RMDir /r "$selectedServer\MelonLoader"
    RMDir /r "$selectedServer\Mods"
    RMDir /r "$selectedServer\Plugins"
    RMDir /r "$selectedServer\UserData"
    RMDir /r "$selectedServer\UserLibs"
    Delete "$selectedServer\version.dll"
    
    ; Zapytaj o usunięcie danych użytkownika
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
  
  ; Instalacja
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
    RMDir /r "$0\MelonLoader"
    RMDir /r "$0\Mods"
    RMDir /r "$0\Plugins"
    RMDir /r "$0\UserData"
    RMDir /r "$0\UserLibs"
    Delete "$0\version.dll"
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