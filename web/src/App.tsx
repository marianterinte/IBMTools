// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md.
// Public first-run shell: report local startup without exposing project content.
import { useEffect, useState } from 'react'
import './App.css'

type LocalStatus = 'checking' | 'scaffold' | 'unavailable'

const statusLabel: Record<LocalStatus, string> = {
  checking: 'Checking the local service…',
  scaffold: 'Local service reachable · Setup not ready',
  unavailable: 'Local service unavailable',
}

/** Reads the local readiness endpoint without probing or authenticating any provider. */
async function readLocalStatus(signal: AbortSignal): Promise<LocalStatus> {
  const response = await fetch('/api/v1/health', {
    signal,
    cache: 'no-store',
    headers: { Accept: 'application/json' },
  })
  if (response.status !== 503) return 'unavailable'

  const body: unknown = await response.json()
  return typeof body === 'object' && body !== null && 'stage' in body && body.stage === 'scaffold'
    ? 'scaffold'
    : 'unavailable'
}

/** Shows the local MVP boundary and the current setup status. */
export default function App() {
  const [status, setStatus] = useState<LocalStatus>('checking')
  const [check, setCheck] = useState(0)

  useEffect(() => {
    const pending = new AbortController()
    const timeout = window.setTimeout(() => {
      pending.abort()
      setStatus('unavailable')
    }, 5_000)
    void readLocalStatus(pending.signal)
      .then((next) => {
        if (!pending.signal.aborted) setStatus(next)
      })
      .catch(() => {
        if (!pending.signal.aborted) setStatus('unavailable')
      })
      .finally(() => window.clearTimeout(timeout))
    return () => {
      window.clearTimeout(timeout)
      pending.abort()
    }
  }, [check])

  /** Rechecks local readiness; does not initiate an import or provider connection. */
  function recheck() {
    setStatus('checking')
    setCheck((current) => current + 1)
  }

  return (
    <main className="shell">
      <header className="brand"><span aria-hidden="true" className="brand-mark" />DEV PRISM AI</header>
      <section className="intro" aria-labelledby="title">
        <p className="eyebrow">Local MVP · Setup</p>
        <h1 id="title">Your project, organized by feature.</h1>
        <p className="lead">A local workspace for evidence from Azure DevOps and Figma, and the decisions that connect them.</p>
        <div className="setup">
          <h2>Local service</h2>
          <p role="status">{statusLabel[status]}</p>
          <button type="button" onClick={recheck} disabled={status === 'checking'}>Check status</button>
        </div>
        <p className="note">Project setup and remote provider connections are not available in this build. No provider has been contacted.</p>
      </section>
    </main>
  )
}
