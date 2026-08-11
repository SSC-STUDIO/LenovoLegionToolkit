/**
 * Font management — port of WPF Utils/AppFontManager.cs.
 * The CSS variable --udt-font-family (global.css) already mirrors the WPF
 * AppFontFamily stack including SimSun; this module keeps the JS-side copy.
 */
export const AppFontStack = [
  'Segoe UI Variable Text',
  'Segoe UI Variable',
  'Segoe UI',
  'SimSun',
  'Microsoft YaHei UI',
  'Microsoft YaHei',
  'PingFang SC',
  'Hiragino Sans GB',
  'sans-serif',
].join(', ')

export function resolveFontStack(override?: string): string {
  return override != null && override.trim() !== '' ? `${override}, ${AppFontStack}` : AppFontStack
}

export const AppDisplayName = 'Universal Device Toolkit'
