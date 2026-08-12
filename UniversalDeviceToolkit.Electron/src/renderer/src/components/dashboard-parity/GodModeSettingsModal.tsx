import { useEffect, useState } from 'react'
import { Button, Input, InputNumber, Modal, Select, Spin, message } from 'antd'
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import {
  addPreset,
  deleteActivePreset,
  getUniquePresetName,
  godModeApi,
  renameActivePreset,
  type GodModePreset,
  type GodModeStore
} from '../../api/godMode'
import { useFeaturesStore } from '../../stores/featuresStore'
import FanCurveEditor from '../FanCurveEditor'
import GodModeValueControl from '../dashboard/GodModeValueControl'

/**
 * Parity modal for Electron Windows/Dashboard/GodModeSettingsWindow.
 *
 * Known gaps vs Electron (no matching host bridge methods yet):
 * - "Load" defaults from other power modes (GodModeController.GetDefaultsInOtherPowerModesAsync)
 * - Vantage / Legion Zone running warnings (software disabler status)
 * - Applying values to hardware (GodModeController.ApplyStateAsync); the store
 *   is persisted and the power mode is switched to God Mode, like the Electron Save.
 */

type StepperField =
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

const CPU_FIELDS: StepperField[] = [
  'cpuLongTermPowerLimit',
  'cpuShortTermPowerLimit',
  'cpuPeakPowerLimit',
  'cpuCrossLoadingPowerLimit',
  'cpuPL1Tau',
  'apUsPPTPowerLimit',
  'cpuTemperatureLimit'
]

const GPU_FIELDS: StepperField[] = [
  'gpuPowerBoost',
  'gpuConfigurableTGP',
  'gpuTemperatureLimit',
  'gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline',
  'gpuToCPUDynamicBoost'
]

const STEPPER_TITLE_KEYS: Record<StepperField, string> = {
  cpuLongTermPowerLimit: 'godMode.cpu.longTermPL',
  cpuShortTermPowerLimit: 'godMode.cpu.shortTermPL',
  cpuPeakPowerLimit: 'godMode.cpu.peakPL',
  cpuCrossLoadingPowerLimit: 'godMode.cpu.crossLoading',
  cpuPL1Tau: 'godMode.cpu.pl1Tau',
  apUsPPTPowerLimit: 'godMode.cpu.apuSppt',
  cpuTemperatureLimit: 'godMode.cpu.tempLimit',
  gpuPowerBoost: 'godMode.gpu.dynamicBoost',
  gpuConfigurableTGP: 'godMode.gpu.ctgp',
  gpuTemperatureLimit: 'godMode.gpu.tempLimit',
  gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline: 'godMode.gpu.totalProcessingPowerTarget',
  gpuToCPUDynamicBoost: 'godMode.gpu.toCpuDynamicBoost'
}

const STEPPER_DESC_KEYS: Record<StepperField, string> = {
  cpuLongTermPowerLimit: 'godMode.cpu.longTermPL.desc',
  cpuShortTermPowerLimit: 'godMode.cpu.shortTermPL.desc',
  cpuPeakPowerLimit: 'godMode.cpu.peakPL.desc',
  cpuCrossLoadingPowerLimit: 'godMode.cpu.crossLoading.desc',
  cpuPL1Tau: 'godMode.cpu.pl1Tau.desc',
  apUsPPTPowerLimit: 'godMode.cpu.apuSppt.desc',
  cpuTemperatureLimit: 'godMode.cpu.tempLimit.desc',
  gpuPowerBoost: 'godMode.gpu.dynamicBoost.desc',
  gpuConfigurableTGP: 'godMode.gpu.ctgp.desc',
  gpuTemperatureLimit: 'godMode.gpu.tempLimit.desc',
  gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline: 'godMode.gpu.totalProcessingPowerTarget.desc',
  gpuToCPUDynamicBoost: 'godMode.gpu.toCpuDynamicBoost.desc'
}

