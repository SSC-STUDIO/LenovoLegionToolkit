import { settingsApi } from './settings'

/**
 * God Mode settings bridge — mirror of Lib.Settings.GodModeSettings
 * (godmode.json). The host serializes the store with PascalCase property
 * names (LltJson compact options), so both cases are accepted when reading.
 */

export interface GodModeStepperValue {
  value: number
  min: number
  max: number
  step: number
  steps: number[]
  defaultValue: number | null
}

export interface GodModePreset {
  name: string
  powerPlanGuid: string | null
  powerMode: string | null
  sourcePowerMode: string | null
  cpuLongTermPowerLimit: GodModeStepperValue | null
  cpuShortTermPowerLimit: GodModeStepperValue | null
  cpuPeakPowerLimit: GodModeStepperValue | null
  cpuCrossLoadingPowerLimit: GodModeStepperValue | null
  cpuPL1Tau: GodModeStepperValue | null
  apUsPPTPowerLimit: GodModeStepperValue | null
  cpuTemperatureLimit: GodModeStepperValue | null
  gpuPowerBoost: GodModeStepperValue | null
  gpuConfigurableTGP: GodModeStepperValue | null
  gpuTemperatureLimit: GodModeStepperValue | null
  gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline: GodModeStepperValue | null
  gpuToCPUDynamicBoost: GodModeStepperValue | null
  /** FanTable.GetTable() — 10 fan speed steps (0-10). */
  fanTable: number[] | null
  fanFullSpeed: boolean | null
  minValueOffset: number | null
  maxValueOffset: number | null
}

export interface GodModeStore {
  activePresetId: string
  presets: Record<string, GodModePreset>
}

export type GodModeStepperFieldName =
  | 'cpuLongTermPowerLimit'
  | 'cpuShortTermPowerLimit'
  | 'cpuPeakPowerLimit'
  | 'cpuCrossLoadingPowerLimit'
  | 'cpuPL1Tau'
  | 'apUsPPTPowerLimit'
  | 'cpuTemperatureLimit'
  | 'gpuPowerBoost'
  | 'gpuConfigurableTGP'
  | 'gpuTemperatureLimit'
  | 'gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline'
  | 'gpuToCPUDynamicBoost'

export const GOD_MODE_STEPPER_FIELDS: GodModeStepperFieldName[] = [
  'cpuLongTermPowerLimit',
  'cpuShortTermPowerLimit',
  'cpuPeakPowerLimit',
  'cpuCrossLoadingPowerLimit',
  'cpuPL1Tau',
  'apUsPPTPowerLimit',
  'cpuTemperatureLimit',
  'gpuPowerBoost',
  'gpuConfigurableTGP',
  'gpuTemperatureLimit',
  'gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline',
  'gpuToCPUDynamicBoost'
]

/** Wire (PascalCase) name of a stepper field. */
export function stepperWireName(field: GodModeStepperFieldName): string {
  return field[0].toUpperCase() + field.slice(1)
}

type JsonObject = Record<string, unknown>

function readObject(value: unknown): JsonObject | null {
  if (value == null || typeof value !== 'object' || Array.isArray(value)) return null
  return value as JsonObject
}

function getKey(record: JsonObject | null, ...names: string[]): unknown {
  if (record == null) return undefined
  for (const name of names) {
    if (name in record) return record[name]
  }
  return undefined
}

function readString(value: unknown): string | null {
  return typeof value === 'string' ? value : null
}

function readNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null
}

function readBool(value: unknown): boolean | null {
  return typeof value === 'boolean' ? value : null
}

function readArray(value: unknown): unknown[] | null {
  return Array.isArray(value) ? value : null
}

