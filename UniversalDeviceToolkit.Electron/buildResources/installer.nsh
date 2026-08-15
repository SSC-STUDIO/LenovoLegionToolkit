; Universal Device Toolkit NSIS installer customizations.
; The setup wizard collects first-run choices before files are extracted. The
; Electron main process reads the resulting INI file and applies the choices to
; the first launch, so the application does not show duplicate setup dialogs.

!include LogicLib.nsh
!include WinMessages.nsh
!include nsDialogs.nsh

!define UDT_SELECTION_FILE "$INSTDIR\installer-selection.ini"

!ifndef BUILD_UNINSTALLER
Var udtSelectionInitialized
Var udtLanguage
Var udtDeviceMode
Var udtLanguageCombo
Var udtAutoRadio
Var udtBasicRadio

Function UdtLoadSelection
  ${If} $udtSelectionInitialized == "1"
    Return
  ${EndIf}

  ; Defaults also cover silent installs and upgrades made before this wizard
  ; existed. An existing valid selection is retained on an upgrade.
  StrCpy $udtLanguage "zh-CN"
  StrCpy $udtDeviceMode "auto"
  IfFileExists "${UDT_SELECTION_FILE}" 0 udtSelectionValidate
    ReadINIStr $udtLanguage "${UDT_SELECTION_FILE}" "installation" "language"
    ReadINIStr $udtDeviceMode "${UDT_SELECTION_FILE}" "installation" "deviceMode"

  udtSelectionValidate:
  StrCmp $udtLanguage "en" udtLanguageValid
  StrCmp $udtLanguage "zh-CN" udtLanguageValid
  StrCmp $udtLanguage "zh-Hant" udtLanguageValid
  StrCmp $udtLanguage "ja" udtLanguageValid
  StrCmp $udtLanguage "de" udtLanguageValid
  StrCmp $udtLanguage "fr" udtLanguageValid
  StrCmp $udtLanguage "es" udtLanguageValid
  StrCmp $udtLanguage "it" udtLanguageValid
  StrCmp $udtLanguage "pt-BR" udtLanguageValid
  StrCmp $udtLanguage "pt" udtLanguageValid
  StrCmp $udtLanguage "ru" udtLanguageValid
  StrCmp $udtLanguage "uk" udtLanguageValid
  StrCmp $udtLanguage "pl" udtLanguageValid
  StrCmp $udtLanguage "cs" udtLanguageValid
  StrCmp $udtLanguage "sk" udtLanguageValid
  StrCmp $udtLanguage "hu" udtLanguageValid
  StrCmp $udtLanguage "ro" udtLanguageValid
  StrCmp $udtLanguage "bg" udtLanguageValid
  StrCmp $udtLanguage "tr" udtLanguageValid
  StrCmp $udtLanguage "el" udtLanguageValid
  StrCmp $udtLanguage "ar" udtLanguageValid
  StrCmp $udtLanguage "lv" udtLanguageValid
  StrCmp $udtLanguage "nl-NL" udtLanguageValid
  StrCmp $udtLanguage "vi" udtLanguageValid
  StrCmp $udtLanguage "uz-Latn-UZ" udtLanguageValid
  StrCpy $udtLanguage "zh-CN"
  udtLanguageValid:
  StrCmp $udtDeviceMode "basic" udtDeviceModeValid
  StrCpy $udtDeviceMode "auto"
  udtDeviceModeValid:
  StrCpy $udtSelectionInitialized "1"
FunctionEnd

