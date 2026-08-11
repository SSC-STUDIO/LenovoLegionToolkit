; Universal Device Toolkit NSIS installer customizations.
; Mirrors the retired Inno Setup installer's behaviors:
;   - uninstall unregisters Nilesoft Shell (Shell.exe) to release file locks

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
!macroend

!macro customInstall
  ; Nothing extra required at install time.
!macroend
