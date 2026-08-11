import { nativeImage, nativeTheme, type NativeImage } from 'electron'

/**
 * Compact 16×16 Fluent-style glyphs for the tray context menu.
 * Light glyphs for dark system menus; dark glyphs for light menus.
 */
const LIGHT: Record<string, string> = {
  home: 'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAB3SURBVDhP5Yy5DYAwEASdkbkCIjKaoAmKIKMWeqECGiAjo5YxOsm2TifzRAQw0ka7s859G8ADE1Db7haRgBnYgQVo7eaUEEITJZFT1kcnMopjLadsQGedjJQXsj7prStyBQzAWJB0pJd4+5EpSDl2W8RKfzx4lQMayjNK9PBkfQAAAABJRU5ErkJggg==',
  keyboard:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABYSURBVDhPY2AYBXDw79+/zf/+/btDIt6MbMAdFBOJACh6YJx///6Z//v3j48YNi4DQE4zJ4aN1QBSAFYDoCbn4cBgF6Droa4BpAB0A85BA4cUfA7FxKEJAL+aC2KJKR8JAAAAAElFTkSuQmCC',
  rocket:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAADBSURBVDhPzZIhDgIxEEXX4XBcAIdDIXFrCI6EBLcOx0FQOCx3wHEBJBgcloRTvE+GdDfN0LKEIHhJ03T657edaVH8PZL6wMDHPwIYA1cbkiq/3wpwqA2AC9Dxmizh6nVyPUqvywJMEwZLr8sCLBIGK6/LAsy+NrBiAaOEwdxrkwDr8IRjlHyTtPPaF6Kr761okcHJ5rf/IbTO+r0NXTCTO3AGJsDG9iUNfe6T8PN60boMpzf9B7qma5LasHr42E95ADzhE1Evj0vAAAAAAElFTkSuQmCC',
  macro:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAB/SURBVDhP3Y/BCYAwDEW9uYRDeHMCJ3Axl3CC3lzAWydwllcpVKlpGvEmPgiU5uWHNM2nABywP5STcxdRkH8S0zmbwKJsXnJHxWwmTMdsJkznRyek9wCswAS0mlOgBJz3b8AonYJKwBxC6DWnQAR0se7Gi4AapgP47O5a+XzmALp3CJDrMvwFAAAAAElFTkSuQmCC',
  gauge:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACfSURBVDhP7ZAtDgJRDITX4XA4FA6HW4fDcQFOgAPFCbjAHoDg1hHw3IoDvK9kSMOW8hMOsF9S05lp+15V9XwFqIEd0HptgHn2vQEMgcZDax+k0oCj1yjnHkgATsAya8KHH4CrmU2yLoM2/wrrqqmZzbQoG3Rm+9J0Yjj09sAqmsYyPhtdcFtKucSwa4OPz4j4x92Ac9b+Qhv89EXWejru2UGqze1/s7cAAAAASUVORK5CYII=',
  play: 'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAB5SURBVDhP3ZGxDYAwDATTMQEdFR0T0LEHS9CxB4OwBAswAbNcUKRQ8JDI6RAnfRM7J9l27p8Ajb4VAUzA6r1vtWYiCo6YGai0J4sIQjZg0L4kL4IrC1Br/4OMIGQHOv1zIyOwjZIQ2JcpgvJzRkGYddSaCaA3bfsTnJaRzIBIvVsFAAAAAElFTkSuQmCC'
}

