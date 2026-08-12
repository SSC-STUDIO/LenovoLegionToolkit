import { useEffect, useState } from 'react'
import {
  ArrowDownOutlined,
  ArrowRightOutlined,
  ArrowUpOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  PlayCircleOutlined,
  SettingOutlined
} from '@ant-design/icons'
import { Tooltip } from 'antd'
import { useTranslation } from 'react-i18next'
import type { AutomationPipeline, AutomationStepType } from '../api/automation'
import { useAutomationStore } from '../stores/automationStore'
import { useLoadingStore } from '../stores/loadingStore'
import CardExpander from '../components/CardExpander'
import { SkeletonList } from '../components/Skeleton'
import { StepEditorModal, createDefaultStep, stepSummaryText } from '../components/automation/StepEditor'
import { formatStepSummary } from '../components/automation/steps'
import { QUICK_ACTION_ICON, triggerIcon } from '../components/automation/triggerMeta'
import { stepIcon } from '../components/automation/stepIcons'
import TriggerPickerModal from '../components/automation/TriggerPickerModal'
import TriggerConfigModal from '../components/automation/TriggerConfigModal'
import type { AutomationTrigger } from '../components/automation/triggers'
import {
  isTriggerConfigurable,
  normalizeTriggerKind,
  triggerDisplayNameKey
} from '../components/automation/triggers'
import { openSymbolPicker } from '../components/utils/SymbolPickerModal'
import { symbolIcon } from '../components/utils/symbolIcons'
import '../components/automation/automation.css'

function shortTypeName(type: string): string {
  return type
    .replace(/AutomationStep$/, '')
    .replace(/AutomationPipelineTrigger$/, '')
}

interface EditingStepTarget {
  pipelineId: string
  index: number
}

interface ContextMenuState {
  id: string
  x: number
  y: number
}

function clampToViewport(x: number, y: number, width = 200, height = 180): { x: number; y: number } {
  const margin = 8
  return {
    x: Math.max(margin, Math.min(x, window.innerWidth - width - margin)),
    y: Math.max(margin, Math.min(y, window.innerHeight - height - margin))
  }
}

const DEACTIVATE_GPU_STABLE = '__udt.quickAction.deactivateGpu'
const DEACTIVATE_GPU_ALIASES = new Set([
  DEACTIVATE_GPU_STABLE,
  '停用 GPU',
  '停用GPU',
  'Deactivate GPU',
  '強制休眠獨顯',
  '休眠独立显卡',
  'Deaktiviere GPU'
])

