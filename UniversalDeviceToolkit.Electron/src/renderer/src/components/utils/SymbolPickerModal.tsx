import { useEffect, useRef, useState } from 'react'
import { create } from 'zustand'
import { useTranslation } from 'react-i18next'
import {
  Apps24Regular,
  ArrowDownload24Regular,
  ArrowSync24Regular,
  BatteryCharge24Regular,
  Battery024Regular,
  Battery624Regular,
  Book24Regular,
  Bug24Regular,
  Calendar24Regular,
  Camera24Regular,
  Chat24Regular,
  CheckmarkCircle24Regular,
  Cloud24Regular,
  Code24Regular,
  Color24Regular,
  Delete24Regular,
  Desktop24Regular,
  Document24Regular,
  Edit24Regular,
  ErrorCircle24Regular,
  Eye24Regular,
  Folder24Regular,
  Gauge24Regular,
  Globe24Regular,
  Heart24Regular,
  History24Regular,
  Home24Regular,
  Image24Regular,
  Info24Regular,
  Keyboard24Regular,
  Key24Regular,
  Lightbulb24Regular,
  Link24Regular,
  LockClosed24Regular,
  Mail24Regular,
  Mic24Regular,
  Money24Regular,
  MusicNote224Regular,
  Open24Regular,
  Options24Regular,
  PaintBrush24Regular,
  People24Regular,
  Play24Regular,
  Power24Regular,
  Rocket24Regular,
  Save24Regular,
  Search24Regular,
  Settings24Regular,
  Shield24Regular,
  Speaker024Regular,
  Star24Regular,
  Stop24Regular,
  Tag24Regular,
  ThumbLike24Regular,
  Timer24Regular,
  TopSpeed24Regular,
  UsbStick24Regular,
  UsbPlug24Regular,
  Warning24Regular,
  Wrench24Regular,
  XboxController24Regular
} from '@fluentui/react-icons'
import './utils.css'

/**
 * Port of WPF SymbolRegularPicker: an icon picker with a search filter
 * (debounced like the WPF DebounceDispatcher) over a grid of symbol buttons.
 * Returns the selected icon name, or null for "Default".
 */

const ICON_CATALOG: Array<{ name: string; icon: React.JSX.Element }> = [
  { name: 'Apps', icon: <Apps24Regular /> },
  { name: 'BatteryCharge', icon: <BatteryCharge24Regular /> },
  { name: 'Battery0', icon: <Battery024Regular /> },
  { name: 'Battery6', icon: <Battery624Regular /> },
  { name: 'Book', icon: <Book24Regular /> },
  { name: 'Bug', icon: <Bug24Regular /> },
  { name: 'Calendar', icon: <Calendar24Regular /> },
  { name: 'Camera', icon: <Camera24Regular /> },
  { name: 'Chat', icon: <Chat24Regular /> },
  { name: 'CheckmarkCircle', icon: <CheckmarkCircle24Regular /> },
  { name: 'Cloud', icon: <Cloud24Regular /> },
  { name: 'Code', icon: <Code24Regular /> },
  { name: 'Color', icon: <Color24Regular /> },
  { name: 'Delete', icon: <Delete24Regular /> },
  { name: 'Desktop', icon: <Desktop24Regular /> },
  { name: 'Document', icon: <Document24Regular /> },
  { name: 'Download', icon: <ArrowDownload24Regular /> },
  { name: 'Edit', icon: <Edit24Regular /> },
  { name: 'ErrorCircle', icon: <ErrorCircle24Regular /> },
  { name: 'Eye', icon: <Eye24Regular /> },
  { name: 'Folder', icon: <Folder24Regular /> },
  { name: 'GameController', icon: <XboxController24Regular /> },
  { name: 'Gauge', icon: <Gauge24Regular /> },
  { name: 'Globe', icon: <Globe24Regular /> },
  { name: 'Heart', icon: <Heart24Regular /> },
  { name: 'History', icon: <History24Regular /> },
  { name: 'Home', icon: <Home24Regular /> },
  { name: 'Image', icon: <Image24Regular /> },
  { name: 'Info', icon: <Info24Regular /> },
  { name: 'Keyboard', icon: <Keyboard24Regular /> },
  { name: 'Key', icon: <Key24Regular /> },
  { name: 'Lightbulb', icon: <Lightbulb24Regular /> },
  { name: 'Link', icon: <Link24Regular /> },
  { name: 'Lock', icon: <LockClosed24Regular /> },
  { name: 'Mail', icon: <Mail24Regular /> },
  { name: 'Mic', icon: <Mic24Regular /> },
  { name: 'Money', icon: <Money24Regular /> },
  { name: 'MusicNote', icon: <MusicNote224Regular /> },
  { name: 'Open', icon: <Open24Regular /> },
  { name: 'Options', icon: <Options24Regular /> },
  { name: 'PaintBrush', icon: <PaintBrush24Regular /> },
  { name: 'People', icon: <People24Regular /> },
  { name: 'Play', icon: <Play24Regular /> },
  { name: 'Power', icon: <Power24Regular /> },
  { name: 'Rocket', icon: <Rocket24Regular /> },
  { name: 'Save', icon: <Save24Regular /> },
  { name: 'Search', icon: <Search24Regular /> },
  { name: 'Settings', icon: <Settings24Regular /> },
  { name: 'Shield', icon: <Shield24Regular /> },
  { name: 'Speaker', icon: <Speaker024Regular /> },
  { name: 'Star', icon: <Star24Regular /> },
  { name: 'Stop', icon: <Stop24Regular /> },
  { name: 'Sync', icon: <ArrowSync24Regular /> },
  { name: 'Tag', icon: <Tag24Regular /> },
  { name: 'ThumbLike', icon: <ThumbLike24Regular /> },
  { name: 'Timer', icon: <Timer24Regular /> },
  { name: 'TopSpeed', icon: <TopSpeed24Regular /> },
  { name: 'USBStick', icon: <UsbStick24Regular /> },
  { name: 'UsbPlug', icon: <UsbPlug24Regular /> },
  { name: 'Warning', icon: <Warning24Regular /> },
  { name: 'Wrench', icon: <Wrench24Regular /> }
]

