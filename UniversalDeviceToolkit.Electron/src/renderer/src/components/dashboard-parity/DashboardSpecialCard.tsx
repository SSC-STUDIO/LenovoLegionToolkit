import { useState } from 'react'
import { Button, Dropdown, Switch, Tooltip, message } from 'antd'
import {
  ChevronDown16Regular,
  Desktop24Regular,
  DeveloperBoard24Regular,
  DeveloperBoardLightning20Regular,
  QuestionCircle24Regular,
  Settings24Regular,
  Sleep24Regular
} from '@fluentui/react-icons'
import { useTranslation } from 'react-i18next'
import {
  dashboardHardwareApi,
  type DashboardHardwareState,
  type DiscreteGpuState
} from '../../api/dashboardHardware'
import type { DashboardItem } from '../../api/dashboard'
import { isSpecialItemSupported } from './dashboardHardwareSupport'
import OverclockProfilesModal from './OverclockProfilesModal'

export type SpecialDashboardItem = Extract<
  DashboardItem,
  'DiscreteGpu' | 'OverclockDiscreteGpu' | 'TurnOffMonitors'
>

interface DashboardSpecialCardProps {
  item: SpecialDashboardItem
  hardware: DashboardHardwareState
  error: string | null
  onChanged: () => Promise<void>
}

function statusTone(state: DiscreteGpuState): string {
  if (state === 'Active' || state === 'MonitorConnected') return 'success'
  if (state === 'Inactive') return 'warning'
  return 'neutral'
}

function CardShell({
  icon,
  title,
  description,
  children,
  error
}: {
  icon: React.ReactNode
  title: string
  description: string
  children: React.ReactNode
  error: string | null
}): React.JSX.Element {
  return (
    <article className="udt-parity-feature-card">
      <div className="udt-parity-feature-card__body">
        <span className="udt-parity-feature-card__icon" aria-hidden="true">{icon}</span>
        <div className="udt-parity-feature-card__copy">
          <div className="udt-parity-feature-card__title" title={title}>{title}</div>
          <div className="udt-parity-feature-card__description" title={description}>{description}</div>
          {error != null && <div className="udt-parity-feature-card__warning" title={error}>{error}</div>}
        </div>
        {children}
      </div>
    </article>
  )
}

function DiscreteGpuCard({
  hardware,
  error,
  onChanged
}: Omit<DashboardSpecialCardProps, 'item'>): React.JSX.Element {
  const { t } = useTranslation()
  const [busy, setBusy] = useState(false)
  const gpu = hardware.discreteGpu
  const title = t('dashboardHardware.discreteGpu.title')
  const description = t('dashboardHardware.discreteGpu.description')
  const status = t(`dashboardHardware.discreteGpu.status.${gpu.state}`, { defaultValue: gpu.state })
  const canRestart = gpu.state === 'Active' || gpu.state === 'Inactive'
  const canKill = gpu.state === 'Active'

  async function run(action: () => Promise<unknown>): Promise<void> {
    setBusy(true)
    try {
      await action()
      await onChanged()
    } catch (reason) {
      void message.error((reason as Error).message)
    } finally {
      setBusy(false)
    }
  }

  const details = (
    <div className="udt-parity-gpu-tooltip">
      <strong>{t('dashboardHardware.discreteGpu.performance')}</strong>
      <div>{gpu.performanceState || '-'}</div>
      <strong>{t('dashboardHardware.discreteGpu.processes')}</strong>
      {gpu.processes.length > 0
        ? gpu.processes.map((process) => <div key={process}>{process}</div>)
        : <div>{t('dashboardHardware.discreteGpu.noProcesses')}</div>}
    </div>
  )

  return (
    <CardShell
      icon={<DeveloperBoard24Regular />}
      title={title}
      description={description}
      error={error}
    >
      <div className="udt-parity-gpu-accessory">
        <div className="udt-parity-gpu-accessory__status-row">
          <span className={`udt-parity-status-dot udt-parity-status-dot--${statusTone(gpu.state)}`} />
          <span className="udt-parity-gpu-accessory__status">{status}</span>
          <Tooltip title={details} placement="topRight">
            <Button
              aria-label={t('dashboardHardware.discreteGpu.information')}
              className="udt-parity-icon-button"
              icon={<QuestionCircle24Regular />}
            />
          </Tooltip>
        </div>
        <Dropdown
          disabled={!canRestart || busy}
          menu={{
            items: [
              {
                key: 'kill',
                label: t('dashboardHardware.discreteGpu.killProcesses'),
                disabled: !canKill
              },
              {
                key: 'restart',
                label: t('dashboardHardware.discreteGpu.restart'),
                disabled: !canRestart
              }
            ],
            onClick: ({ key }) => {
              if (key === 'kill') void run(() => dashboardHardwareApi.killGpuProcesses())
              if (key === 'restart') void run(() => dashboardHardwareApi.restartGpu())
            }
          }}
          placement="bottomRight"
          trigger={['click']}
        >
          <Button
            type="primary"
            disabled={!canRestart}
            loading={busy}
            icon={<Sleep24Regular />}
          >
            {t('dashboardHardware.discreteGpu.deactivate')}
            <ChevronDown16Regular />
          </Button>
        </Dropdown>
      </div>
    </CardShell>
  )
}

