import { useEffect, useState } from 'react'
import {
  ArrowRightOutlined,
  ClockCircleOutlined,
  DesktopOutlined,
  EyeInvisibleOutlined,
  FontSizeOutlined,
  HighlightOutlined,
  KeyOutlined,
  MoonOutlined,
  PoweroffOutlined,
  SlidersOutlined,
  SunOutlined,
  ThunderboltOutlined,
  UsbOutlined
} from '@ant-design/icons'
import type { TFunction } from 'i18next'
import { useTranslation } from 'react-i18next'
import { automationApi } from '../../api/automation'
import type { AutomationPipeline, AutomationStepType } from '../../api/automation'
import { formatStepSummary } from './steps'
import { StepEditor as OtherStepEditor } from './stepEditors'

/**
 * Electron counterpart of the 12 WPF step card controls (AbstractAutomationStepControl /
 * AbstractComboBoxAutomationStepCardControl). Each entry mirrors the WPF wiring:
 * icon + Title/Subtitle resources, parameter collection and serialization shape
 * ($type discriminator + camelCase fields, enums as strings).
 */

/** "AlwaysOnUsbAutomationStep" → "alwaysOnUsb" (mirrors the host discriminator). */
function shortTypeName(type: string): string {
  return type
    .replace(/AutomationStep$/, '')
    .replace(/AutomationPipelineTrigger$/, '')
}

type StepKind = 'select' | 'number' | 'preset' | 'none'

interface StepMeta {
  kind: StepKind
  icon: React.JSX.Element
}

const STEP_META: Record<string, StepMeta> = {
  alwaysOnUsb: { kind: 'select', icon: <UsbOutlined /> },
  battery: { kind: 'select', icon: <ThunderboltOutlined /> },
  batteryNightCharge: { kind: 'select', icon: <MoonOutlined /> },
  deactivateGPU: { kind: 'select', icon: <DesktopOutlined /> },
  delay: { kind: 'select', icon: <ClockCircleOutlined /> },
  displayBrightness: { kind: 'number', icon: <HighlightOutlined /> },
  dpiScale: { kind: 'select', icon: <FontSizeOutlined /> },
  flipToStart: { kind: 'select', icon: <PoweroffOutlined /> },
  fnLock: { kind: 'select', icon: <KeyOutlined /> },
  godModePreset: { kind: 'preset', icon: <SlidersOutlined /> },
  hdr: { kind: 'select', icon: <SunOutlined /> },
  hideMainWindow: { kind: 'none', icon: <EyeInvisibleOutlined /> }
}

export function stepTitleKey(type: string): string {
  return `automation.stepEditors.${type}.title`
}

export function stepDescKey(type: string): string {
  return `automation.stepEditors.${type}.desc`
}

/** Static enum option lists (mirrors GetAllStatesAsync of the feature steps). */
const ENUM_OPTIONS: Record<string, string[]> = {
  alwaysOnUsb: ['Off', 'OnWhenSleeping', 'OnAlways'],
  battery: ['Conservation', 'Normal', 'RapidCharge'],
  batteryNightCharge: ['On', 'Off'],
  deactivateGPU: ['KillApps', 'RestartGPU'],
  flipToStart: ['Off', 'On'],
  fnLock: ['Off', 'On'],
  hdr: ['Off', 'On']
}

/** Default enum state per step — mirrors default(T) used by the WPF palette factories. */
const DEFAULT_ENUM_STATE: Record<string, string> = {
  alwaysOnUsb: 'Off',
  battery: 'Conservation',
  batteryNightCharge: 'On',
  deactivateGPU: 'KillApps',
  flipToStart: 'Off',
  fnLock: 'Off',
  hdr: 'Off'
}

/** DelayAutomationStep.GetAllStatesAsync(). */
const DELAY_OPTIONS = [1, 2, 3, 5]