Function UdtLanguagePage
  Call UdtLoadSelection
  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel} 0u 0u 300u 18u "Universal Device Toolkit"
  Pop $0
  ${NSD_CreateLabel} 0u 23u 300u 28u "Choose the language for the application / 选择应用语言"
  Pop $0
  ${NSD_CreateLabel} 0u 57u 300u 18u "Language / 语言"
  Pop $0
  ${NSD_CreateDropList} 0u 77u 300u 120u ""
  Pop $udtLanguageCombo

  ${NSD_CB_AddString} $udtLanguageCombo "English (en)"
  ${NSD_CB_AddString} $udtLanguageCombo "简体中文 (zh-CN)"
  ${NSD_CB_AddString} $udtLanguageCombo "繁體中文 (zh-Hant)"
  ${NSD_CB_AddString} $udtLanguageCombo "日本語 (ja)"
  ${NSD_CB_AddString} $udtLanguageCombo "Deutsch (de)"
  ${NSD_CB_AddString} $udtLanguageCombo "Français (fr)"
  ${NSD_CB_AddString} $udtLanguageCombo "Español (es)"
  ${NSD_CB_AddString} $udtLanguageCombo "Italiano (it)"
  ${NSD_CB_AddString} $udtLanguageCombo "Português (Brasil) (pt-BR)"
  ${NSD_CB_AddString} $udtLanguageCombo "Português (pt)"
  ${NSD_CB_AddString} $udtLanguageCombo "Русский (ru)"
  ${NSD_CB_AddString} $udtLanguageCombo "Українська (uk)"
  ${NSD_CB_AddString} $udtLanguageCombo "Polski (pl)"
  ${NSD_CB_AddString} $udtLanguageCombo "Čeština (cs)"
  ${NSD_CB_AddString} $udtLanguageCombo "Slovenčina (sk)"
  ${NSD_CB_AddString} $udtLanguageCombo "Magyar (hu)"
  ${NSD_CB_AddString} $udtLanguageCombo "Română (ro)"
  ${NSD_CB_AddString} $udtLanguageCombo "Български (bg)"
  ${NSD_CB_AddString} $udtLanguageCombo "Türkçe (tr)"
  ${NSD_CB_AddString} $udtLanguageCombo "Ελληνικά (el)"
  ${NSD_CB_AddString} $udtLanguageCombo "العربية (ar)"
  ${NSD_CB_AddString} $udtLanguageCombo "Latviešu (lv)"
  ${NSD_CB_AddString} $udtLanguageCombo "Nederlands (nl-NL)"
  ${NSD_CB_AddString} $udtLanguageCombo "Tiếng Việt (vi)"
  ${NSD_CB_AddString} $udtLanguageCombo "O'zbek (uz-Latn-UZ)"

  ${If} $udtLanguage == "en"
    ${NSD_CB_SelectString} $udtLanguageCombo "English (en)"
  ${ElseIf} $udtLanguage == "zh-CN"
    ${NSD_CB_SelectString} $udtLanguageCombo "简体中文 (zh-CN)"
  ${ElseIf} $udtLanguage == "zh-Hant"
    ${NSD_CB_SelectString} $udtLanguageCombo "繁體中文 (zh-Hant)"
  ${ElseIf} $udtLanguage == "ja"
    ${NSD_CB_SelectString} $udtLanguageCombo "日本語 (ja)"
  ${ElseIf} $udtLanguage == "de"
    ${NSD_CB_SelectString} $udtLanguageCombo "Deutsch (de)"
  ${ElseIf} $udtLanguage == "fr"
    ${NSD_CB_SelectString} $udtLanguageCombo "Français (fr)"
  ${ElseIf} $udtLanguage == "es"
    ${NSD_CB_SelectString} $udtLanguageCombo "Español (es)"
  ${ElseIf} $udtLanguage == "it"
    ${NSD_CB_SelectString} $udtLanguageCombo "Italiano (it)"
  ${ElseIf} $udtLanguage == "pt-BR"
    ${NSD_CB_SelectString} $udtLanguageCombo "Português (Brasil) (pt-BR)"
  ${ElseIf} $udtLanguage == "pt"
    ${NSD_CB_SelectString} $udtLanguageCombo "Português (pt)"
  ${ElseIf} $udtLanguage == "ru"
    ${NSD_CB_SelectString} $udtLanguageCombo "Русский (ru)"
  ${ElseIf} $udtLanguage == "uk"
    ${NSD_CB_SelectString} $udtLanguageCombo "Українська (uk)"
  ${ElseIf} $udtLanguage == "pl"
    ${NSD_CB_SelectString} $udtLanguageCombo "Polski (pl)"
  ${ElseIf} $udtLanguage == "cs"
    ${NSD_CB_SelectString} $udtLanguageCombo "Čeština (cs)"
  ${ElseIf} $udtLanguage == "sk"
    ${NSD_CB_SelectString} $udtLanguageCombo "Slovenčina (sk)"
  ${ElseIf} $udtLanguage == "hu"
    ${NSD_CB_SelectString} $udtLanguageCombo "Magyar (hu)"
  ${ElseIf} $udtLanguage == "ro"
    ${NSD_CB_SelectString} $udtLanguageCombo "Română (ro)"
  ${ElseIf} $udtLanguage == "bg"
    ${NSD_CB_SelectString} $udtLanguageCombo "Български (bg)"
  ${ElseIf} $udtLanguage == "tr"
    ${NSD_CB_SelectString} $udtLanguageCombo "Türkçe (tr)"
  ${ElseIf} $udtLanguage == "el"
    ${NSD_CB_SelectString} $udtLanguageCombo "Ελληνικά (el)"
  ${ElseIf} $udtLanguage == "ar"
    ${NSD_CB_SelectString} $udtLanguageCombo "العربية (ar)"
  ${ElseIf} $udtLanguage == "lv"
    ${NSD_CB_SelectString} $udtLanguageCombo "Latviešu (lv)"
  ${ElseIf} $udtLanguage == "nl-NL"
    ${NSD_CB_SelectString} $udtLanguageCombo "Nederlands (nl-NL)"
  ${ElseIf} $udtLanguage == "vi"
    ${NSD_CB_SelectString} $udtLanguageCombo "Tiếng Việt (vi)"
  ${Else}
    ${NSD_CB_SelectString} $udtLanguageCombo "O'zbek (uz-Latn-UZ)"
  ${EndIf}
  nsDialogs::Show
