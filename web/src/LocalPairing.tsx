// Spec: Documentation/dev-prism-ai-specs/12_SECURITY_AND_OPERATIONS.md.
// Pairs the browser with the local service without persisting credentials or session tokens.
import { type FormEvent, useEffect, useState } from 'react'

type Session = {
  status: 'authenticated' | 'pairingRequired' | 'pairingUnavailable'
  csrfToken: string
}

/** Retrieves session state and an antiforgery token from the local API only. */
async function readSession(signal?: AbortSignal): Promise<Session> {
  const response = await fetch('/api/v1/auth/session', { signal, cache: 'no-store' })
  if (!response.ok) throw new Error('Session unavailable')
  return response.json()
}

/** Provides explicit pairing or sign-out at the packaged HTTPS origin. */
export function LocalPairing() {
  const [session, setSession] = useState<Session | null>(null)
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const packaged = window.location.origin === 'https://localhost:5081'

  useEffect(() => {
    if (!packaged) return
    const controller = new AbortController()
    void readSession(controller.signal).then(setSession).catch(() => {
      if (!controller.signal.aborted) setError('Could not check the local session. Reload to retry.')
    })
    return () => controller.abort()
  }, [packaged])

  /** Sends an explicit mutation with the volatile antiforgery token from the last session read. */
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!session || busy) return
    const signingOut = session.status === 'authenticated'
    setBusy(true)
    setError('')
    try {
      const response = await fetch(`/api/v1/auth/${signingOut ? 'sign-out' : 'pair'}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-DevPrism-CSRF': session.csrfToken },
        body: signingOut ? undefined : JSON.stringify({ code: code.trim() }),
      })
      if (!response.ok) {
        setError(response.status === 429 ? 'Too many attempts. Wait one minute before retrying.' : 'The local operation was refused.')
      }
      setSession(await readSession())
    } catch {
      setError('The local service could not complete the request. Reload before retrying.')
    } finally {
      setCode('')
      setBusy(false)
    }
  }

  return (
    <section className="setup" aria-labelledby="pairing-title">
      <h2 id="pairing-title">Local operator</h2>
      {!packaged ? <p>Open the packaged HTTPS app to pair.</p> : null}
      {session?.status === 'pairingUnavailable' ? <p>No active pairing grant. Start the app with a fresh local code.</p> : null}
      {session?.status === 'pairingRequired' || session?.status === 'authenticated' ? (
        <form onSubmit={submit}>
          {session.status === 'pairingRequired' ? (
            <div className="pairing-field">
              <label htmlFor="pairing-code">One-time pairing code</label>
              <input id="pairing-code" type="password" autoComplete="off" required maxLength={64}
                value={code} onChange={(event) => setCode(event.target.value)} />
            </div>
          ) : <p>Paired for this session.</p>}
          <button type="submit" disabled={busy}>{session.status === 'authenticated' ? 'Sign out' : 'Pair browser'}</button>
        </form>
      ) : null}
      {error ? <p role="alert">{error}</p> : null}
    </section>
  )
}
