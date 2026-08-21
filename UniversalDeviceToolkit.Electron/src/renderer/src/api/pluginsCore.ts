export type PluginState = 'Installed' | 'NotInstalled'

export interface PluginCapabilities {
  settingsPage: boolean
  featurePage: boolean
  optimizationCategory: boolean
  webPage?: boolean
  executableEntryPoint: boolean
}

export interface PluginView {
  id: string
  name: string
  description: string
  details?: string
  usageGuide?: string
  author: string
  version: string
  icon: string
  iconBackground?: string
  tags: string[]
  isSystemPlugin: boolean
  dependencies: string[]
  changelog?: string
  releaseDate: string
  fileSize: number
  installedVersion?: string
  updateAvailable: boolean
  availableVersion?: string
  state: PluginState
  directory?: string | null
  webPage?: string | null
  capabilities: PluginCapabilities
}

export interface PluginUpdate {
  id: string
  availableVersion: string
}

export interface InstallProgress {
  pluginId: string
  progressPercentage: number
  statusText: string
  phase: 'downloading' | 'completed' | 'failed'
}

export interface PluginInstalledEvent {
  pluginId: string
}

export interface PluginOperationOutcome {
  ok: boolean
  degraded: boolean
  unloadPending: boolean
  recoveryId?: string | null
  recoveryPath?: string | null
  error?: string | null
}

export interface PluginScanOutcome {
  ok: boolean
  registeredCount: number
  degraded: boolean
  unloadPending: boolean
  failures: PluginOperationOutcome[]
}

export interface PluginsApi {
  list: (forceRefresh?: boolean) => Promise<{ plugins: PluginView[]; online: boolean }>
  checkUpdates: () => Promise<{ updates: PluginUpdate[] }>
  install: (pluginId: string) => Promise<PluginOperationOutcome>
  uninstall: (pluginId: string) => Promise<PluginOperationOutcome & { dependencyBlocked?: boolean }>
  importFile: (filePath: string) => Promise<PluginOperationOutcome>
  refresh: () => Promise<PluginScanOutcome>
  onInstallProgress: (callback: (data: InstallProgress) => void) => () => void
  onInstalled: (callback: (data: PluginInstalledEvent) => void) => () => void
  onUninstalled: (callback: (data: PluginInstalledEvent) => void) => () => void
}

export type PluginBridgeInvoke = <T>(method: string, params?: unknown) => Promise<T>
export type PluginBridgeOn = <T>(event: string, callback: (data: T) => void) => () => void

type JsonObject = Record<string, unknown>

function readObject(value: unknown): JsonObject | null {
  if (value == null || typeof value !== 'object' || Array.isArray(value)) return null
  return value as JsonObject
}

function getKey(record: JsonObject | null, ...names: string[]): unknown {
  if (record == null) return undefined
  for (const name of names) {
    if (Object.prototype.hasOwnProperty.call(record, name)) return record[name]
  }
  return undefined
}

function readString(value: unknown): string | null {
  return typeof value === 'string' ? value : null
}

function readNonEmptyString(value: unknown): string | null {
  const text = readString(value)
  if (text == null) return null
  const trimmed = text.trim()
  return trimmed.length > 0 ? trimmed : null
}

function readBool(value: unknown): boolean | null {
  return typeof value === 'boolean' ? value : null
}

function readNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null
}

function readStringList(value: unknown): string[] {
  if (!Array.isArray(value)) return []
  return value.filter((item): item is string => typeof item === 'string')
}

/**
 * Accepts a Host string, a contributes.webPage object, or PascalCase aliases.
 * Missing / blank values stay null so callers never treat empty data as a page.
 */
export function resolvePluginWebPageValue(value: unknown): string | null {
  const direct = readNonEmptyString(value)
  if (direct != null) return direct
  const record = readObject(value)
  if (record == null) return null
  return readNonEmptyString(getKey(record, 'entry', 'Entry', 'webPage', 'WebPage'))
}

