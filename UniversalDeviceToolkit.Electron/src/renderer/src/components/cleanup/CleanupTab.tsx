import { useMemo, useState } from 'react'
import {
  AppstoreOutlined,
  ClearOutlined,
  DatabaseOutlined,
  DeleteOutlined,
  FileSearchOutlined,
  FolderOpenOutlined,
  HddOutlined,
  HighlightOutlined,
  PlayCircleOutlined,
  RocketOutlined,
  ThunderboltOutlined
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import { useOptimizationStore } from '../../stores/optimizationStore'
import type { OptimizationCategoryDefinition } from '../../api/optimization'
import { presentCategoryActions } from '../../utils/optimizationToggle'
import CleanupRulesPanel from '../optimization/CleanupRulesPanel'
import CardExpander from '../CardExpander'
import './cleanup.css'

type CleanupPhase = 'idle' | 'scanning' | 'scanned' | 'running' | 'done'

function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'
  const gb = bytes / 1024 ** 3
  if (gb >= 1) return `${gb.toFixed(2)} GB`
  const mb = bytes / 1024 ** 2
  if (mb >= 1) return `${mb.toFixed(1)} MB`
  return `${bytes.toFixed(0)} B`
}

function cleanupIcon(key: string): React.JSX.Element {
  if (key.includes('cache')) return <DatabaseOutlined />
  if (key.includes('performance')) return <ThunderboltOutlined />
  if (key.includes('systemComponents')) return <AppstoreOutlined />
  if (key.includes('largeFiles')) return <HddOutlined />
  if (key.includes('systemFiles')) return <FolderOpenOutlined />
  if (key.includes('custom')) return <RocketOutlined />
  return <ClearOutlined />
}

interface CleanupTabProps {
  selectedKeys: string[]
  onSelectedKeysChange: (keys: string[]) => void
}