function readStepper(value: unknown): GodModeStepperValue | null {
  const obj = readObject(value)
  if (obj == null) return null
  const rawValue = readNumber(getKey(obj, 'Value', 'value'))
  if (rawValue == null) return null
  const steps = readArray(getKey(obj, 'Steps', 'steps')) ?? []
  const defaultValue = readNumber(getKey(obj, 'DefaultValue', 'defaultValue'))
  return {
    value: rawValue,
    min: readNumber(getKey(obj, 'Min', 'min')) ?? 0,
    max: readNumber(getKey(obj, 'Max', 'max')) ?? 0,
    step: readNumber(getKey(obj, 'Step', 'step')) ?? 1,
    steps: steps.map((step) => readNumber(step)).filter((step): step is number => step != null),
    defaultValue: defaultValue == null ? null : defaultValue
  }
}

function readFanTable(value: unknown): number[] | null {
  const obj = readObject(value)
  if (obj == null) return null
  const speeds: number[] = []
  for (let i = 0; i < 10; i++) {
    const speed = readNumber(getKey(obj, `FSS${i}`, `fss${i}`))
    if (speed == null) return null
    speeds.push(speed)
  }
  return speeds
}

function readPreset(value: unknown): GodModePreset | null {
  const obj = readObject(value)
  if (obj == null) return null
  const name = readString(getKey(obj, 'Name', 'name')) ?? ''
  const preset: GodModePreset = {
    name,
    powerPlanGuid: readString(getKey(obj, 'PowerPlanGuid', 'powerPlanGuid')),
    powerMode: readString(getKey(obj, 'PowerMode', 'powerMode')),
    sourcePowerMode: readString(getKey(obj, 'SourcePowerMode', 'sourcePowerMode')),
    cpuLongTermPowerLimit: null,
    cpuShortTermPowerLimit: null,
    cpuPeakPowerLimit: null,
    cpuCrossLoadingPowerLimit: null,
    cpuPL1Tau: null,
    apUsPPTPowerLimit: null,
    cpuTemperatureLimit: null,
    gpuPowerBoost: null,
    gpuConfigurableTGP: null,
    gpuTemperatureLimit: null,
    gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline: null,
    gpuToCPUDynamicBoost: null,
    fanTable: readFanTable(getKey(obj, 'FanTable', 'fanTable')),
    fanFullSpeed: readBool(getKey(obj, 'FanFullSpeed', 'fanFullSpeed')),
    minValueOffset: readNumber(getKey(obj, 'MinValueOffset', 'minValueOffset')),
    maxValueOffset: readNumber(getKey(obj, 'MaxValueOffset', 'maxValueOffset'))
  }
  for (const field of GOD_MODE_STEPPER_FIELDS) {
    preset[field] = readStepper(getKey(obj, stepperWireName(field), field))
  }
  return preset
}

export function parseGodModeStore(value: unknown): GodModeStore | null {
  const obj = readObject(value)
  if (obj == null) return null
  const activePresetId = readString(getKey(obj, 'ActivePresetId', 'activePresetId')) ?? ''
  const presetsObj = readObject(getKey(obj, 'Presets', 'presets'))
  const presets: Record<string, GodModePreset> = {}
  if (presetsObj != null) {
    for (const [id, presetValue] of Object.entries(presetsObj)) {
      const preset = readPreset(presetValue)
      if (preset != null) presets[id] = preset
    }
  }
  return { activePresetId, presets }
}

function serializeStepper(stepper: GodModeStepperValue): Record<string, unknown> {
  return {
    Value: stepper.value,
    Min: stepper.min,
    Max: stepper.max,
    Step: stepper.step,
    Steps: stepper.steps,
    DefaultValue: stepper.defaultValue
  }
}

function serializeFanTable(table: number[]): Record<string, unknown> {
  const result: Record<string, unknown> = { FSTM: 1, FSID: 0, FSTL: 0 }
  for (let i = 0; i < 10; i++) result[`FSS${i}`] = table[i]
  return result
}