function OverclockGpuCard({
  hardware,
  error,
  onChanged
}: Omit<DashboardSpecialCardProps, 'item'>): React.JSX.Element {
  const { t } = useTranslation()
  const overclock = hardware.overclockDiscreteGpu
  const [busy, setBusy] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)

  async function setEnabled(enabled: boolean): Promise<void> {
    setBusy(true)
    try {
      await dashboardHardwareApi.setOverclockEnabled(enabled)
      await onChanged()
    } catch (reason) {
      void message.error((reason as Error).message)
    } finally {
      setBusy(false)
    }
  }

  const title = t('dashboardHardware.overclock.title')
  return (
    <>
      <CardShell
        icon={<DeveloperBoardLightning20Regular />}
        title={title}
        description={t('dashboardHardware.overclock.description')}
        error={error}
      >
        <div className="udt-parity-feature-card__accessory udt-parity-overclock-accessory">
          <Switch
            aria-label={title}
            checked={overclock.enabled}
            disabled={busy}
            loading={busy}
            onChange={(enabled) => void setEnabled(enabled)}
          />
          <Tooltip title={t('dashboardHardware.overclock.settings')}>
            <Button
              aria-label={t('dashboardHardware.overclock.settings')}
              className="udt-parity-icon-button"
              icon={<Settings24Regular />}
              onClick={() => setModalOpen(true)}
            />
          </Tooltip>
        </div>
      </CardShell>
      <OverclockProfilesModal
        open={modalOpen}
        hardware={overclock}
        onClose={() => setModalOpen(false)}
        onApplied={() => void onChanged()}
      />
    </>
  )
}

function TurnOffMonitorsCard({ error }: Omit<DashboardSpecialCardProps, 'item'>): React.JSX.Element {
  const { t } = useTranslation()
  const [busy, setBusy] = useState(false)
  const title = t('dashboardHardware.turnOffMonitors.title')

  async function turnOff(): Promise<void> {
    setBusy(true)
    try {
      await dashboardHardwareApi.turnOffMonitors()
    } catch (reason) {
      void message.error((reason as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <CardShell
      icon={<Desktop24Regular />}
      title={title}
      description={t('dashboardHardware.turnOffMonitors.description')}
      error={error}
    >
      <div className="udt-parity-feature-card__accessory">
        <Button loading={busy} onClick={() => void turnOff()}>
          {t('dashboardHardware.turnOffMonitors.action')}
        </Button>
      </div>
    </CardShell>
  )
}

export default function DashboardSpecialCard(props: DashboardSpecialCardProps): React.JSX.Element | null {
  if (!isSpecialItemSupported(props.item, props.hardware)) return null
  if (props.item === 'DiscreteGpu') return <DiscreteGpuCard {...props} />
  if (props.item === 'OverclockDiscreteGpu') return <OverclockGpuCard {...props} />
  if (props.item === 'TurnOffMonitors') return <TurnOffMonitorsCard {...props} />
  return null
}