const STEPPER_UNITS: Partial<Record<StepperField, string>> = {
  cpuLongTermPowerLimit: 'W',
  cpuShortTermPowerLimit: 'W',
  cpuPeakPowerLimit: 'W',
  cpuCrossLoadingPowerLimit: 'W',
  cpuPL1Tau: 's',
  apUsPPTPowerLimit: 'W',
  cpuTemperatureLimit: '°C',
  gpuPowerBoost: 'W',
  gpuConfigurableTGP: 'W',
  gpuTemperatureLimit: '°C',
  gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline: 'W',
  gpuToCPUDynamicBoost: 'W'
}

interface WorkingValues {
  steppers: Partial<Record<StepperField, number>>
  fanTable: number[] | null
  fanFullSpeed: boolean | null
  maxValueOffset: number | null
  minValueOffset: number | null
}

function emptyWorkingValues(): WorkingValues {
  return { steppers: {}, fanTable: null, fanFullSpeed: null, maxValueOffset: null, minValueOffset: null }
}

function workingFromPreset(preset: GodModePreset): WorkingValues {
  const working = emptyWorkingValues()
  for (const field of [...CPU_FIELDS, ...GPU_FIELDS]) {
    const stepper = preset[field]
    if (stepper != null) working.steppers[field] = stepper.value
  }
  working.fanTable = preset.fanTable
  working.fanFullSpeed = preset.fanFullSpeed
  working.maxValueOffset = preset.maxValueOffset
  working.minValueOffset = preset.minValueOffset
  return working
}

/** Electron GodModeSettingsWindow.BuildActivePresetFromControls (store side only). */
function flushWorkingPreset(preset: GodModePreset, working: WorkingValues): GodModePreset {
  const next: GodModePreset = { ...preset }
  for (const field of [...CPU_FIELDS, ...GPU_FIELDS]) {
    const stepper = next[field]
    const value = working.steppers[field]
    if (stepper != null && value != null) {
      next[field] = { ...stepper, value }
    }
  }
  next.fanTable = working.fanTable
  next.fanFullSpeed = working.fanFullSpeed
  next.maxValueOffset = working.maxValueOffset
  next.minValueOffset = working.minValueOffset
  return next
}

function storeWithPreset(store: GodModeStore, preset: GodModePreset): GodModeStore {
  return { ...store, presets: { ...store.presets, [store.activePresetId]: preset } }
}

/** Electron CardHeaderControl warning text: strip the first line when it looks like a heading. */
function removeWarningHeading(messageText: string): string {
  const lines = messageText.replace(/\r\n/g, '\n').split('\n')
  if (lines.length > 1) {
    const heading = lines[0].trim()
    if (
      heading.length <= 32 &&
      (heading.includes('!') || heading.includes('！') ||
        heading.endsWith(':') || heading.endsWith('：'))
    ) {
      return lines.slice(1).join('\n').trim()
    }
  }
  return messageText.trim()
}

/** Electron TryNormalizeOffsetValue: whole number within [min, max]. */
function normalizeOffset(raw: number | null, minimum: number, maximum: number): number | null {
  if (raw == null || !Number.isFinite(raw)) return null
  if (raw < minimum || raw > maximum || raw !== Math.trunc(raw)) return null
  return raw
}

interface NamePromptState {
  mode: 'add' | 'rename'
}

interface GodModeSettingsModalProps {
  open: boolean
  onClose: () => void
  onSaved?: () => void
}

