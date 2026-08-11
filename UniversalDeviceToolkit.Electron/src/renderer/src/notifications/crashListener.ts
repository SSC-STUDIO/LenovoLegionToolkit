import { startupApi } from '../api/startup'
import { openCrashReportNotification } from '../components/utils/CrashReportNotificationModal'

let unsubscribe: (() => void) | undefined

/**
 * Mirrors WPF AppDomain_UnhandledException → CrashReportHelper → crash
 * notification modal: forwards main-process crash reports to the modal host.
 */
export function initCrashReportListener(): () => void {
  if (unsubscribe) {
    return unsubscribe
  }
  unsubscribe = startupApi.onCrashReport((report) => {
    void openCrashReportNotification(report)
  })
  return unsubscribe
}