function serializePreset(preset: GodModePreset): Record<string, unknown> {
  const result: Record<string, unknown> = {
    Name: preset.name,
    PowerPlanGuid: preset.powerPlanGuid,
    PowerMode: preset.powerMode,
    SourcePowerMode: preset.sourcePowerMode,
    FanTable: preset.fanTable != null ? serializeFanTable(preset.fanTable) : null,
    FanFullSpeed: preset.fanFullSpeed,
    MinValueOffset: preset.minValueOffset,
    MaxValueOffset: preset.maxValueOffset
  }
  for (const field of GOD_MODE_STEPPER_FIELDS) {
    result[stepperWireName(field)] = preset[field] != null ? serializeStepper(preset[field]!) : null
  }
  return result
}

export function serializeGodModeStore(store: GodModeStore): Record<string, unknown> {
  const presets: Record<string, unknown> = {}
  for (const [id, preset] of Object.entries(store.presets)) {
    presets[id] = serializePreset(preset)
  }
  return {
    ActivePresetId: store.activePresetId,
    Presets: presets
  }
}

export const godModeApi = {
  async load(): Promise<GodModeStore | null> {
    const result = await settingsApi.get('godMode')
    return parseGodModeStore(result.value)
  },
  async save(store: GodModeStore): Promise<void> {
    await settingsApi.set('godMode', serializeGodModeStore(store))
    await settingsApi.save(['godMode'])
  }
}

/** Electron GodModeSettingsWindow.GetUniquePresetName — "Name (2)", "Name (3)", ... */
export function getUniquePresetName(
  requestedName: string,
  presets: Record<string, GodModePreset>,
  excludePresetId?: string
): string {
  const normalized = requestedName.trim() || 'Preset'
  const existing = new Set(
    Object.entries(presets)
      .filter(([id]) => id !== excludePresetId)
      .map(([, preset]) => preset.name.trim())
      .filter((name) => name.length > 0)
      .map((name) => name.toLowerCase())
  )
  if (!existing.has(normalized.toLowerCase())) return normalized
  let suffix = 2
  while (true) {
    const candidate = `${normalized} (${suffix})`
    if (!existing.has(candidate.toLowerCase())) return candidate
    suffix++
  }
}

/** Electron GodModeSettingsWindow.AddPreset — copy of the active preset under a new id. */
export function addPreset(
  store: GodModeStore,
  requestedName: string,
  newPresetId?: string
): GodModeStore {
  const presetId = newPresetId ?? crypto.randomUUID()
  if (store.presets[presetId] != null) throw new Error(`Preset with ID ${presetId} already exists.`)
  const active = store.presets[store.activePresetId]
  if (active == null) throw new Error('Active preset not found.')
  const name = getUniquePresetName(requestedName, store.presets)
  return {
    activePresetId: presetId,
    presets: {
      ...store.presets,
      [presetId]: { ...active, name, sourcePowerMode: null }
    }
  }
}

/** Electron GodModeSettingsWindow.RenameActivePreset. */
export function renameActivePreset(store: GodModeStore, requestedName: string): GodModeStore {
  const preset = store.presets[store.activePresetId]
  if (preset == null) return store
  const name = getUniquePresetName(requestedName, store.presets, store.activePresetId)
  return {
    ...store,
    presets: {
      ...store.presets,
      [store.activePresetId]: { ...preset, name, sourcePowerMode: null }
    }
  }
}

/** Electron GodModeSettingsWindow.DeleteActivePreset — keep at least one preset. */
export function deleteActivePreset(store: GodModeStore): GodModeStore {
  const preset = store.presets[store.activePresetId]
  if (preset == null || Object.keys(store.presets).length <= 1) return store
  const presets: Record<string, GodModePreset> = {}
  for (const [id, value] of Object.entries(store.presets)) {
    if (id !== store.activePresetId) presets[id] = value
  }
  const nextActiveId = Object.entries(presets)
    .sort((a, b) => a[1].name.localeCompare(b[1].name))
    .map(([id]) => id)[0]
  return { activePresetId: nextActiveId ?? store.activePresetId, presets }
}