export default function GodModeSettingsModal({
  open,
  onClose,
  onSaved
}: GodModeSettingsModalProps): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [store, setStore] = useState<GodModeStore | null>(null)
  const [working, setWorking] = useState<WorkingValues>(emptyWorkingValues())
  const [namePrompt, setNamePrompt] = useState<NamePromptState | null>(null)
  const [nameInput, setNameInput] = useState('')

  useEffect(() => {
    if (!open) return
    let cancelled = false
    godModeApi
      .load()
      .then((loaded) => {
        if (cancelled || loaded == null) return
        setStore(loaded)
        const preset = loaded.presets[loaded.activePresetId]
        if (preset != null) setWorking(workingFromPreset(preset))
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          void message.error(`${t('godMode.errorLoad')}: ${(reason as Error).message}`)
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [open, t])

  const presetList = store == null
    ? []
    : Object.entries(store.presets).sort((a, b) => a[1].name.localeCompare(b[1].name))
  const activePreset = store == null ? undefined : store.presets[store.activePresetId]

  const cpuVisible = activePreset != null && CPU_FIELDS.some((field) => activePreset[field] != null)
  const gpuVisible = activePreset != null && GPU_FIELDS.some((field) => activePreset[field] != null)
  const fanCurveVisible = activePreset != null && working.fanTable != null
  const fanFullSpeedVisible = activePreset != null && working.fanFullSpeed != null
  const fanSectionVisible = fanCurveVisible || fanFullSpeedVisible
  const advancedVisible = activePreset != null &&
    (working.maxValueOffset != null || working.minValueOffset != null)

  function updateStepper(field: StepperField, value: number): void {
    setWorking((prev) => {
      const steppers: Partial<Record<StepperField, number>> = { ...prev.steppers, [field]: value }
      if (field === 'cpuLongTermPowerLimit') {
        const short = steppers.cpuShortTermPowerLimit
        if (short != null && value > short) steppers.cpuShortTermPowerLimit = value
      }
      if (field === 'cpuShortTermPowerLimit') {
        const long = steppers.cpuLongTermPowerLimit
        if (long != null && value < long) steppers.cpuLongTermPowerLimit = value
      }
      return { ...prev, steppers }
    })
  }

  async function persist(flushed: GodModeStore): Promise<void> {
    await godModeApi.save(flushed)
    setStore(flushed)
  }

  async function handlePresetSwitch(id: string): Promise<void> {
    if (store == null || activePreset == null || id === store.activePresetId) return
    const flushed = storeWithPreset(store, flushWorkingPreset(activePreset, working))
    const next = { ...flushed, activePresetId: id }
    try {
      await persist(next)
      const preset = next.presets[id]
      if (preset != null) setWorking(workingFromPreset(preset))
    } catch (reason) {
      void message.error(`${t('godMode.errorApply')}: ${(reason as Error).message}`)
    }
  }

  function openNamePrompt(mode: 'add' | 'rename'): void {
    if (mode === 'rename') {
      setNameInput(activePreset?.name ?? '')
    } else {
      const defaultName = getUniquePresetName(
        t('godMode.defaultPresetName'),
        store?.presets ?? {}
      )
      setNameInput(defaultName)
    }
    setNamePrompt({ mode })
  }

  async function confirmNamePrompt(): Promise<void> {
    if (namePrompt == null || store == null) return
    const name = nameInput.trim()
    setNamePrompt(null)
    if (name.length === 0) return

    let next: GodModeStore
    if (namePrompt.mode === 'add') {
      next = addPreset(store, name)
      setWorking(workingFromPreset(next.presets[next.activePresetId]))
    } else {
      next = renameActivePreset(store, name)
    }
    try {
      await persist(next)
    } catch (reason) {
      void message.error(`${t('godMode.errorApply')}: ${(reason as Error).message}`)
    }
  }

  async function handleDeletePreset(): Promise<void> {
    if (store == null || Object.keys(store.presets).length <= 1) return
    const next = deleteActivePreset(store)
    try {
      await persist(next)
      const preset = next.presets[next.activePresetId]
      if (preset != null) setWorking(workingFromPreset(preset))
    } catch (reason) {
      void message.error(`${t('godMode.errorApply')}: ${(reason as Error).message}`)
    }
  }

  function validateOffsets(): string | null {
    if (advancedVisible) {
      const max = normalizeOffset(working.maxValueOffset, 0, 100)
      const min = normalizeOffset(working.minValueOffset, -100, 0)
      if (working.maxValueOffset != null && max == null) return t('godMode.advanced.invalidOffset')
      if (working.minValueOffset != null && min == null) return t('godMode.advanced.invalidOffset')
    }
    return null
  }

  async function apply(): Promise<boolean> {
    if (store == null || activePreset == null) return false
    const invalid = validateOffsets()
    if (invalid != null) {
      void message.error(invalid)
      return false
    }
    try {
      const flushed = storeWithPreset(store, flushWorkingPreset(activePreset, working))
      const powerState = useFeaturesStore.getState().states.powerMode
      if (powerState !== 'GodMode') {
        await useFeaturesStore.getState().setState('powerMode', 'GodMode')
      }
      await persist(flushed)
      message.success(t('godMode.applySuccess'))
      return true
    } catch (reason) {
      void message.error(`${t('godMode.errorApply')}: ${(reason as Error).message}`)
      return false
    }
  }

  async function handleSave(): Promise<void> {
    setSaving(true)
    try {
      await apply()
    } finally {
      setSaving(false)
    }
  }

  async function handleSaveAndClose(): Promise<void> {
    setSaving(true)
    try {
      if (await apply()) {
        onSaved?.()
        onClose()
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open={open}
      title={t('godMode.title')}
      width={920}
      okText={t('common.saveAndClose')}
      cancelText={t('common.cancel')}
      confirmLoading={saving}
      onOk={() => void handleSaveAndClose()}
      onCancel={onClose}
      footer={[
        <Button key="save" loading={saving} onClick={() => void handleSave()}>
          {t('common.save')}
        </Button>,
        <Button key="save-close" type="primary" loading={saving} onClick={() => void handleSaveAndClose()}>
          {t('common.saveAndClose')}
        </Button>
      ]}
      styles={{ body: { maxHeight: 'calc(100vh - 200px)', overflowY: 'auto' } }}
    >
      {loading ? (
        <div className="udt-dashboard-edit__loading">
          <Spin size="large" />
        </div>
      ) : store == null || activePreset == null ? (
        <div className="udt-dashboard-edit__error">{t('godMode.errorLoad')}</div>
      ) : (
        <div className="udt-god-mode">
          <div className="udt-god-mode__preset-row">
            <Select
              className="udt-god-mode__preset-select"
              aria-label={t('godMode.activePreset')}
              value={store.activePresetId}
              options={presetList.map(([id, preset]) => ({ value: id, label: preset.name }))}
              onChange={(value) => void handlePresetSwitch(value)}
            />
            <Button
              icon={<EditOutlined />}
              title={t('common.rename')}
              onClick={() => openNamePrompt('rename')}
            />
            <Button
              icon={<DeleteOutlined />}
              title={t('common.delete')}
              disabled={Object.keys(store.presets).length <= 1}
              onClick={() => void handleDeletePreset()}
            />
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => openNamePrompt('add')}
            >
              {t('common.add')}
            </Button>
          </div>

          {cpuVisible && (
            <div className="udt-god-mode__section">
              <h3 className="udt-god-mode__section-title">{t('godMode.cpu.title')}</h3>
              {CPU_FIELDS.map((field) => {
                const stepper = activePreset[field]
                if (stepper == null) return null
                return (
                  <GodModeValueControl
                    key={field}
                    title={t(STEPPER_TITLE_KEYS[field])}
                    description={t(STEPPER_DESC_KEYS[field])}
                    unit={STEPPER_UNITS[field] ?? ''}
                    stepper={{
                      steps: stepper.steps.length > 0 ? stepper.steps : undefined,
                      min: stepper.min,
                      max: stepper.max,
                      step: stepper.step,
                      value: working.steppers[field],
                      defaultValue: stepper.defaultValue ?? undefined
                    }}
                    onChange={(value) => updateStepper(field, value)}
                  />
                )
              })}
            </div>
          )}

          {gpuVisible && (
            <div className="udt-god-mode__section">
              <h3 className="udt-god-mode__section-title">{t('godMode.gpu.title')}</h3>
              {GPU_FIELDS.map((field) => {
                const stepper = activePreset[field]
                if (stepper == null) return null
                return (
                  <GodModeValueControl
                    key={field}
                    title={t(STEPPER_TITLE_KEYS[field])}
                    description={t(STEPPER_DESC_KEYS[field])}
                    unit={STEPPER_UNITS[field] ?? ''}
                    stepper={{
                      steps: stepper.steps.length > 0 ? stepper.steps : undefined,
                      min: stepper.min,
                      max: stepper.max,
                      step: stepper.step,
                      value: working.steppers[field],
                      defaultValue: stepper.defaultValue ?? undefined
                    }}
                    onChange={(value) => updateStepper(field, value)}
                  />
                )
              })}
            </div>
          )}

          {fanSectionVisible && (
            <div className="udt-god-mode__section">
              <h3 className="udt-god-mode__section-title">{t('godMode.fans.title')}</h3>
              {fanCurveVisible && (
                <div className="udt-god-mode__card">
                  <div className="udt-god-mode__card-title">{t('godMode.fans.curve')}</div>
                  <div className="udt-god-mode__card-desc">{t('godMode.fans.curveMessage')}</div>
                  <FanCurveEditor
                    value={working.fanTable ?? []}
                    disabled={working.fanFullSpeed === true}
                    onChange={(value) => setWorking((prev) => ({ ...prev, fanTable: value }))}
                  />
                </div>
              )}
              {fanFullSpeedVisible && (
                <div className="udt-god-mode__card">
                  <div className="udt-god-mode__card-row">
                    <div className="udt-god-mode__card-copy">
                      <div className="udt-god-mode__card-title">{t('godMode.fans.maxSpeed')}</div>
                      {working.fanFullSpeed === true && (
                        <div className="udt-god-mode__card-warning">
                          {removeWarningHeading(t('godMode.fans.maxSpeedWarning'))}
                        </div>
                      )}
                    </div>
                    <input
                      type="checkbox"
                      className="udt-dashboard-edit__checkbox"
                      checked={working.fanFullSpeed === true}
                      onChange={(event) => {
                        setWorking((prev) => ({ ...prev, fanFullSpeed: event.target.checked }))
                      }}
                    />
                    <span className="udt-dashboard-edit__switch" aria-hidden="true" />
                  </div>
                </div>
              )}
            </div>
          )}

          {advancedVisible && (
            <div className="udt-god-mode__section">
              <h3 className="udt-god-mode__section-title">{t('godMode.advanced.title')}</h3>
              <div className="udt-god-mode__advanced-hint">{t('godMode.advanced.message')}</div>
              {working.maxValueOffset != null && (
                <div className="udt-god-mode__card">
                  <div className="udt-god-mode__card-row">
                    <div className="udt-god-mode__card-copy">
                      <div className="udt-god-mode__card-title">{t('godMode.advanced.maxOffset')}</div>
                      {normalizeOffset(working.maxValueOffset, 0, 100) !== 0 && (
                        <div className="udt-god-mode__card-warning">
                          {removeWarningHeading(t('godMode.advanced.maxOffsetWarning'))}
                        </div>
                      )}
                    </div>
                    <InputNumber
                      min={0}
                      max={100}
                      precision={0}
                      value={working.maxValueOffset}
                      onChange={(value) =>
                        setWorking((prev) => ({ ...prev, maxValueOffset: value ?? 0 }))
                      }
                    />
                  </div>
                </div>
              )}
              {working.minValueOffset != null && (
                <div className="udt-god-mode__card">
                  <div className="udt-god-mode__card-row">
                    <div className="udt-god-mode__card-copy">
                      <div className="udt-god-mode__card-title">{t('godMode.advanced.minOffset')}</div>
                      {normalizeOffset(working.minValueOffset, -100, 0) !== 0 && (
                        <div className="udt-god-mode__card-warning">
                          {removeWarningHeading(t('godMode.advanced.minOffsetWarning'))}
                        </div>
                      )}
                    </div>
                    <InputNumber
                      min={-100}
                      max={0}
                      precision={0}
                      value={working.minValueOffset}
                      onChange={(value) =>
                        setWorking((prev) => ({ ...prev, minValueOffset: value ?? 0 }))
                      }
                    />
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      <Modal
        open={namePrompt != null}
        title={t('godMode.presetName')}
        okText={t('common.ok')}
        cancelText={t('common.cancel')}
        onOk={() => void confirmNamePrompt()}
        onCancel={() => setNamePrompt(null)}
        destroyOnHidden
      >
        <Input
          autoFocus
          value={nameInput}
          onChange={(event) => setNameInput(event.target.value)}
          onPressEnter={() => void confirmNamePrompt()}
        />
      </Modal>
    </Modal>
  )
}