/** Step types with parameter editors implemented in stepEditors.tsx. */
const OTHER_EDITABLE_TYPES = new Set([
  'run',
  'rgbKeyboardBacklight',
  'speaker',
  'touchpadLock',
  'whiteKeyboardBacklight',
  'winKey',
  'spectrumKeyboardBacklightBrightness',
  'spectrumKeyboardBacklightProfile',
  'spectrumKeyboardBacklightImportProfile'
])

function enumStateLabelKey(type: string, value: string): string {
  if (value === 'On' || value === 'Off') return `automation.state.${value.toLowerCase()}`
  return `automation.stepEditors.${type}.options.${value}`
}

/**
 * Default serialized payload per step type — mirrors the WPF AddStep palette
 * factories (DisplayBrightnessAutomationStep(50), DelayAutomationStep(1), ...).
 */
export function createDefaultStep(type: string): AutomationStepType {
  const step: AutomationStepType = { $type: type }
  if (DEFAULT_ENUM_STATE[type] !== undefined) {
    step.state = DEFAULT_ENUM_STATE[type]
  } else if (type === 'delay') {
    step.state = { delaySeconds: 1 }
  } else if (type === 'displayBrightness') {
    step.brightness = 50
  } else if (type === 'dpiScale') {
    step.state = { scale: 0 }
  } else if (type === 'godModePreset') {
    step.presetId = ''
  }
  return step
}

/** Localized one-line summary of a step's parameters (card subtitle parity). */
export function stepSummaryText(step: AutomationStepType, t: TFunction): string {
  const type = String(step.$type)
  if (ENUM_OPTIONS[type] !== undefined) {
    const state = typeof step.state === 'string' ? step.state : ''
    if (state === '') return ''
    return t(enumStateLabelKey(type, state), { defaultValue: state })
  }
  if (type === 'delay') {
    const state = step.state as { delaySeconds?: unknown } | undefined
    const seconds = typeof state?.delaySeconds === 'number' ? state.delaySeconds : undefined
    return seconds === undefined ? '' : t('automation.stepEditors.delay.second', { count: seconds })
  }
  if (type === 'displayBrightness' || type === 'dpiScale') {
    const raw = type === 'displayBrightness'
      ? step.brightness
      : (step.state as Record<string, unknown> | undefined)?.['scale'] ?? (step.state as Record<string, unknown> | undefined)?.['Scale']
    const value = typeof raw === 'number' && Number.isFinite(raw) ? raw : undefined
    return value === undefined ? '' : t(`automation.stepEditors.${type}.percent`, { value })
  }
  return ''
}

function EnumStateSelect(props: {
  type: string
  step: AutomationStepType
  onChange: (next: AutomationStepType) => void
}): React.JSX.Element {
  const { t } = useTranslation()
  const options = ENUM_OPTIONS[props.type] ?? []
  const current = typeof props.step.state === 'string' ? props.step.state : (options[0] ?? '')
  return (
    <select
      className="udt-select"
      value={current}
      disabled={options.length === 0}
      onChange={(e) => props.onChange({ ...props.step, state: e.target.value })}
    >
      {options.map((o) => (
        <option key={o} value={o}>
          {t(enumStateLabelKey(props.type, o), { defaultValue: o })}
        </option>
      ))}
    </select>
  )
}

function DelaySelect(props: {
  step: AutomationStepType
  onChange: (next: AutomationStepType) => void
}): React.JSX.Element {
  const { t } = useTranslation()
  const state = props.step.state as { delaySeconds?: unknown } | undefined
  const seconds = typeof state?.delaySeconds === 'number' ? state.delaySeconds : DELAY_OPTIONS[0]
  const current = DELAY_OPTIONS.includes(seconds) ? seconds : DELAY_OPTIONS[0]
  return (
    <select
      className="udt-select"
      value={current}
      onChange={(e) => props.onChange({ ...props.step, state: { delaySeconds: Number(e.target.value) } })}
    >
      {DELAY_OPTIONS.map((s) => (
        <option key={s} value={s}>
          {t('automation.stepEditors.delay.second', { count: s })}
        </option>
      ))}
    </select>
  )
}

