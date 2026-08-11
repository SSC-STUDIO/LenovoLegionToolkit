/**
 * Spectrum keyboard layout geometry — port of the WPF
 * SpectrumKeyboardANSIControl/ISOControl/JisControl.xaml zone tables
 * (Width × Height @ KeyCode, 2px margins; null code = non-selectable spacer).
 */

export interface SpectrumZone {
  w: number
  h: number
  code: number | null
}

export const SPECTRUM_KEYBOARD_LAYOUTS: Record<'Ansi' | 'Iso' | 'Jis', SpectrumZone[][]> = {
  Ansi: [
    [
      { w: 28, h: 24, code: 0x01 },
      { w: 28, h: 24, code: 0x02 },
      { w: 28, h: 24, code: 0x03 },
      { w: 28, h: 24, code: 0x04 },
      { w: 28, h: 24, code: 0x05 },
      { w: 28, h: 24, code: 0x06 },
      { w: 28, h: 24, code: 0x07 },
      { w: 28, h: 24, code: 0x08 },
      { w: 28, h: 24, code: 0x09 },
      { w: 28, h: 24, code: 0x0a },
      { w: 28, h: 24, code: 0x0b },
      { w: 28, h: 24, code: 0x0c },
      { w: 28, h: 24, code: 0x0d },
      { w: 28, h: 24, code: 0x0e },
      { w: 28, h: 24, code: 0x0f },
      { w: 32, h: 24, code: 0x10 },
      { w: 32, h: 24, code: 0x11 },
      { w: 32, h: 24, code: 0x12 },
      { w: 32, h: 24, code: 0x13 },
      { w: 32, h: 24, code: 0x14 }
    ],
    [
      { w: 24, h: 32, code: 0x16 },
      { w: 32, h: 32, code: 0x17 },
      { w: 32, h: 32, code: 0x18 },
      { w: 32, h: 32, code: 0x19 },
      { w: 32, h: 32, code: 0x1a },
      { w: 32, h: 32, code: 0x1b },
      { w: 32, h: 32, code: 0x1c },
      { w: 32, h: 32, code: 0x1d },
      { w: 32, h: 32, code: 0x1e },
      { w: 32, h: 32, code: 0x1f },
      { w: 32, h: 32, code: 0x20 },
      { w: 32, h: 32, code: 0x21 },
      { w: 32, h: 32, code: 0x22 },
      { w: 52, h: 32, code: 0x38 }
    ],
    [
      { w: 44, h: 32, code: 0x40 },
      { w: 32, h: 32, code: 0x42 },
      { w: 32, h: 32, code: 0x43 },
      { w: 32, h: 32, code: 0x44 },
      { w: 32, h: 32, code: 0x45 },
      { w: 32, h: 32, code: 0x46 },
      { w: 32, h: 32, code: 0x47 },
      { w: 32, h: 32, code: 0x48 },
      { w: 32, h: 32, code: 0x49 },
      { w: 32, h: 32, code: 0x4a },
      { w: 32, h: 32, code: 0x4b },
      { w: 32, h: 32, code: 0x4c },
      { w: 32, h: 32, code: 0x4d },
      { w: 32, h: 32, code: 0x4e }
    ],
    [
      { w: 56, h: 32, code: 0x55 },
      { w: 32, h: 32, code: 0x6d },
      { w: 32, h: 32, code: 0x6e },
      { w: 32, h: 32, code: 0x58 },
      { w: 32, h: 32, code: 0x59 },
      { w: 32, h: 32, code: 0x5a },
      { w: 32, h: 32, code: 0x71 },
      { w: 32, h: 32, code: 0x72 },
      { w: 32, h: 32, code: 0x5b },
      { w: 32, h: 32, code: 0x5c },
      { w: 32, h: 32, code: 0x5d },
      { w: 32, h: 32, code: 0x5f },
      { w: 56, h: 32, code: 0x77 }
    ],
    [
      { w: 74, h: 32, code: 0x6a },
      { w: 32, h: 32, code: 0x82 },
      { w: 32, h: 32, code: 0x83 },
      { w: 32, h: 32, code: 0x6f },
      { w: 32, h: 32, code: 0x70 },
      { w: 32, h: 32, code: 0x87 },
      { w: 32, h: 32, code: 0x88 },
      { w: 32, h: 32, code: 0x73 },
      { w: 32, h: 32, code: 0x74 },
      { w: 32, h: 32, code: 0x75 },
      { w: 32, h: 32, code: 0x76 },
      { w: 74, h: 32, code: 0x8d }
    ],
    [
      { w: 38, h: 32, code: 0x7f },
      { w: 32, h: 32, code: 0x80 },
      { w: 32, h: 32, code: 0x96 },
      { w: 32, h: 32, code: 0x97 },
      { w: 176, h: 32, code: 0x98 },
      { w: 32, h: 32, code: 0x9a },
      { w: 32, h: 32, code: 0x9b },
      { w: 32, h: 32, code: null },
      { w: 32, h: 32, code: 0x9d },
      { w: 32, h: 32, code: null }
    ],
    [
      { w: 398, h: 32, code: null },
      { w: 32, h: 32, code: 0x9c },
      { w: 32, h: 32, code: 0x9f },
      { w: 32, h: 32, code: 0xa1 }
    ]
  ],
  Iso: [
    [
      { w: 28, h: 24, code: 0x01 },
      { w: 28, h: 24, code: 0x02 },
      { w: 28, h: 24, code: 0x03 },
      { w: 28, h: 24, code: 0x04 },
      { w: 28, h: 24, code: 0x05 },
      { w: 28, h: 24, code: 0x06 },
      { w: 28, h: 24, code: 0x07 },
      { w: 28, h: 24, code: 0x08 },
      { w: 28, h: 24, code: 0x09 },
      { w: 28, h: 24, code: 0x0a },
      { w: 28, h: 24, code: 0x0b },
      { w: 28, h: 24, code: 0x0c },
      { w: 28, h: 24, code: 0x0d },
      { w: 28, h: 24, code: 0x0e },
      { w: 28, h: 24, code: 0x0f },
      { w: 32, h: 24, code: 0x10 },
      { w: 32, h: 24, code: 0x11 },
      { w: 32, h: 24, code: 0x12 },
      { w: 32, h: 24, code: 0x13 },
      { w: 32, h: 24, code: 0x14 }
    ],
    [
      { w: 24, h: 32, code: 0x16 },
      { w: 32, h: 32, code: 0x17 },
      { w: 32, h: 32, code: 0x18 },
      { w: 32, h: 32, code: 0x19 },
      { w: 32, h: 32, code: 0x1a },
      { w: 32, h: 32, code: 0x1b },
      { w: 32, h: 32, code: 0x1c },
      { w: 32, h: 32, code: 0x1d },
      { w: 32, h: 32, code: 0x1e },
      { w: 32, h: 32, code: 0x1f },
      { w: 32, h: 32, code: 0x20 },
      { w: 32, h: 32, code: 0x21 },
      { w: 32, h: 32, code: 0x22 },
      { w: 52, h: 32, code: 0x38 }
    ],
    [
      { w: 44, h: 32, code: 0x40 },
      { w: 32, h: 32, code: 0x42 },
      { w: 32, h: 32, code: 0x43 },
      { w: 32, h: 32, code: 0x44 },
      { w: 32, h: 32, code: 0x45 },
      { w: 32, h: 32, code: 0x46 },
      { w: 32, h: 32, code: 0x47 },
      { w: 32, h: 32, code: 0x48 },
      { w: 32, h: 32, code: 0x49 },
      { w: 32, h: 32, code: 0x4a },
      { w: 32, h: 32, code: 0x4b },
      { w: 32, h: 32, code: 0x4c },
      { w: 36, h: 32, code: 0x4d }
    ],
    [
      { w: 56, h: 32, code: 0x55 },
      { w: 32, h: 32, code: 0x6d },
      { w: 32, h: 32, code: 0x6e },
      { w: 32, h: 32, code: 0x58 },
      { w: 32, h: 32, code: 0x59 },
      { w: 32, h: 32, code: 0x5a },
      { w: 32, h: 32, code: 0x71 },
      { w: 32, h: 32, code: 0x72 },
      { w: 32, h: 32, code: 0x5b },
      { w: 32, h: 32, code: 0x5c },
      { w: 32, h: 32, code: 0x5d },
      { w: 32, h: 32, code: 0x5f },
      { w: 24, h: 32, code: 0xa8 }
    ],
    [
      { w: 28, h: 68, code: 0x77 },
      { w: 38, h: 32, code: 0x6a },
      { w: 32, h: 32, code: 0x4e },
      { w: 32, h: 32, code: 0x82 },
      { w: 32, h: 32, code: 0x83 },
      { w: 32, h: 32, code: 0x6f },
      { w: 32, h: 32, code: 0x70 },
      { w: 32, h: 32, code: 0x87 },
      { w: 32, h: 32, code: 0x88 },
      { w: 32, h: 32, code: 0x73 },
      { w: 32, h: 32, code: 0x74 },
      { w: 32, h: 32, code: 0x75 },
      { w: 32, h: 32, code: 0x76 },
      { w: 74, h: 32, code: 0x8d }
    ],
    [
      { w: 38, h: 32, code: 0x7f },
      { w: 32, h: 32, code: 0x80 },
      { w: 32, h: 32, code: 0x96 },
      { w: 32, h: 32, code: 0x97 },
      { w: 176, h: 32, code: 0x98 },
      { w: 32, h: 32, code: 0x9a },
      { w: 32, h: 32, code: 0x9b },
      { w: 32, h: 32, code: null },
      { w: 32, h: 32, code: 0x9d },
      { w: 32, h: 32, code: null }
    ],
    [
      { w: 398, h: 32, code: null },
      { w: 32, h: 32, code: 0x9c },
      { w: 32, h: 32, code: 0x9f },
      { w: 32, h: 32, code: 0xa1 }
    ]
  ],
  Jis: [
    [
      { w: 28, h: 24, code: 0x01 },
      { w: 28, h: 24, code: 0x02 },
      { w: 28, h: 24, code: 0x03 },
      { w: 28, h: 24, code: 0x04 },
      { w: 28, h: 24, code: 0x05 },
      { w: 28, h: 24, code: 0x06 },
      { w: 28, h: 24, code: 0x07 },
      { w: 28, h: 24, code: 0x08 },
      { w: 28, h: 24, code: 0x09 },
      { w: 28, h: 24, code: 0x0a },
      { w: 28, h: 24, code: 0x0b },
      { w: 28, h: 24, code: 0x0c },
      { w: 28, h: 24, code: 0x0d },
      { w: 28, h: 24, code: 0x0e },
      { w: 28, h: 24, code: 0x0f },
      { w: 32, h: 24, code: 0x10 },
      { w: 32, h: 24, code: 0x11 },
      { w: 32, h: 24, code: 0x12 },
      { w: 32, h: 24, code: 0x13 },
      { w: 32, h: 24, code: 0x14 }
    ],
    [
      { w: 24, h: 32, code: 0x16 },
      { w: 32, h: 32, code: 0x17 },
      { w: 32, h: 32, code: 0x18 },
      { w: 32, h: 32, code: 0x19 },
      { w: 32, h: 32, code: 0x1a },
      { w: 32, h: 32, code: 0x1b },
      { w: 32, h: 32, code: 0x1c },
      { w: 32, h: 32, code: 0x1d },
      { w: 32, h: 32, code: 0x1e },
      { w: 32, h: 32, code: 0x1f },
      { w: 32, h: 32, code: 0x20 },
      { w: 32, h: 32, code: 0x21 },
      { w: 32, h: 32, code: 0x22 },
      { w: 24, h: 32, code: 0xa8 },
      { w: 24, h: 32, code: 0x38 }
    ],
    [
      { w: 44, h: 32, code: 0x40 },
      { w: 32, h: 32, code: 0x42 },
      { w: 32, h: 32, code: 0x43 },
      { w: 32, h: 32, code: 0x44 },
      { w: 32, h: 32, code: 0x45 },
      { w: 32, h: 32, code: 0x46 },
      { w: 32, h: 32, code: 0x47 },
      { w: 32, h: 32, code: 0x48 },
      { w: 32, h: 32, code: 0x49 },
      { w: 32, h: 32, code: 0x4a },
      { w: 32, h: 32, code: 0x4b },
      { w: 32, h: 32, code: 0x60 },
      { w: 36, h: 32, code: 0x4c }
    ],
    [
      { w: 56, h: 32, code: 0x55 },
      { w: 32, h: 32, code: 0x6d },
      { w: 32, h: 32, code: 0x6e },
      { w: 32, h: 32, code: 0x58 },
      { w: 32, h: 32, code: 0x59 },
      { w: 32, h: 32, code: 0x5a },
      { w: 32, h: 32, code: 0x71 },
      { w: 32, h: 32, code: 0x72 },
      { w: 32, h: 32, code: 0x5b },
      { w: 32, h: 32, code: 0x5c },
      { w: 32, h: 32, code: 0x5d },
      { w: 32, h: 32, code: 0x5f },
      { w: 24, h: 32, code: 0x4d }
    ],
    [
      { w: 28, h: 68, code: 0x77 },
      { w: 38, h: 32, code: 0x6a },
      { w: 32, h: 32, code: 0x4e },
      { w: 32, h: 32, code: 0x82 },
      { w: 32, h: 32, code: 0x83 },
      { w: 32, h: 32, code: 0x6f },
      { w: 32, h: 32, code: 0x70 },
      { w: 32, h: 32, code: 0x87 },
      { w: 32, h: 32, code: 0x88 },
      { w: 32, h: 32, code: 0x73 },
      { w: 32, h: 32, code: 0x74 },
      { w: 32, h: 32, code: 0x75 },
      { w: 32, h: 32, code: 0x76 },
      { w: 74, h: 32, code: 0x8d }
    ],
    [
      { w: 38, h: 32, code: 0x7f },
      { w: 32, h: 32, code: 0x80 },
      { w: 32, h: 32, code: 0x96 },
      { w: 32, h: 32, code: 0x97 },
      { w: 32, h: 32, code: 0xa9 },
      { w: 104, h: 32, code: 0x98 },
      { w: 32, h: 32, code: 0xaa },
      { w: 32, h: 32, code: 0xab },
      { w: 32, h: 32, code: 0x9b },
      { w: 32, h: 32, code: null },
      { w: 32, h: 32, code: 0x9d },
      { w: 32, h: 32, code: null }
    ],
    [
      { w: 398, h: 32, code: null },
      { w: 32, h: 32, code: 0x9c },
      { w: 32, h: 32, code: 0x9f },
      { w: 32, h: 32, code: 0xa1 }
    ]
  ]
}

export type SpectrumKeyboardLayoutName = keyof typeof SPECTRUM_KEYBOARD_LAYOUTS

export function normalizeKeyboardLayout(name: string): SpectrumKeyboardLayoutName {
  const lowered = name.toLowerCase()
  if (lowered.includes('iso')) return 'Iso'
  if (lowered.includes('jis')) return 'Jis'
  return 'Ansi'
}
