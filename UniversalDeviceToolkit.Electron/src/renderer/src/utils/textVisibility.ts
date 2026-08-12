/**
 * Text → visibility helpers — port of Electron Utils/TextToVisibilityConverter.cs.
 */
export function textIsVisible(value: unknown): boolean {
  return typeof value === 'string' && value.trim() !== ''
}

export function textToHidden(value: unknown): boolean {
  return !textIsVisible(value)
}

export function textOrFallback(value: unknown, fallback: string): string {
  return textIsVisible(value) ? String(value) : fallback
}