FunctionEnd

Function UdtLanguageLeave
  ${NSD_GetText} $udtLanguageCombo $0
  ${If} $0 == "English (en)"
    StrCpy $udtLanguage "en"
  ${ElseIf} $0 == "简体中文 (zh-CN)"
    StrCpy $udtLanguage "zh-CN"
  ${ElseIf} $0 == "繁體中文 (zh-Hant)"
    StrCpy $udtLanguage "zh-Hant"
  ${ElseIf} $0 == "日本語 (ja)"
    StrCpy $udtLanguage "ja"
  ${ElseIf} $0 == "Deutsch (de)"
    StrCpy $udtLanguage "de"
  ${ElseIf} $0 == "Français (fr)"
    StrCpy $udtLanguage "fr"
  ${ElseIf} $0 == "Español (es)"
    StrCpy $udtLanguage "es"
  ${ElseIf} $0 == "Italiano (it)"
    StrCpy $udtLanguage "it"
  ${ElseIf} $0 == "Português (Brasil) (pt-BR)"
    StrCpy $udtLanguage "pt-BR"
  ${ElseIf} $0 == "Português (pt)"
    StrCpy $udtLanguage "pt"
  ${ElseIf} $0 == "Русский (ru)"
    StrCpy $udtLanguage "ru"
  ${ElseIf} $0 == "Українська (uk)"
    StrCpy $udtLanguage "uk"
  ${ElseIf} $0 == "Polski (pl)"
    StrCpy $udtLanguage "pl"
  ${ElseIf} $0 == "Čeština (cs)"
    StrCpy $udtLanguage "cs"
  ${ElseIf} $0 == "Slovenčina (sk)"
    StrCpy $udtLanguage "sk"
  ${ElseIf} $0 == "Magyar (hu)"
    StrCpy $udtLanguage "hu"
  ${ElseIf} $0 == "Română (ro)"
    StrCpy $udtLanguage "ro"
  ${ElseIf} $0 == "Български (bg)"
    StrCpy $udtLanguage "bg"
  ${ElseIf} $0 == "Türkçe (tr)"
    StrCpy $udtLanguage "tr"
  ${ElseIf} $0 == "Ελληνικά (el)"
    StrCpy $udtLanguage "el"
  ${ElseIf} $0 == "العربية (ar)"
    StrCpy $udtLanguage "ar"
  ${ElseIf} $0 == "Latviešu (lv)"
    StrCpy $udtLanguage "lv"
  ${ElseIf} $0 == "Nederlands (nl-NL)"
    StrCpy $udtLanguage "nl-NL"
  ${ElseIf} $0 == "Tiếng Việt (vi)"
    StrCpy $udtLanguage "vi"
  ${Else}
    StrCpy $udtLanguage "uz-Latn-UZ"
  ${EndIf}