interface SymbolPickerRequest {
  id: number
}

let requestSeq = 0
let pendingResolve: ((icon: string | null) => void) | null = null

interface SymbolPickerState {
  request: SymbolPickerRequest | null
  show: () => void
  settle: (icon: string | null) => void
}

const useSymbolPickerStore = create<SymbolPickerState>((set) => ({
  request: null,
  show: () => set({ request: { id: ++requestSeq } }),
  settle: (icon) => {
    pendingResolve?.(icon)
    pendingResolve = null
    set({ request: null })
  }
}))

export function openSymbolPicker(): Promise<string | null> {
  return new Promise((resolve) => {
    pendingResolve = resolve
    useSymbolPickerStore.getState().show()
  })
}

const DEBOUNCE_MS = 300

export default function SymbolPickerModalHost(): React.JSX.Element {
  const { t } = useTranslation()
  const request = useSymbolPickerStore((s) => s.request)
  const settle = useSymbolPickerStore((s) => s.settle)
  const [filter, setFilter] = useState('')
  const [filtered, setFiltered] = useState(ICON_CATALOG)
  const inputRef = useRef<HTMLInputElement>(null)
  const debounceRef = useRef<number | undefined>(undefined)

  useEffect(() => {
    if (!request) return
    setFilter('')
    setFiltered(ICON_CATALOG)
    const timer = window.setTimeout(() => inputRef.current?.focus(), 0)
    return () => window.clearTimeout(timer)
  }, [request])

  useEffect(() => {
    return () => window.clearTimeout(debounceRef.current)
  }, [])

  const handleFilterChange = (value: string): void => {
    setFilter(value)
    window.clearTimeout(debounceRef.current)
    debounceRef.current = window.setTimeout(() => {
      const needle = value.trim().toLowerCase()
      setFiltered(
        needle.length === 0
          ? ICON_CATALOG
          : ICON_CATALOG.filter((item) => item.name.toLowerCase().includes(needle))
      )
    }, DEBOUNCE_MS)
  }

  if (!request) return <></>

  return (
    <div className="udt-utils-backdrop" onClick={() => settle(null)}>
      <div
        className="udt-utils-modal"
        style={{ width: 760, height: 560 }}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-utils-modal__title" style={{ paddingBottom: 8 }}>
          {t('wpf.symbolRegularPickertitle')}
        </div>
        <div className="udt-utils-modal__body" style={{ paddingTop: 0 }}>
          <input
            ref={inputRef}
            className="udt-utils-input"
            value={filter}
            placeholder={t('wpf.filter')}
            onChange={(event) => handleFilterChange(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Escape') {
                event.preventDefault()
                settle(null)
              }
            }}
          />
          <div className="udt-utils-symbol-grid" style={{ marginTop: 10 }}>
            {filtered.map((item) => (
              <button
                key={item.name}
                type="button"
                className="udt-utils-symbol-cell"
                title={item.name}
                onClick={() => settle(item.name)}
              >
                {item.icon}
              </button>
            ))}
          </div>
          {filtered.length === 0 && (
            <p className="udt-utils-text" style={{ textAlign: 'center', padding: 20 }}>
              {t('wpf.symbolRegularPickerempty')}
            </p>
          )}
        </div>
        <div className="udt-utils-modal__actions">
          <button type="button" className="udt-utils-button" onClick={() => settle(null)}>
            {t('wpf.default')}
          </button>
        </div>
      </div>
    </div>
  )
}
