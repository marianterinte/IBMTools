import { useSyncExternalStore } from 'react'

type Theme = 'dark' | 'light'
const storageKey = 'dev-prism-theme'
const changes = new EventTarget()

function initialTheme(): Theme {
  try { return localStorage.getItem(storageKey) === 'light' ? 'light' : 'dark' }
  catch { return 'dark' }
}

let theme = initialTheme()
document.documentElement.dataset.theme = theme

function subscribe(listener: () => void) {
  changes.addEventListener('change', listener)
  return () => changes.removeEventListener('change', listener)
}

function setTheme(next: Theme) {
  theme = next
  document.documentElement.dataset.theme = next
  try { localStorage.setItem(storageKey, next) } catch { /* Theme still applies in this tab. */ }
  changes.dispatchEvent(new Event('change'))
}

export function ThemeToggle() {
  const light = useSyncExternalStore(subscribe, () => theme) === 'light'
  return <button type="button" className="icon-button theme-toggle" aria-label="Light theme" aria-pressed={light}
    title={light ? 'Switch to dark theme' : 'Switch to light theme'} onClick={() => setTheme(light ? 'dark' : 'light')}>
    <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round">
      {light
        ? <path d="M20.8 14A9 9 0 0 1 10 3.2 9 9 0 1 0 20.8 14Z" />
        : <path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5m-2 7a5 5 0 1 1-10 0 5 5 0 0 1 10 0Z" />}
    </svg>
  </button>
}
