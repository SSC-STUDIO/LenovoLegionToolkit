/**
 * Message box helpers — port of Electron Utils/MessageBoxHelper.cs
 * implemented with antd Modal (renderer-side equivalent of MessageBox).
 */
import { Modal, message } from 'antd'

export type MessageBoxKind = 'info' | 'warning' | 'error' | 'confirm'

export interface MessageBoxOptions {
  title: string
  message: string
  kind?: MessageBoxKind
  okText?: string
  cancelText?: string
  danger?: boolean
}

/** Promise-based dialog: resolves true when OK, false when cancelled. */
export function showMessageBox(options: MessageBoxOptions): Promise<boolean> {
  return new Promise((resolve) => {
    const kind = options.kind ?? 'info'
    const title = options.title
    const content = options.message
    const okText = options.okText ?? 'OK'
    const cancelText = options.cancelText ?? 'Cancel'

    if (kind === 'confirm') {
      Modal.confirm({
        title,
        content,
        okText,
        cancelText,
        okButtonProps: options.danger === true ? { danger: true } : undefined,
        onOk: () => resolve(true),
        onCancel: () => resolve(false),
      })
      return
    }

    Modal[kind === 'info' ? 'info' : kind === 'warning' ? 'warning' : 'error']({
      title,
      content,
      okText,
      onOk: () => resolve(true),
    })
  })
}

/** Non-blocking toast variant (Electron Snackbar semantics). */
export function showToast(kind: 'success' | 'info' | 'warning' | 'error', text: string): void {
  message[kind](text)
}
