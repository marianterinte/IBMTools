// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md (local workspace).
import { useQuery } from '@tanstack/react-query'
import { ProjectWorkspace } from './ProjectWorkspace'
import { ContextCard, Icon, PageIntro, WorkspaceColumns, WorkspaceFrame, type WorkspaceHealth } from './WorkspaceUi'
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
  const ready = database.isSuccess && database.data === 'ready'
  const health: WorkspaceHealth = {
    label: database.isPending ? 'Checking PostgreSQL…' : database.isSuccess ? statusLabel[database.data] : 'Local API unavailable',
    tone: ready ? 'good' : database.isPending ? 'neutral' : 'warning',
    checking: database.isFetching,
    refresh: () => void database.refetch(),
  }

  if (packagedOrigin && ready) return <ProjectWorkspace health={health} />

  return (
    <WorkspaceFrame health={health}>
      <PageIntro eyebrow="Local workspace" title="Workspace" accent="setup" description="DEV PRISM AI keeps its workspace and decision history on this workstation." hero />
      <WorkspaceColumns context={
        <ContextCard title="Local access" icon="info"><p>Project management is available only on the packaged HTTPS app.</p></ContextCard>
      }>
        <section className="panel">
          <div className="panel-heading"><span className="icon-tile"><Icon name="database" /></span><div><h2>Database readiness</h2><p>{health.label}</p></div></div>
          {!packagedOrigin ? <a className="button button-primary" href="https://localhost:5081">Open local workspace <Icon name="arrow" /></a> :
            <button className="button button-secondary" type="button" onClick={health.refresh} disabled={health.checking}><Icon name="refresh" /> Check status</button>}
        </section>
      </WorkspaceColumns>
    </WorkspaceFrame>
  )
}
