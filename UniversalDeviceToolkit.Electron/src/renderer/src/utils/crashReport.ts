/**
 * Crash report model & naming — port of WPF Utils/CrashReportHelper.cs
 * (pure parts). File IO and process-wide handlers live in the host process.
 */
export interface CrashReport {
  timestamp: string
  appVersion: string
  osVersion: string
  runtimeVersion: string
  exceptionType: string
  exceptionMessage: string
  stackTrace: string
  source: string
  uptime: string
  innerExceptionType?: string
  innerExceptionMessage?: string
  innerExceptionStackTrace?: string
}

/** crash_yyyy_MM_dd_HH_mm_ss_fff.json — mirrors the WPF file name pattern. */
export function crashReportFileName(date: Date = new Date()): string {
  const pad = (n: number, width = 2): string => n.toString().padStart(width, '0')
  return (
    `crash_${date.getUTCFullYear()}_${pad(date.getUTCMonth() + 1)}_${pad(date.getUTCDate())}_` +
    `${pad(date.getUTCHours())}_${pad(date.getUTCMinutes())}_${pad(date.getUTCSeconds())}_` +
    `${pad(date.getUTCMilliseconds(), 3)}.json`
  )
}

export function buildCrashReport(input: Partial<CrashReport>): CrashReport {
  return {
    timestamp: new Date().toISOString(),
    appVersion: input.appVersion ?? 'unknown',
    osVersion: input.osVersion ?? navigator.userAgent,
    runtimeVersion: 'Electron/Chromium',
    exceptionType: input.exceptionType ?? 'Unknown',
    exceptionMessage: input.exceptionMessage ?? 'No exception message',
    stackTrace: input.stackTrace ?? 'No stack trace available',
    source: input.source ?? 'Unknown',
    uptime: input.uptime ?? '0',
    ...input,
  }
}

/** Truncates a stack trace to 1200 characters (mirrors the WPF report window). */
export function truncateStackTrace(stackTrace: string, maxLength = 1200): string {
  return stackTrace.length <= maxLength ? stackTrace : `${stackTrace.slice(0, maxLength)}…`
}