export function normalizePluginCapabilities(
  raw: unknown,
  webPage: string | null
): PluginCapabilities {
  const record = readObject(raw)
  return {
    settingsPage: readBool(getKey(record, 'settingsPage', 'SettingsPage')) === true,
    featurePage: readBool(getKey(record, 'featurePage', 'FeaturePage')) === true,
    optimizationCategory:
      readBool(getKey(record, 'optimizationCategory', 'OptimizationCategory')) === true,
    webPage: readBool(getKey(record, 'webPage', 'WebPage')) === true || webPage != null,
    executableEntryPoint:
      readBool(getKey(record, 'executableEntryPoint', 'ExecutableEntryPoint')) === true
  }
}

export function normalizePluginView(raw: unknown): PluginView | null {
  const record = readObject(raw)
  if (record == null) return null
  const id = readNonEmptyString(getKey(record, 'id', 'Id'))
  if (id == null) return null

  const webPage = resolvePluginWebPageValue(getKey(record, 'webPage', 'WebPage'))
  const directory = readNonEmptyString(getKey(record, 'directory', 'Directory'))
  const stateRaw = readNonEmptyString(getKey(record, 'state', 'State'))
  const state: PluginState = stateRaw === 'Installed' ? 'Installed' : 'NotInstalled'

  return {
    id,
    name: readString(getKey(record, 'name', 'Name')) ?? id,
    description: readString(getKey(record, 'description', 'Description')) ?? '',
    details: readNonEmptyString(getKey(record, 'details', 'Details')) ?? undefined,
    usageGuide: readNonEmptyString(getKey(record, 'usageGuide', 'UsageGuide')) ?? undefined,
    author: readString(getKey(record, 'author', 'Author')) ?? '',
    version: readString(getKey(record, 'version', 'Version')) ?? '',
    icon: readString(getKey(record, 'icon', 'Icon')) ?? '',
    iconBackground: readNonEmptyString(getKey(record, 'iconBackground', 'IconBackground')) ?? undefined,
    tags: readStringList(getKey(record, 'tags', 'Tags')),
    isSystemPlugin: readBool(getKey(record, 'isSystemPlugin', 'IsSystemPlugin')) === true,
    dependencies: readStringList(getKey(record, 'dependencies', 'Dependencies')),
    changelog: readNonEmptyString(getKey(record, 'changelog', 'Changelog')) ?? undefined,
    releaseDate: readString(getKey(record, 'releaseDate', 'ReleaseDate')) ?? '',
    fileSize: readNumber(getKey(record, 'fileSize', 'FileSize')) ?? 0,
    installedVersion: readNonEmptyString(getKey(record, 'installedVersion', 'InstalledVersion')) ?? undefined,
    updateAvailable: readBool(getKey(record, 'updateAvailable', 'UpdateAvailable')) === true,
    availableVersion: readNonEmptyString(getKey(record, 'availableVersion', 'AvailableVersion')) ?? undefined,
    state,
    directory,
    webPage,
    capabilities: normalizePluginCapabilities(getKey(record, 'capabilities', 'Capabilities'), webPage)
  }
}

export function normalizePluginListResult(raw: unknown): { plugins: PluginView[]; online: boolean } {
  const record = readObject(raw)
  const pluginsRaw = getKey(record, 'plugins', 'Plugins')
  const plugins = Array.isArray(pluginsRaw)
    ? pluginsRaw
        .map((entry) => normalizePluginView(entry))
        .filter((plugin): plugin is PluginView => plugin != null)
    : []
  return {
    plugins,
    online: readBool(getKey(record, 'online', 'Online')) === true
  }
}

export function normalizePluginUpdates(raw: unknown): { updates: PluginUpdate[] } {
  const record = readObject(raw)
  const updatesRaw = getKey(record, 'updates', 'Updates')
  if (!Array.isArray(updatesRaw)) return { updates: [] }
  const updates: PluginUpdate[] = []
  for (const entry of updatesRaw) {
    const item = readObject(entry)
    const id = readNonEmptyString(getKey(item, 'id', 'Id'))
    const availableVersion = readNonEmptyString(
      getKey(item, 'availableVersion', 'AvailableVersion')
    )
    if (id == null || availableVersion == null) continue
    updates.push({ id, availableVersion })
  }
  return { updates }
}