const DARK: Record<string, string> = {
  home: 'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACFSURBVDhP5ZCxDYNAEATJyOjgtXu6z1wFTVAFmYuhGFfgDpyRUYe1Aa/XCQwRAR5po9vZ4Jrm3uScO5KTu6d4O0QSyRfJBcA7pfSInV3cPUuSvAbA59SISirXcpUZQB+dgo4/5DJCcoiuHtaa2Whmzw2pRHdFD44bhSjVid1NovSPA5fyBS/4XiE/J8zRAAAAAElFTkSuQmCC',
  keyboard:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABeSURBVDhPY2AYBXCgoKCwW0FB4RmJeDeyAc9QTCQCoOiBceTk5KxUVFT4iGFjNQBEgxQQw8ZqACkAqwEgkxUVFYuxYZgL0PVQ1wBSAIoeeXn5GyABUjBID4qJQxMAAHSITP39FjhqAAAAAElFTkSuQmCC',
  rocket:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAADRSURBVDhPzZKhDsIwEIbncEjc9v/dzuFQWNwMwZEgkDgcL4LDIXmJOZ4AA3YOR3gFckuzNMeaEYLgS5qm1//+9O6aJH9PURSSpunYxj8CwIzk3a+Nve8FwDkwqEVkYDVR9OlBcrMAlFYXheTCGjjntlYXheS6w2BndVFILr820GYBmFoDACur7QTAXksAcAkMHiRPVvtG8PRKmxYYXP0e/w9+dDXJo59CRfJJ8uacm5M86H2WZROb26A/L8/zUXAufe3t/EVkqLo2qQ/th439lBcn0E0Dt+ffDwAAAABJRU5ErkJggg==',
  macro:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACMSURBVDhPY2AYVEBBQWG3goLCMwJ4N7o+OAApQBdDB3jVwCQVFBTWYrF5LbIarACvJBTgVYNXEgrwqhlGXgABOTk5K3l5+UMKCgphKioq7NjUYAB0A2D+l5eXvyAvL++BrgYD4DBgupycnBE2NRgAWVJZWVkWhFFVkGAALoBXjby8/A2Yv3FhkBpkPQAEPFIsajbFGQAAAABJRU5ErkJggg==',
  gauge:
    'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACxSURBVDhP7ZArEgIxEETjcEhc0p2PwyFxOBwn4AY4UNyAE6ARSBzFAbgTnmoq7GazbBUH2Fc1Zrp7ZhJjRgZxzi0BHEneVN77PYBV7euRUpqSPOfQToNUGkDyqgohzOrcBwkk7yQ3tSY0HMAFwDPGmGrd5M2DYV1lrZ075xZa1DHoTBk6zUwZ/vYAnEhuG1OM0crYNNrggeSjDGdt8vMZJfnjXr1z/0UbdDqAda2NtLwBSUUnLujde5oAAAAASUVORK5CYII=',
  play: 'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACASURBVDhPY2AYnkBZWVkWXYwkoKioWKygoLBFWVlZBV2OKAA14BkIy8vLV6moqLCjq8ELkA2AGnJSXl7eEV0dToBuABKerqSkJIauHgPgMQDkmhuysrLa6HpQAC4DiPYKNgNICkw0A0iPTpABIL8qKCjEossRBeTk5KyICu1BAQBX/jn5bN+t7AAAAABJRU5ErkJggg=='
}

const cache = new Map<string, NativeImage>()

function load(set: Record<string, string>, key: string): NativeImage | undefined {
  const b64 = set[key]
  if (!b64) return undefined
  const cacheKey = `${nativeTheme.shouldUseDarkColors ? 'L' : 'D'}:${key}`
  const hit = cache.get(cacheKey)
  if (hit) return hit
  const image = nativeImage.createFromDataURL(`data:image/png;base64,${b64}`)
  if (image.isEmpty()) return undefined
  cache.set(cacheKey, image)
  return image
}

/** Map WPF SymbolRegular / pipeline.iconName to a tray glyph. */
export function trayIconForSymbol(symbol?: string | null): NativeImage | undefined {
  const set = nativeTheme.shouldUseDarkColors ? LIGHT : DARK
  const name = (symbol ?? '').replace(/\d+$/, '').toLowerCase()
  if (name.includes('home')) return load(set, 'home')
  if (name.includes('keyboard')) return load(set, 'keyboard')
  if (name.includes('rocket')) return load(set, 'rocket')
  if (name.includes('receipt') || name.includes('macro')) return load(set, 'macro')
  if (name.includes('gauge') || name.includes('topspeed')) return load(set, 'gauge')
  if (name.includes('play') || name === '') return load(set, 'play')
  return load(set, 'play')
}

export function trayNavIcon(id: 'home' | 'keyboard' | 'rocket' | 'macro' | 'gauge'): NativeImage | undefined {
  const set = nativeTheme.shouldUseDarkColors ? LIGHT : DARK
  return load(set, id)
}

export function clearTrayIconCache(): void {
  cache.clear()
}
