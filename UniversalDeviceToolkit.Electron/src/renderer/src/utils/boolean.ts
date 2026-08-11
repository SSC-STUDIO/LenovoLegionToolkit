/**
 * Boolean helpers — port of WPF Utils/BooleanAndConverter.cs and
 * Utils/InverseBooleanToVisibilityConverter.cs.
 */
export function and(...values: unknown[]): boolean {
  return values.every((value) => value === true)
}

export function or(...values: unknown[]): boolean {
  return values.some((value) => value === true)
}

export function inverse(value: unknown): boolean {
  return value !== true
}

/** Inverse → visibility: true hides, false shows (WPF Collapsed semantics). */
export function inverseToHidden(value: unknown): boolean {
  return value === true
}
