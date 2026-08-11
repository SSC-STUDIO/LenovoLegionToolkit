export function formatSensorValue(value: number | null | undefined, digits = 0): string {
  if (value == null || !Number.isFinite(value)) return '--'
  return value.toFixed(digits)
}