export default function CleanupTab({ selectedKeys, onSelectedKeysChange }: CleanupTabProps): React.JSX.Element {
  const { t } = useTranslation()
  const categories = useOptimizationStore((s) => s.categories)
  const estimate = useOptimizationStore((s) => s.estimate)
  const runCleanup = useOptimizationStore((s) => s.runCleanup)
  const [phase, setPhase] = useState<CleanupPhase>('idle')
  const [sizeByKey, setSizeByKey] = useState<Record<string, number>>({})
  const [scanProgress, setScanProgress] = useState(0)
  const [currentScan, setCurrentScan] = useState<string | null>(null)
  const [totalSize, setTotalSize] = useState<number | null>(null)
  const [runningPercent, setRunningPercent] = useState(0)

  const cleanupCategories = useMemo(
    () => categories.filter((c) => c.key.startsWith('cleanup.') && c.key !== 'cleanup.custom'),
    [categories]
  )
  const allVisibleActions = useMemo(() => {
    const out = new Map<string, { actionKey: string; categoryTitle: string }>()
    for (const category of cleanupCategories) {
      const presentation = presentCategoryActions(category.actions, false)
      for (const { action } of presentation.visible) {
        if (!action.applied) out.set(action.key, { actionKey: action.key, categoryTitle: category.title })
      }
    }
    return out
  }, [cleanupCategories])

  const selectedDetail = selectedKeys
    .map((key) => allVisibleActions.get(key))
    .filter((d): d is { actionKey: string; categoryTitle: string } => d != null)

  const toggleSelection = (key: string): void => {
    onSelectedKeysChange(
      selectedKeys.includes(key) ? selectedKeys.filter((k) => k !== key) : [...selectedKeys, key]
    )
  }

  const toggleCategory = (category: OptimizationCategoryDefinition): void => {
    const presentation = presentCategoryActions(category.actions, false)
    const keys = presentation.visible.filter(({ action }) => !action.applied).map(({ action }) => action.key)
    const allSelected = keys.every((k) => selectedKeys.includes(k))
    if (allSelected) {
      onSelectedKeysChange(selectedKeys.filter((k) => !keys.includes(k)))
    } else {
      onSelectedKeysChange([...new Set([...selectedKeys, ...keys])])
    }
  }

  const handleScan = async (): Promise<void> => {
    if (selectedKeys.length === 0) return
    setPhase('scanning')
    setSizeByKey({})
    setTotalSize(null)
    setScanProgress(0)
    const targets = cleanupCategories.filter((c) => {
      const presentation = presentCategoryActions(c.actions, false)
      return presentation.visible.some(({ action }) => selectedKeys.includes(action.key) || action.applied)
    })
    let done = 0
    let total = 0
    for (const category of targets) {
      const presentation = presentCategoryActions(category.actions, false)
      const keys = presentation.visible.map(({ action }) => action.key)
      setCurrentScan(category.title)
      try {
        const bytes = await estimate(keys)
        setSizeByKey((prev) => ({ ...prev, [category.key]: bytes }))
        total += bytes
      } catch {
        // keep going; a single category failure must not abort the scan
      }
      done += 1
      setScanProgress(done / Math.max(1, targets.length))
      await new Promise((r) => setTimeout(r, 120))
    }
    setTotalSize(total)
    setCurrentScan(null)
    setPhase('scanned')
  }

  const handleClean = async (): Promise<void> => {
    setPhase('running')
    setRunningPercent(0)
    const timer = window.setInterval(() => {
      setRunningPercent((p) => Math.min(90, p + 5 + Math.random() * 12))
    }, 180)
    try {
      await runCleanup(selectedKeys)
    } finally {
      window.clearInterval(timer)
      setRunningPercent(100)
      setPhase('done')
      setSizeByKey({})
      setTotalSize(null)
      window.setTimeout(() => setPhase('idle'), 1600)
    }
  }

  const scanning = phase === 'scanning'
  const running = phase === 'running'

  return (
    <div className="udt-cleanup">
      <div className="udt-cleanup__main">
        {cleanupCategories.map((category) => {
          const presentation = presentCategoryActions(category.actions, scanning || running)
          const keys = presentation.visible.filter(({ action }) => !action.applied).map(({ action }) => action.key)
          const selectedCount = keys.filter((k) => selectedKeys.includes(k)).length
          const allSelected = keys.length > 0 && selectedCount === keys.length
          const size = sizeByKey[category.key]
          return (
            <div key={category.key} className={`udt-cleanup-category${allSelected ? ' udt-cleanup-category--selected' : ''}`}>
              <div className="udt-cleanup-category__header">
                <span className="udt-cleanup-category__icon">{cleanupIcon(category.key)}</span>
                <div className="udt-cleanup-category__copy">
                  <div className="udt-cleanup-category__title">{category.title}</div>
                  <div className="udt-cleanup-category__desc">{category.description}</div>
                </div>
                <div className="udt-cleanup-category__meta">
                  {size !== undefined && phase !== 'idle' && (
                    <span className="udt-cleanup-category__size">{formatBytes(size)}</span>
                  )}
                  <span className="udt-cleanup-category__count">
                    {selectedCount}/{keys.length}
                  </span>
                  <label className="udt-cleanup-check">
                    <input
                      type="checkbox"
                      checked={allSelected}
                      disabled={scanning || running || keys.length === 0}
                      onChange={() => toggleCategory(category)}
                    />
                    <span className="udt-cleanup-check__box" />
                  </label>
                </div>
              </div>
              <CardExpander
                header={t('optimization.cleanup.items', { defaultValue: 'Items' })}
                description={''}
                defaultExpanded={false}
              >
                {presentation.visible.map(({ action, editable }) => (
                  <label
                    key={action.key}
                    className={`udt-cleanup-item${selectedKeys.includes(action.key) ? ' udt-cleanup-item--selected' : ''}`}
                  >
                    <input
                      type="checkbox"
                      checked={selectedKeys.includes(action.key)}
                      disabled={!editable || scanning || running}
                      onChange={() => toggleSelection(action.key)}
                    />
                    <span className="udt-cleanup-item__title">{action.title}</span>
                    <span className="udt-cleanup-item__desc">{action.description}</span>
                  </label>
                ))}
              </CardExpander>
            </div>
          )
        })}
      </div>

      <div className="udt-cleanup__divider" aria-hidden="true" />

      <div className="udt-cleanup__side">
        <div className="udt-card udt-cleanup-summary">
          <div className="udt-card__title">
            {t('wpf.windowsOptimizationPagecleanupInfo', { defaultValue: t('optimization.estimate') })}
          </div>
          {phase === 'scanning' && (
            <div className="udt-cleanup-scanline">
              <FileSearchOutlined className="udt-cleanup-scanline__icon" />
              <span className="udt-cleanup-scanline__text">
                {t('cleanup.scanning', { defaultValue: 'Scanning' })}: {currentScan ?? '…'}
              </span>
            </div>
          )}
          {phase === 'running' && (
            <div className="udt-cleanup-scanline">
              <ClearOutlined className="udt-cleanup-scanline__icon udt-cleanup-scanline__icon--spin" />
              <span className="udt-cleanup-scanline__text">
                {t('cleanup.running', { defaultValue: 'Cleaning…' })}
              </span>
            </div>
          )}
          {(scanning || running) && (
            <div className="udt-cleanup-progress">
              <div
                className="udt-cleanup-progress__fill"
                style={{ width: `${running ? runningPercent : Math.round(scanProgress * 100)}%` }}
              />
            </div>
          )}
          {phase === 'scanned' && totalSize !== null && (
            <div className="udt-cleanup-total">
              <span className="udt-cleanup-total__label">
                {t('optimization.estimateResult', { defaultValue: 'Freed space' })}
              </span>
              <span className="udt-cleanup-total__value">{formatBytes(totalSize)}</span>
            </div>
          )}
          {phase === 'done' && (
            <div className="udt-cleanup-total">
              <span className="udt-cleanup-total__label">
                {t('cleanup.done', { defaultValue: 'Cleanup complete' })}
              </span>
              <HighlightOutlined className="udt-cleanup-total__check" />
            </div>
          )}
          {phase === 'idle' && (
            <div className="udt-card__desc">
              {t('wpf.windowsOptimizationPagecleanupDescription', {
                defaultValue: t('optimization.cleanupHint')
              })}
            </div>
          )}
          <div className="udt-cleanup-summary__actions">
            <button
              type="button"
              className="udt-btn udt-btn--primary"
              disabled={selectedKeys.length === 0 || scanning || running}
              onClick={() => void handleScan()}
            >
              <FileSearchOutlined /> {t('wpf.windowsOptimizationPagescanbutton', { defaultValue: 'Scan' })}
            </button>
            <button
              type="button"
              className="udt-btn udt-btn--danger"
              disabled={phase !== 'scanned' || running}
              onClick={() => void handleClean()}
            >
              <DeleteOutlined /> {t('wpf.windowsOptimizationPagerunCleanupbutton', { defaultValue: 'Clean now' })}
            </button>
          </div>
        </div>

        {selectedDetail.length > 0 && (
          <div className="udt-card udt-cleanup-selection">
            <div className="udt-card__title">
              {t('wpf.windowsOptimizationPageselectedActionsheader', { defaultValue: 'Selected items' })}
            </div>
            <div className="udt-cleanup-selection__list">
              {selectedDetail.slice(0, 12).map((detail) => (
                <div key={detail.actionKey} className="udt-cleanup-selection__item">
                  <PlayCircleOutlined className="udt-cleanup-selection__icon" />
                  <div className="udt-cleanup-selection__copy">
                    <span className="udt-cleanup-selection__title">{detail.categoryTitle}</span>
                    <span className="udt-cleanup-selection__key">{detail.actionKey}</span>
                  </div>
                </div>
              ))}
              {selectedDetail.length > 12 && (
                <div className="udt-cleanup-selection__more">
                  +{selectedDetail.length - 12} {t('cleanup.moreItems', { defaultValue: 'more' })}
                </div>
              )}
            </div>
          </div>
        )}

        <CleanupRulesPanel />
      </div>
    </div>
  )
}
