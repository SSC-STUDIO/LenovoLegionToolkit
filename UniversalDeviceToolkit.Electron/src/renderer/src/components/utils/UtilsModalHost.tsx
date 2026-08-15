import InputDialogHost from './InputDialog'
import ActionDetailsModalHost from './ActionDetailsModal'
import CompatibilityCheckErrorModalHost from './CompatibilityCheckErrorModal'
import CrashReportNotificationModalHost from './CrashReportNotificationModal'
import DeviceInformationModalHost from './DeviceInformationModal'
import StatusModalHost from './StatusModal'
import SymbolPickerModalHost from './SymbolPickerModal'
import UnsupportedDeviceModalHost from './UnsupportedDeviceModal'
import UpdateModalHost from './UpdateModal'

/**
 * Mounts the promise-driven hosts of all Electron Windows/Utils ports. Each host is
 * invisible until its `open*`/`show*` helper is called. Add once in the app
 * shell (AppLayout) so any page can open these modals.
 */
export default function UtilsModalHost(): React.JSX.Element {
  return (
    <>
      <InputDialogHost />
      <ActionDetailsModalHost />
      <CompatibilityCheckErrorModalHost />
      <CrashReportNotificationModalHost />
      <DeviceInformationModalHost />
      <StatusModalHost />
      <SymbolPickerModalHost />
      <UnsupportedDeviceModalHost />
      <UpdateModalHost />
    </>
  )
}