/** DisplayBrightnessAutomationStepControl — NumberBox 0..100, step 5. */
function BrightnessNumber(props: {
  step: AutomationStepType
  onChange: (next: AutomationStepType) => void
}): React.JSX.Element {
  const { t } = useTranslation()
  const brightness =
    typeof props.step.brightness === 'number' && Number.isFinite(props.step.brightness)
      ? props.step.brightness
      : 50
  return (
    <input
      type="number"
      className="udt-number"
      min={0}
      max={100}
      step={5}
      value={brightness}
      aria-label={t('automation.stepEditors.displayBrightness.title')}
      onChange={(e) => {
        const raw = e.target.valueAsNumber
        const next = Number.isNaN(raw) ? 0 : Math.min(100, Math.max(0, raw))
        props.onChange({ ...props.step, brightness: next })
      }}
    />
  )
}

/** DpiScaleAutomationStepControl — hardware state list via feature.getStates. */
function DpiScaleSelect(props: {
  step: AutomationStepType
  onChange: (next: AutomationStepType) => void
}): React.JSX.Element {
  const { t } = useTranslation()
  const [scales, setScales] = useState<number[]>([])
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    let cancelled = false
    automationApi
      .getFeatureStates('dpiScale')
      .then((res) => {
        if (cancelled) return
        const list = (res.states ?? [])
          .map((s): number => {
            if (typeof s === 'number') return s
            if (s !== null && typeof s === 'object') {
              const rec = s as Record<string, unknown>
              const raw = rec['Scale'] ?? rec['scale']
              return typeof raw === 'number' ? raw : Number.NaN
            }
            return Number.NaN
          })
          .filter((n) => Number.isFinite(n))
          .sort((a, b) => a - b)
        setScales(list)
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setLoaded(true)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const state = props.step.state as Record<string, unknown> | undefined
  const raw = state?.['scale'] ?? state?.['Scale']
  const current = typeof raw === 'number' && scales.includes(raw) ? raw : ''
  const disabled = !loaded || scales.length === 0

  return (
    <select
      className="udt-select"
      value={current}
      disabled={disabled}
      onChange={(e) => props.onChange({ ...props.step, state: { scale: Number(e.target.value) } })}
    >
      {disabled && <option value="">—</option>}
      {scales.map((s) => (
        <option key={s} value={s}>
          {t('automation.stepEditors.dpiScale.percent', { value: s })}
        </option>
      ))}
    </select>
  )
}

interface GodModePresetOption {
  id: string
  name: string
}

/** GodModePresetAutomationStepControl — presets from the godMode settings scope. */
function GodModePresetSelect(props: {
  step: AutomationStepType
  onChange: (next: AutomationStepType) => void
}): React.JSX.Element {
  const [presets, setPresets] = useState<GodModePresetOption[]>([])
  const [activeId, setActiveId] = useState('')
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    let cancelled = false
    automationApi
      .getGodModePresets()
      .then((res) => {
        if (cancelled) return
        const value = (res.value ?? {}) as Record<string, unknown>
        const active = value['activePresetId'] ?? value['ActivePresetId']
        const presetsRaw = value['presets'] ?? value['Presets']
        const list: GodModePresetOption[] = []
        if (presetsRaw !== null && typeof presetsRaw === 'object') {
          for (const [id, info] of Object.entries(presetsRaw as Record<string, unknown>)) {
            const rec = (info ?? {}) as Record<string, unknown>
            const name = rec['name'] ?? rec['Name']
            list.push({ id, name: typeof name === 'string' && name.length > 0 ? name : id })
          }
          list.sort((a, b) => a.name.localeCompare(b.name))
        }
        setPresets(list)
        if (typeof active === 'string') setActiveId(active)
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setLoaded(true)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const requested = typeof props.step.presetId === 'string' ? props.step.presetId : ''

  // ResolveSelectedPreset: requested → active → first by name (WPF parity).
  const current = presets.some((p) => p.id === requested)
    ? requested
    : presets.some((p) => p.id === activeId)
      ? activeId
      : presets.length > 0
        ? presets[0].id
        : ''

  // WPF resolves the selection on load; write the resolved preset back to the step.
  useEffect(() => {
    if (loaded && current !== '' && current !== requested) {
      props.onChange({ ...props.step, presetId: current })
    }
  })

  const disabled = !loaded || presets.length === 0
  return (
    <select
      className="udt-select"
      value={current}
      disabled={disabled}
      onChange={(e) => props.onChange({ ...props.step, presetId: e.target.value })}
    >
      {disabled && <option value="">—</option>}
      {presets.map((p) => (
        <option key={p.id} value={p.id}>
          {p.name}
        </option>
      ))}
    </select>
  )
}

export interface StepEditorModalProps {
  step: AutomationStepType | undefined
  pipelines: AutomationPipeline[]
  onApply: (next: AutomationStepType) => void
  onCancel: () => void
}

/**
 * Modal editor for a pipeline step. Renders the parameter form of the 12 migrated
 * step controls; steps implemented by other feature areas fall back to their
 * editors (stepEditors.tsx), unknown steps only show a summary.
 */
export function StepEditorModal(props: StepEditorModalProps): React.JSX.Element | null {
  const { t } = useTranslation()
  const { step, pipelines, onApply, onCancel } = props
  const [draft, setDraft] = useState<AutomationStepType | undefined>(() => (step ? { ...step } : undefined))

  useEffect(() => {
    setDraft(step ? { ...step } : undefined)
  }, [step])

  if (draft === undefined) return null

  const type = String(draft.$type)
  const meta = STEP_META[type]

  const title =
    meta !== undefined ? t(stepTitleKey(type), { defaultValue: shortTypeName(type) }) : shortTypeName(type)
  const desc = meta !== undefined ? t(stepDescKey(type), { defaultValue: '' }) : ''
  const summary = stepSummaryText(draft, t) || formatStepSummary(draft, t, pipelines)

  return (
    <div className="udt-modal-backdrop" onClick={onCancel}>
      <div className="udt-modal udt-step-modal" onClick={(e) => e.stopPropagation()}>
        <div className="udt-modal__title">{title}</div>
        {desc !== '' && <div className="udt-step-editor__desc">{desc}</div>}
        <div className="udt-step-editor__body">
          {meta !== undefined && meta.kind === 'select' && ENUM_OPTIONS[type] !== undefined && (
            <EnumStateSelect type={type} step={draft} onChange={(next) => setDraft(next)} />
          )}
          {meta !== undefined && type === 'delay' && (
            <DelaySelect step={draft} onChange={(next) => setDraft(next)} />
          )}
          {meta !== undefined && meta.kind === 'number' && (
            <BrightnessNumber step={draft} onChange={(next) => setDraft(next)} />
          )}
          {meta !== undefined && type === 'dpiScale' && (
            <DpiScaleSelect step={draft} onChange={(next) => setDraft(next)} />
          )}
          {meta !== undefined && meta.kind === 'preset' && (
            <GodModePresetSelect step={draft} onChange={(next) => setDraft(next)} />
          )}
          {meta !== undefined && meta.kind === 'none' && (
            <div className="udt-step-editor__empty">{t('automation.noEditableParameters')}</div>
          )}
          {meta === undefined && OTHER_EDITABLE_TYPES.has(type) && (
            <OtherStepEditor step={draft} onChange={(next) => setDraft(next)} />
          )}
          {meta === undefined && !OTHER_EDITABLE_TYPES.has(type) && (
            <div className="udt-step-editor__empty">{t('automation.noEditableParameters')}</div>
          )}
        </div>
        {summary !== '' && <div className="udt-step-editor__summary">{summary}</div>}
        <div className="udt-modal__actions">
          <button type="button" className="udt-btn udt-btn--secondary" onClick={onCancel}>
            {t('common.cancel', { defaultValue: '取消' })}
          </button>
          <button type="button" className="udt-btn udt-btn--primary" onClick={() => onApply(draft)}>
            <ArrowRightOutlined /> {t('common.confirm', { defaultValue: '确定' })}
          </button>
        </div>
      </div>
    </div>
  )
}

export default StepEditorModal