/**
 * Success requires an explicit true `ok`/`success`. Missing flags, degraded
 * installs, and pending unloads are failures so the UI cannot toast success.
 */
export function normalizePluginOperationOutcome(raw: unknown): PluginOperationOutcome {
  const record = readObject(raw)
  const explicitOk =
    readBool(getKey(record, 'ok', 'Ok')) === true ||
    readBool(getKey(record, 'success', 'Success')) === true
  const degraded = readBool(getKey(record, 'degraded', 'Degraded')) === true
  const unloadPending = readBool(getKey(record, 'unloadPending', 'UnloadPending')) === true
  return {
    ok: explicitOk && !degraded && !unloadPending,
    degraded,
    unloadPending,
    recoveryId: readNonEmptyString(getKey(record, 'recoveryId', 'RecoveryId')),
    recoveryPath: readNonEmptyString(getKey(record, 'recoveryPath', 'RecoveryPath')),
    error: readNonEmptyString(getKey(record, 'error', 'Error', 'message', 'Message'))
  }
}

export function normalizePluginUninstallOutcome(
  raw: unknown
): PluginOperationOutcome & { dependencyBlocked: boolean } {
  const record = readObject(raw)
  return {
    ...normalizePluginOperationOutcome(raw),
    dependencyBlocked: readBool(getKey(record, 'dependencyBlocked', 'DependencyBlocked')) === true
  }
}

export function normalizePluginScanOutcome(raw: unknown): PluginScanOutcome {
  const record = readObject(raw)
  const explicitOk =
    readBool(getKey(record, 'ok', 'Ok')) === true ||
    readBool(getKey(record, 'success', 'Success')) === true
  const degraded = readBool(getKey(record, 'degraded', 'Degraded')) === true
  const unloadPending = readBool(getKey(record, 'unloadPending', 'UnloadPending')) === true
  const failuresRaw = getKey(record, 'failures', 'Failures')
  const failures = Array.isArray(failuresRaw)
    ? failuresRaw.map((entry) => normalizePluginOperationOutcome(entry))
    : []
  return {
    ok: explicitOk && !degraded && !unloadPending,
    registeredCount: readNumber(getKey(record, 'registeredCount', 'RegisteredCount')) ?? 0,
    degraded,
    unloadPending,
    failures
  }
}

function expectInvokeObject(value: unknown, method: string): object {
  if (value == null || typeof value !== 'object') {
    throw new Error(`Host method ${method} returned an invalid result`)
  }
  return value
}

export function createPluginsApi(
  invoke: PluginBridgeInvoke,
  on: PluginBridgeOn
): PluginsApi {
  return {
    async list(forceRefresh) {
      return normalizePluginListResult(
        expectInvokeObject(
          await invoke<unknown>('plugins.list', forceRefresh ? { forceRefresh } : {}),
          'plugins.list'
        )
      )
    },

    async checkUpdates() {
      return normalizePluginUpdates(
        expectInvokeObject(await invoke<unknown>('plugins.checkUpdates', {}), 'plugins.checkUpdates')
      )
    },

    async install(pluginId) {
      return normalizePluginOperationOutcome(
        expectInvokeObject(await invoke<unknown>('plugins.install', { pluginId }), 'plugins.install')
      )
    },

    async uninstall(pluginId) {
      return normalizePluginUninstallOutcome(
        expectInvokeObject(
          await invoke<unknown>('plugins.uninstall', { pluginId }),
          'plugins.uninstall'
        )
      )
    },

    async importFile(filePath) {
      return normalizePluginOperationOutcome(
        expectInvokeObject(await invoke<unknown>('plugins.import', { filePath }), 'plugins.import')
      )
    },

    async refresh() {
      return normalizePluginScanOutcome(
        expectInvokeObject(await invoke<unknown>('plugins.refresh', {}), 'plugins.refresh')
      )
    },

    onInstallProgress(callback) {
      return on<InstallProgress>('plugins.installProgress', callback)
    },

    onInstalled(callback) {
      return on<PluginInstalledEvent>('plugins.installed', callback)
    },

    onUninstalled(callback) {
      return on<PluginInstalledEvent>('plugins.uninstalled', callback)
    }
  }
}