export default function AutomationPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { state, steps, loading: automationLoading, load, setEnabled, save, runNow } = useAutomationStore()
  const [pipelines, setPipelines] = useState<AutomationPipeline[]>([])
  const [dirty, setDirty] = useState(false)
  const [createOpen, setCreateOpen] = useState(false)
  const [createName, setCreateName] = useState('')
  const [pickerOpen, setPickerOpen] = useState(false)
  const [pendingTrigger, setPendingTrigger] = useState<AutomationTrigger | null>(null)
  const [configFor, setConfigFor] = useState<string | null>(null)
  const [addStepFor, setAddStepFor] = useState<string | null>(null)
  const [selectedStepType, setSelectedStepType] = useState<string>('')
  const [editingStep, setEditingStep] = useState<EditingStepTarget | null>(null)
  const [contextMenu, setContextMenu] = useState<ContextMenuState | null>(null)
  const [renaming, setRenaming] = useState<{ id: string; name: string } | null>(null)
  const [renameName, setRenameName] = useState('')
  const [expandedIds, setExpandedIds] = useState<Record<string, boolean>>({})

  useEffect(() => {
    const loadingId = useLoadingStore.getState().start(
      t('loading.automation', { defaultValue: 'Loading automation…' }),
      { canCancel: false }
    )
    void load()
      .then(() => {
        const latest = useAutomationStore.getState().state
        setPipelines(latest?.pipelines ?? [])
        setDirty(false)
      })
      .finally(() => {
        useLoadingStore.getState().finish(loadingId)
      })
    return () => {
      useLoadingStore.getState().finish(loadingId)
    }
  }, [load])

  const automatic = pipelines.filter((pipeline) => pipeline.trigger != null)
  const manual = pipelines.filter((pipeline) => pipeline.trigger == null)

  const markDirty = (next: AutomationPipeline[]): void => {
    setPipelines(next)
    setDirty(true)
  }

  const handleSave = async (): Promise<void> => {
    try {
      await save(pipelines, state?.isEnabled)
      const latest = useAutomationStore.getState().state
      setPipelines(latest?.pipelines ?? [])
      setDirty(false)
    } catch {
      // ignored
    }
  }

  const handleRevert = (): void => {
    void load().then(() => {
      const latest = useAutomationStore.getState().state
      setPipelines(latest?.pipelines ?? [])
      setDirty(false)
    })
  }

  const handleCreate = (): void => {
    if (!createName.trim()) return
    const id = crypto.randomUUID()
    const pipeline: AutomationPipeline = {
      id,
      name: createName.trim(),
      trigger: pendingTrigger,
      steps: [],
      isExclusive: pendingTrigger != null
    }
    markDirty([...pipelines, pipeline])
    setExpandedIds((prev) => ({ ...prev, [id]: true }))
    setCreateOpen(false)
    setCreateName('')
    setPendingTrigger(null)
  }

  const localizeStoredName = (name: string): string => {
    if (DEACTIVATE_GPU_ALIASES.has(name)) {
      return t('automation.deactivateGpu', { defaultValue: 'Deactivate GPU' })
    }
    return name
  }

  const pipelineTitle = (pipeline: AutomationPipeline): string => {
    const stored = pipeline.name?.trim() ?? ''
    if (stored !== '') return localizeStoredName(stored)
    if (pipeline.trigger != null) {
      return t(triggerDisplayNameKey(pipeline.trigger as AutomationTrigger), {
        defaultValue: shortTypeName(String(pipeline.trigger['$type']))
      })
    }
    return t('wpf.automationPipelineControlunnamed', { defaultValue: t('automation.quickAction') })
  }

  const pipelineSubtitle = (pipeline: AutomationPipeline): string => {
    const count = pipeline.steps?.length ?? 0
    const stepKey =
      count === 1 ? 'wpf.automationPipelineControlstep' : 'wpf.automationPipelineControlstepmany'
    return t(stepKey, { defaultValue: t('automation.steps') + ` (${count})` }).replace(
      '{0}',
      String(count)
    )
  }

  const setExclusive = (id: string, isExclusive: boolean): void => {
    markDirty(pipelines.map((p) => (p.id === id ? { ...p, isExclusive } : p)))
  }

  const toggleExpanded = (id: string, expanded: boolean): void => {
    setExpandedIds((prev) => ({ ...prev, [id]: expanded }))
  }

  const openCreateForTrigger = (trigger: AutomationTrigger): void => {
    setPickerOpen(false)
    setPendingTrigger(trigger)
    setCreateOpen(true)
  }

  const handleDelete = (id: string): void => {
    markDirty(pipelines.filter((p) => p.id !== id))
    setContextMenu(null)
  }

  const handleRename = (id: string, name: string): void => {
    setRenaming({ id, name })
    setRenameName(name)
    setContextMenu(null)
  }

  // Mirrors WPF AutomationPage ChangePipelineIconAsync: opens the symbol
  // picker and stores the icon name on the pipeline (null = default).
  const handleChangeIcon = async (id: string): Promise<void> => {
    setContextMenu(null)
    try {
      const icon = await openSymbolPicker()
      markDirty(pipelines.map((p) => (p.id === id ? { ...p, iconName: icon ?? undefined } : p)))
    } catch {
      // icon picker unavailable
    }
  }

  const commitRename = (): void => {
    if (renaming == null) return
    const trimmed = renameName.trim()
    if (trimmed !== '') {
      markDirty(pipelines.map((p) => (p.id === renaming.id ? { ...p, name: trimmed } : p)))
    }
    setRenaming(null)
  }

  const handleMovePipeline = (id: string, offset: number): void => {
    const pipeline = pipelines.find((p) => p.id === id)
    if (pipeline == null) {
      setContextMenu(null)
      return
    }
    const isManual = pipeline.trigger == null
    const section = pipelines.filter((p) => (p.trigger == null) === isManual)
    const sectionIds = section.map((p) => p.id)
    const from = sectionIds.indexOf(id)
    const to = from + offset
    if (from < 0 || to < 0 || to >= sectionIds.length) {
      setContextMenu(null)
      return
    }
    const targetId = sectionIds[to]
    const next = [...pipelines]
    const fromIndex = next.findIndex((p) => p.id === id)
    const toIndex = next.findIndex((p) => p.id === targetId)
    const [moved] = next.splice(fromIndex, 1)
    next.splice(toIndex, 0, moved)
    markDirty(next)
    setContextMenu(null)
  }

  const handleAddStep = (): void => {
    if (!addStepFor || !selectedStepType) return
    markDirty(
      pipelines.map((p) =>
        p.id === addStepFor ? { ...p, steps: [...(p.steps ?? []), createDefaultStep(selectedStepType)] } : p
      )
    )
    setAddStepFor(null)
    setSelectedStepType('')
  }

  const handleRemoveStep = (pipelineId: string, index: number): void => {
    markDirty(
      pipelines.map((p) =>
        p.id === pipelineId
          ? { ...p, steps: (p.steps ?? []).filter((_, i) => i !== index) }
          : p
      )
    )
  }

  const handleMoveStep = (pipelineId: string, from: number, to: number): void => {
    markDirty(
      pipelines.map((p) => {
        if (p.id !== pipelineId) return p
        const steps = [...(p.steps ?? [])]
        if (from < 0 || from >= steps.length || to < 0 || to >= steps.length) return p
        const [moved] = steps.splice(from, 1)
        steps.splice(to, 0, moved)
        return { ...p, steps }
      })
    )
  }

  const handleApplyStep = (target: EditingStepTarget, next: AutomationStepType): void => {
    markDirty(
      pipelines.map((p) =>
        p.id === target.pipelineId
          ? { ...p, steps: (p.steps ?? []).map((s, i) => (i === target.index ? next : s)) }
          : p
      )
    )
    setEditingStep(null)
  }

  const openContextMenu = (pipeline: AutomationPipeline) => (
    event: React.MouseEvent
  ): void => {
    event.preventDefault()
    if (pipeline.id == null) return
    const position = clampToViewport(event.clientX, event.clientY)
    setContextMenu({ id: pipeline.id, x: position.x, y: position.y })
  }

  const renderPipeline = (
    pipeline: AutomationPipeline,
    index: number,
    section: AutomationPipeline[]
  ): React.JSX.Element => {
    const sectionIds = section.map((p) => p.id)
    const pipelineId = pipeline.id!
    const isExpanded = expandedIds[pipelineId] === true
    return (
      <div key={pipelineId} className="udt-pipeline-wrap" onContextMenu={openContextMenu(pipeline)}>
        <CardExpander
          className="udt-pipeline"
          expanded={isExpanded}
          onExpandedChange={(next) => toggleExpanded(pipelineId, next)}
          icon={
            pipeline.trigger
              ? triggerIcon(String(pipeline.trigger['$type']))
              : (symbolIcon(String(pipeline.iconName ?? '')) ?? QUICK_ACTION_ICON)
          }
          header={
            <>
              <span className="udt-card-expander__title">{pipelineTitle(pipeline)}</span>
              <span className="udt-card-expander__desc">{pipelineSubtitle(pipeline)}</span>
            </>
          }
        >
          <div className="udt-pipeline__steps">
            {(pipeline.steps ?? []).map((step, stepIndex) => {
              const summary = stepSummaryText(step, t) || formatStepSummary(step, t, pipelines)
              const icon = stepIcon(String(step.$type))
              return (
                <div key={stepIndex} className="udt-step-row">
                  <button
                    type="button"
                    className="udt-step-row__name-button"
                    onClick={() => setEditingStep({ pipelineId, index: stepIndex })}
                  >
                    {icon != null && <span className="udt-step-row__icon" aria-hidden="true">{icon}</span>}
                    <span>
                      {t(`automation.stepEditors.${step.$type}.title`, {
                        defaultValue: shortTypeName(step.$type)
                      })}
                    </span>
                    {summary !== '' && <span className="udt-step-row__summary">{summary}</span>}
                  </button>
                  <div className="udt-step-row__actions">
                    <Tooltip title={t('automation.moveUp')}>
                      <button
                        type="button"
                        className="udt-icon-btn"
                        disabled={stepIndex === 0}
                        aria-label={t('automation.moveUp')}
                        onClick={() => handleMoveStep(pipelineId, stepIndex, stepIndex - 1)}
                      >
                        <ArrowUpOutlined />
                      </button>
                    </Tooltip>
                    <Tooltip title={t('automation.moveDown')}>
                      <button
                        type="button"
                        className="udt-icon-btn"
                        disabled={stepIndex === (pipeline.steps?.length ?? 0) - 1}
                        aria-label={t('automation.moveDown')}
                        onClick={() => handleMoveStep(pipelineId, stepIndex, stepIndex + 1)}
                      >
                        <ArrowDownOutlined />
                      </button>
                    </Tooltip>
                    <Tooltip title={t('automation.deleteStep')}>
                      <button
                        type="button"
                        className="udt-icon-btn udt-icon-btn--danger"
                        aria-label={t('automation.deleteStep')}
                        onClick={() => handleRemoveStep(pipelineId, stepIndex)}
                      >
                        <DeleteOutlined />
                      </button>
                    </Tooltip>
                  </div>
                </div>
              )
            })}
          </div>
          <div className="udt-pipeline__actions">
            {pipeline.trigger != null ? (
              <label
                className="udt-checkbox udt-pipeline__exclusive"
                title={t('wpf.automationPipelineControlexclusivetoolTip', {
                  defaultValue: t('automation.enableDesc')
                })}
              >
                <input
                  type="checkbox"
                  checked={pipeline.isExclusive ?? true}
                  onChange={(e) => setExclusive(pipelineId, e.target.checked)}
                />
                <span className="udt-checkbox__box" />
                <span>
                  {t('wpf.automationPipelineControlexclusive', {
                    defaultValue: 'Exclusive'
                  })}
                </span>
              </label>
            ) : (
              <span className="udt-pipeline__exclusive-spacer" />
            )}
            {pipeline.trigger != null &&
              isTriggerConfigurable(pipeline.trigger as AutomationTrigger) && (
                <button
                  type="button"
                  className="udt-btn udt-btn--secondary udt-btn--sm"
                  onClick={() => setConfigFor(pipelineId)}
                >
                  <SettingOutlined /> {t('automation.configure')}
                </button>
              )}
            <button
              type="button"
              className="udt-btn udt-btn--secondary udt-btn--sm"
              onClick={() => void runNow(pipelineId)}
            >
              <PlayCircleOutlined /> {t('automation.runNow')}
            </button>
            <button
              type="button"
              className="udt-btn udt-btn--secondary udt-btn--sm"
              onClick={() => {
                setAddStepFor(pipelineId)
                setSelectedStepType('')
              }}
            >
              <PlusOutlined /> {t('automation.addStep')}
            </button>
            <button
              type="button"
              className="udt-btn udt-btn--danger udt-btn--sm"
              onClick={() => handleDelete(pipelineId)}
            >
              <DeleteOutlined /> {t('automation.delete')}
            </button>
          </div>
        </CardExpander>
        {contextMenu?.id === pipelineId && contextMenu != null && (
          <div
            className="udt-context-menu"
            style={{ left: contextMenu.x, top: contextMenu.y }}
            role="menu"
          >
            <button
              type="button"
              role="menuitem"
              className="udt-context-menu__item"
              disabled={index === 0}
              onClick={() => handleMovePipeline(pipelineId, -1)}
            >
              <ArrowUpOutlined /> {t('automation.moveUp')}
            </button>
            <button
              type="button"
              role="menuitem"
              className="udt-context-menu__item"
              disabled={index === sectionIds.length - 1}
              onClick={() => handleMovePipeline(pipelineId, 1)}
            >
              <ArrowDownOutlined /> {t('automation.moveDown')}
            </button>
            <div className="udt-context-menu__divider" />
            <button
              type="button"
              role="menuitem"
              className="udt-context-menu__item"
              onClick={() => handleRename(pipelineId, localizeStoredName(pipeline.name ?? ''))}
            >
              <EditOutlined /> {t('automation.renamePipeline')}
            </button>
            <button
              type="button"
              role="menuitem"
              className="udt-context-menu__item"
              onClick={() => void handleChangeIcon(pipelineId)}
            >
              <EditOutlined /> {t('automation.changeIcon')}
            </button>
            <div className="udt-context-menu__divider" />
            <button
              type="button"
              role="menuitem"
              className="udt-context-menu__item udt-context-menu__item--danger"
              onClick={() => handleDelete(pipelineId)}
            >
              <DeleteOutlined /> {t('automation.delete')}
            </button>
          </div>
        )}
      </div>
    )
  }

  const showSkeleton = automationLoading && pipelines.length === 0

  const newLabel = t('wpf.automationPageaddManualPipelinetitle', {
    defaultValue: t('automation.addPipeline')
  })

  return (
    <div className="udt-page udt-automation-page">
      {showSkeleton ? (
        <SkeletonList rows={3} className="udt-automation-skeleton" />
      ) : (
        <>
          <div className="udt-card udt-card--row udt-automation-enable">
            <div className="udt-card__copy">
              <div className="udt-card__title">{t('automation.enable')}</div>
              <div className="udt-card__desc">{t('automation.enableDesc')}</div>
            </div>
            <label className="udt-switch">
              <input
                type="checkbox"
                checked={state?.isEnabled ?? false}
                onChange={(e) => void setEnabled(e.target.checked)}
              />
              <span className="udt-switch__track" />
            </label>
          </div>

          {automatic.length === 0 ? (
            <div className="udt-empty">
              <div className="udt-empty__title">{t('automation.actionsEmpty')}</div>
            </div>
          ) : (
            <div className="udt-pipeline-list">
              {automatic.map((pipeline, index) => renderPipeline(pipeline, index, automatic))}
            </div>
          )}

          <div className="udt-automation-new-row">
            <button type="button" className="udt-btn udt-automation-new" onClick={() => setPickerOpen(true)}>
              {newLabel}
            </button>
          </div>

          <section className="udt-automation-section">
            <h2 className="udt-automation-section__title">
              {t('wpf.automationPagequickActionstitle', {
                defaultValue: t('automation.quickActionsTitle')
              })}
            </h2>
            <p className="udt-automation-section__hint">
              {t('wpf.automationPagequickActionsmessage', {
                defaultValue: t('automation.quickActionsHint')
              })}
            </p>

            {manual.length === 0 ? (
              <div className="udt-empty">
                <div className="udt-empty__title">{t('automation.quickActionsEmpty')}</div>
              </div>
            ) : (
              <div className="udt-pipeline-list">
                {manual.map((pipeline, index) => renderPipeline(pipeline, index, manual))}
              </div>
            )}

            <div className="udt-automation-new-row">
              <button
                type="button"
                className="udt-btn udt-automation-new"
                onClick={() => {
                  setPendingTrigger(null)
                  setCreateOpen(true)
                }}
              >
                {newLabel}
              </button>
            </div>
          </section>
        </>
      )}

      {dirty && (
        <div className="udt-automation-savebar">
          <button type="button" className="udt-btn udt-btn--secondary" onClick={handleRevert}>
            {t('automation.revert')}
          </button>
          <button type="button" className="udt-btn udt-btn--primary" onClick={() => void handleSave()}>
            {t('automation.save')}
          </button>
        </div>
      )}

      {contextMenu != null && (
        <div
          className="udt-context-menu-backdrop"
          onClick={() => setContextMenu(null)}
          onContextMenu={(event) => {
            event.preventDefault()
            setContextMenu(null)
          }}
        />
      )}

      {renaming != null && (
        <div className="udt-modal-backdrop" onClick={() => setRenaming(null)}>
          <div className="udt-modal" onClick={(e) => e.stopPropagation()}>
            <div className="udt-modal__title">{t('automation.renamePipelineTitle')}</div>
            <input
              autoFocus
              className="udt-input"
              value={renameName}
              placeholder={t('automation.renamePipelinePlaceholder')}
              onChange={(e) => setRenameName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') commitRename()
                if (e.key === 'Escape') setRenaming(null)
              }}
            />
            <div className="udt-modal__actions">
              <button type="button" className="udt-btn udt-btn--secondary" onClick={() => setRenaming(null)}>
                {t('common.cancel', { defaultValue: '取消' })}
              </button>
              <button type="button" className="udt-btn udt-btn--primary" onClick={commitRename}>
                <ArrowRightOutlined /> {t('common.confirm', { defaultValue: '确定' })}
              </button>
            </div>
          </div>
        </div>
      )}

      {createOpen && (
        <div className="udt-modal-backdrop" onClick={() => setCreateOpen(false)}>
          <div className="udt-modal" onClick={(e) => e.stopPropagation()}>
            <div className="udt-modal__title">
              {pendingTrigger != null ? t('automation.pipelineName') : t('automation.quickActionName')}
            </div>
            <input
              autoFocus
              className="udt-input"
              value={createName}
              placeholder={t('automation.pipelineNamePlaceholder')}
              onChange={(e) => setCreateName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') handleCreate()
                if (e.key === 'Escape') setCreateOpen(false)
              }}
            />
            <div className="udt-modal__actions">
              <button type="button" className="udt-btn udt-btn--secondary" onClick={() => setCreateOpen(false)}>
                {t('common.cancel', { defaultValue: '取消' })}
              </button>
              <button
                type="button"
                className="udt-btn udt-btn--primary"
                disabled={!createName.trim()}
                onClick={handleCreate}
              >
                <ArrowRightOutlined /> {t('common.confirm', { defaultValue: '确定' })}
              </button>
            </div>
          </div>
        </div>
      )}

      {editingStep &&
        (() => {
          const step = (pipelines.find((p) => p.id === editingStep.pipelineId)?.steps ?? [])[editingStep.index]
          if (!step) return null
          return (
            <StepEditorModal
              step={step}
              pipelines={pipelines}
              onApply={(next) => handleApplyStep(editingStep, next)}
              onCancel={() => setEditingStep(null)}
            />
          )
        })()}

      {addStepFor !== null && (
        <div className="udt-modal-backdrop" onClick={() => setAddStepFor(null)}>
          <div className="udt-modal" onClick={(e) => e.stopPropagation()}>
            <div className="udt-modal__title">{t('automation.addStep')}</div>
            <div className="udt-modal__list">
              {(steps ?? []).map((s) => (
                <button
                  key={s}
                  type="button"
                  className={`udt-step-option${selectedStepType === s ? ' udt-step-option--active' : ''}`}
                  onClick={() => setSelectedStepType(s)}
                >
                  {stepIcon(s)}
                  {t(`automation.stepEditors.${s}.title`, { defaultValue: shortTypeName(s) })}
                </button>
              ))}
            </div>
            <div className="udt-modal__actions">
              <button type="button" className="udt-btn udt-btn--secondary" onClick={() => setAddStepFor(null)}>
                {t('common.cancel', { defaultValue: '取消' })}
              </button>
              <button
                type="button"
                className="udt-btn udt-btn--primary"
                disabled={!selectedStepType}
                onClick={handleAddStep}
              >
                <ArrowRightOutlined /> {t('common.confirm', { defaultValue: '确定' })}
              </button>
            </div>
          </div>
        </div>
      )}

      {pickerOpen && (
        <TriggerPickerModal
          existingKinds={automatic
            .map((p) => p.trigger)
            .filter((trigger): trigger is AutomationTrigger => trigger != null)
            .map((trigger) => normalizeTriggerKind(String(trigger.$type)) ?? '')
            .filter((kind) => kind !== '')}
          onPick={openCreateForTrigger}
          onCancel={() => setPickerOpen(false)}
        />
      )}

      {configFor !== null &&
        (() => {
          const pipeline = pipelines.find((p) => p.id === configFor)
          if (pipeline?.trigger == null) return null
          return (
            <TriggerConfigModal
              trigger={pipeline.trigger as AutomationTrigger}
              onSave={(next) => {
                markDirty(pipelines.map((p) => (p.id === configFor ? { ...p, trigger: next } : p)))
                setConfigFor(null)
              }}
              onCancel={() => setConfigFor(null)}
            />
          )
        })()}
    </div>
  )
}
