import { invoke, on } from './bridge'
import type { CrashReportInfo } from '../components/utils/CrashReportNotificationModal'

/** Mirror of Lib AutorunState (scheduled-task based startup behavior). */
export type AutorunState = 'Enabled' | 'EnabledDelayed' | 'Disabled'

export interface AutorunResult {
  state: AutorunState
}

export const startupApi = {
  getAutorun: (): Promise<AutorunResult> => invoke<AutorunResult>('app.getAutorun', {}),
  setAutorun: (state: AutorunState): Promise<{ ok: boolean; state: AutorunState }> =>
    invoke<{ ok: boolean; state: AutorunState }>('app.setAutorun', { state }),
  /**
   * Renderer-side crash-report listener — mirrors the WPF crash notification
   * modal triggered by AppDomain/Dispatcher unhandled exceptions. Returns an
   * unsubscribe function.
   */
  onCrashReport: (callback: (report: CrashReportInfo) => void): (() => void) =>
    on<CrashReportInfo>('app.crash', callback)
}
