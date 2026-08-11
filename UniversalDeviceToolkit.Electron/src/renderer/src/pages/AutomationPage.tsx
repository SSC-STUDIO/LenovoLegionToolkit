import { useEffect, useState } from 'react'
import {
  Button,
  Card,
  Collapse,
  Empty,
  Flex,
  Input,
  List,
  Modal,
  Popconfirm,
  Select,
  Space,
  Switch,
  Tag,
  Typography,
  message
} from 'antd'
import { useTranslation } from 'react-i18next'
import type { AutomationPipeline, AutomationStepType } from '../api/automation'
import { useAutomationStore } from '../stores/automationStore'

function shortTypeName(type: string): string {
  return type
    .replace(/AutomationStep$/, '')
    .replace(/AutomationPipelineTrigger$/, '')
}

function stepSummary(step: AutomationStepType): string {
  const keys = Object.keys(step).filter((k) => k !== '$type')
  if (keys.length === 0) return ''
  const first = step[keys[0]]
  return JSON.stringify(first)
}

export default function AutomationPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { state, steps, load, setEnabled, save, runNow } = useAutomationStore()
  const [pipelines, setPipelines] = useState<AutomationPipeline[]>([])
  const [dirty, setDirty] = useState(false)
  const [createOpen, setCreateOpen] = useState(false)
  const [createName, setCreateName] = useState('')
  const [addStepFor, setAddStepFor] = useState<string | null>(null)
  const [selectedStepType, setSelectedStepType] = useState<string>('')

  useEffect(() => {
    void load().then(() => setPipelines([]))
  }, [load])

  useEffect(() => {
    setPipelines(state?.pipelines ?? [])
  }, [state])

  const markDirty = (next: AutomationPipeline[]): void => {
    setPipelines(next)
    setDirty(true)
  }

  const handleSave = async (): Promise<void> => {
    try {
      await save(pipelines, state?.isEnabled)
      setDirty(false)
      message.success(t('settings.saved'))
    } catch (error) {
      message.error((error as Error).message)
    }
  }

  const handleRevert = (): void => {
    void load()
    setDirty(false)
  }

  const handleCreate = (): void => {
    if (!createName.trim()) return
    const pipeline: AutomationPipeline = {
      id: crypto.randomUUID(),
      name: createName.trim(),
      trigger: null,
      steps: [],
      isExclusive: false
    }
    markDirty([...pipelines, pipeline])
    setCreateOpen(false)
    setCreateName('')
  }

  const handleDelete = (id: string): void => {
    markDirty(pipelines.filter((p) => p.id !== id))
  }

  const handleAddStep = (): void => {
    if (!addStepFor || !selectedStepType) return
    const stepType = selectedStepType.endsWith('AutomationStep')
      ? selectedStepType
      : `${selectedStepType}AutomationStep`
    markDirty(
      pipelines.map((p) =>
        p.id === addStepFor ? { ...p, steps: [...(p.steps ?? []), { $type: stepType }] } : p
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

  return (
    <Flex vertical gap={16}>
      <Flex align="center" justify="space-between">
        <Typography.Title level={3} style={{ margin: 0 }}>
          {t('automation.title')}
        </Typography.Title>
        <Space>
          <span>{t('automation.enable')}</span>
          <Switch
            checked={state?.isEnabled ?? false}
            onChange={(checked) => void setEnabled(checked)}
          />
        </Space>
      </Flex>

      {pipelines.length === 0 ? (
        <Empty description={t('automation.empty')} />
      ) : (
        <List
          dataSource={pipelines}
          renderItem={(pipeline) => (
            <Card
              size="small"
              title={
                <Space>
                  {pipeline.name ?? t('automation.quickAction')}
                  <Tag>{shortTypeName(String(pipeline.trigger?.['$type'] ?? 'quickAction'))}</Tag>
                  <Tag>{(pipeline.steps ?? []).length}</Tag>
                </Space>
              }
              extra={
                <Space>
                  <Button
                    size="small"
                    disabled={pipeline.trigger !== null && pipeline.trigger !== undefined}
                    onClick={() => void runNow(pipeline.id!)}
                  >
                    {t('automation.runNow')}
                  </Button>
                  <Popconfirm
                    title={t('automation.delete')}
                    onConfirm={() => handleDelete(pipeline.id!)}
                  >
                    <Button size="small" danger>
                      {t('automation.delete')}
                    </Button>
                  </Popconfirm>
                </Space>
              }
            >
              <Collapse
                ghost
                size="small"
                items={[
                  {
                    key: pipeline.id!,
                    label: `${t('automation.steps')} (${(pipeline.steps ?? []).length})`,
                    children: (
                      <Space direction="vertical" style={{ width: '100%' }}>
                        {(pipeline.steps ?? []).map((step, index) => (
                          <Flex key={index} justify="space-between" align="center">
                            <Typography.Text code>
                              {shortTypeName(step.$type)}
                              {stepSummary(step) && ` · ${stepSummary(step)}`}
                            </Typography.Text>
                            <Button
                              size="small"
                              danger
                              onClick={() => handleRemoveStep(pipeline.id!, index)}
                            >
                              {t('automation.deleteStep')}
                            </Button>
                          </Flex>
                        ))}
                        <Button
                          size="small"
                          onClick={() => {
                            setAddStepFor(pipeline.id!)
                            setSelectedStepType('')
                          }}
                        >
                          {t('automation.addStep')}
                        </Button>
                      </Space>
                    )
                  }
                ]}
              />
            </Card>
          )}
        />
      )}

      <Flex gap={8}>
        <Button type="primary" onClick={() => setCreateOpen(true)}>
          {t('automation.addPipeline')}
        </Button>
        {dirty && (
          <>
            <Button onClick={handleRevert}>{t('automation.revert')}</Button>
            <Button type="primary" onClick={() => void handleSave()}>
              {t('automation.save')}
            </Button>
          </>
        )}
      </Flex>

      <Modal
        title={t('automation.pipelineName')}
        open={createOpen}
        onOk={handleCreate}
        onCancel={() => setCreateOpen(false)}
        okButtonProps={{ disabled: !createName.trim() }}
      >
        <Input
          value={createName}
          onChange={(e) => setCreateName(e.target.value)}
          placeholder={t('automation.pipelineNamePlaceholder')}
        />
      </Modal>

      <Modal
        title={t('automation.addStep')}
        open={addStepFor !== null}
        onOk={handleAddStep}
        onCancel={() => setAddStepFor(null)}
        okButtonProps={{ disabled: !selectedStepType }}
      >
        <Select
          style={{ width: '100%' }}
          value={selectedStepType || undefined}
          placeholder={t('automation.stepType')}
          options={(steps ?? []).map((s) => ({
            value: s,
            label: shortTypeName(s)
          }))}
          onChange={setSelectedStepType}
        />
      </Modal>
    </Flex>
  )
}
