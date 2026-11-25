// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md (local workspace).
import { useQuery } from '@tanstack/react-query'
import { ProjectWorkspace } from './ProjectWorkspace'
import './App.css'

type DatabaseState = 'ready' | 'notConfigured' | 'migrationRequired' | 'unreachable'

const statusLabel: Record<DatabaseState, string> = {
  ready: 'PostgreSQL connected',
  notConfigured: 'PostgreSQL is not configured',
  migrationRequired: 'PostgreSQL connected · Run the database migrations',
  unreachable: 'Cannot connect to PostgreSQL · Check the local service and connection settings',
}

/** Queries the local database status, never Azure DevOps or Figma. */
async function readDatabaseStatus(signal: AbortSignal): Promise<DatabaseState> {
  const response = await fetch('/api/v1/health', {
    signal: AbortSignal.any([signal, AbortSignal.timeout(5_000)]),
    cache: 'no-store',
    headers: { Accept: 'application/json' },
  })
  if (!response.ok && response.status !== 503) throw new Error('Unexpected health status')
  const body: unknown = await response.json()
  if (typeof body !== 'object' || body === null || !('database' in body)
    || typeof body.database !== 'string' || !Object.hasOwn(statusLabel, body.database))
    throw new Error('Unexpected health response')
  return body.database as DatabaseState
}

/** Opens the single-operator workspace directly when the local database is ready. */
export default function App() {
  const database = useQuery({ queryKey: ['database-health'], queryFn: ({ signal }) => readDatabaseStatus(signal) })
  const packagedOrigin = window.location.origin === 'https://localhost:5081'
  const label = database.isFetching ? 'Checking PostgreSQL…' : database.isSuccess
    ? statusLabel[database.data] : 'Local API unavailable'

  return (
    <main className="shell">
      <header className="brand"><span aria-hidden="true" className="brand-mark" />DEV PRISM AI</header>
      <section className="intro" aria-labelledby="title">
        <p className="eyebrow">Local MVP · Workspace</p>
        <h1 id="title">Your project, organized by feature.</h1>
        <p className="lead">A local workspace for evidence from Azure DevOps and Figma, and the decisions that connect them.</p>
        <div className="setup">
          <h2>Local database</h2>
          <p role="status">{label}</p>
          <button type="button" onClick={() => void database.refetch()} disabled={database.isFetching}>Check status</button>
        </div>
        {packagedOrigin && database.isSuccess && database.data === 'ready' ? <ProjectWorkspace /> : null}
        {!packagedOrigin ? <p className="note">Open the local HTTPS app at <a href="https://localhost:5081">localhost:5081</a> to manage projects.</p> : null}
        <p className="note">Azure DevOps and Figma connections are not available in this build.</p>
      </section>
    </main>
  )
}
