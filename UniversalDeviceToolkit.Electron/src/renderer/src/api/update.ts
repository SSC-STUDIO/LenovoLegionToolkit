import { invoke } from './bridge'

export const updateApi = {
  check: (force = false) =>
    invoke<{ available: boolean; version?: string | null; error?: string | null }>('app.update.check', { force }),
  status: () => invoke<{ status: string; disable: boolean }>('app.update.status')
}