FunctionEnd

Function UdtDevicePage
  Call UdtLoadSelection
  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel} 0u 0u 300u 18u "Device configuration / 设备配置"
  Pop $0
  ${NSD_CreateLabel} 0u 25u 300u 30u "Choose how hardware support should start. / 选择硬件支持启动方式。"
  Pop $0
  ${NSD_CreateRadioButton} 10u 65u 285u 24u "Automatically detect supported hardware (recommended) / 自动识别支持的硬件 (推荐)"
  Pop $udtAutoRadio
  ${NSD_CreateRadioButton} 10u 98u 285u 30u "Basic mode — plugins and system optimization only / 基础模式 — 仅插件与系统优化"
  Pop $udtBasicRadio
  ${If} $udtDeviceMode == "basic"
    ${NSD_SetState} $udtBasicRadio ${BST_CHECKED}
  ${Else}
    ${NSD_SetState} $udtAutoRadio ${BST_CHECKED}
  ${EndIf}
  ${NSD_CreateLabel} 10u 143u 285u 28u "You can change this later in Settings. / 稍后可在设置中更改。"
  Pop $0
  nsDialogs::Show
FunctionEnd

Function UdtDeviceLeave
  ${NSD_GetState} $udtAutoRadio $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $udtDeviceMode "auto"
  ${Else}
    StrCpy $udtDeviceMode "basic"
  ${EndIf}
FunctionEnd

; Insert the two first-run setup pages after the installation directory page and
; before the file-copy progress page supplied by electron-builder.
!macro customPageAfterChangeDir
  Page custom UdtLanguagePage UdtLanguageLeave
  Page custom UdtDevicePage UdtDeviceLeave
!macroend
!endif

!macro customUnInstall
  ; Unregister Nilesoft Shell before deleting files so its file locks are released.
  ; This mirrors the Inno Setup InitializeUninstall logic. Explorer restarts as
  ; part of the unregistration; the waits give it time to release the locks.
  StrCpy $R0 "$INSTDIR\Shell.exe"
  IfFileExists $R0 0 uninstallShellDone
    ; silent unregister (also restarts Explorer)
    nsExec::Exec '"$R0" -unregister -treat -restart -silent'
    Sleep 2000
    Sleep 5000
  uninstallShellDone:
  Delete "$INSTDIR\installer-selection.ini"
!macroend

!macro customInstall
  ; The same loader is used for interactive, silent, and upgrade installs.
  ; Silent upgrades therefore retain the existing selection instead of
  ; replacing it with the defaults used for a brand-new unattended install.
  Call UdtLoadSelection
  WriteINIStr "${UDT_SELECTION_FILE}" "installation" "language" "$udtLanguage"
  WriteINIStr "${UDT_SELECTION_FILE}" "installation" "deviceMode" "$udtDeviceMode"
!macroend
