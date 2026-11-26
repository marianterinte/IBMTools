// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md and 11_API_AND_EVENTS.md.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Link, Navigate, Route, Routes, useNavigate, useParams } from 'react-router'
import { SyncHistory } from './SyncHistory'
import { FeatureWorkspace } from './FeatureWorkspace'
import { ProviderConnections } from './ProviderConnections'
import { McpProfilesPanel } from './McpSettings'
import { ContextCard, CreateDialog, EmptyState, Icon, PageIntro, SourceContext, WorkspaceColumns, WorkspaceFrame, type ProjectOption, type WorkspaceHealth } from './WorkspaceUi'

type Project = ProjectOption & { version: number }
type ProjectList = { items: Project[] }
const api = '/api/v1/projects'
const projectsKey = ['operator-projects'] as const

async function readJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { cache: 'no-store', ...init })
  if (response.status === 404) throw new Error('Project unavailable.')
  if (response.status === 409) throw new Error('A project already uses that slug.')
  if (response.status === 400) throw new Error('Check the project name and slug.')
  if (!response.ok) throw new Error('Local project data is unavailable. Check the database setup.')
  return response.json()
}

export function ProjectWorkspace({ health }: { health: WorkspaceHealth }) {
  const projects = useQuery({ queryKey: projectsKey, queryFn: ({ signal }) => readJson<ProjectList>(api, { signal }) })
  const frame = { health, projects: projects.data?.items, projectsLoading: projects.isPending }
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/projects" replace />} />
      <Route path="/projects" element={<WorkspaceFrame {...frame}><ProjectPicker projects={projects} /></WorkspaceFrame>} />
      <Route path="/settings/mcp" element={<WorkspaceFrame {...frame}>
        <PageIntro eyebrow="Settings / Integrations" title="MCP" accent="profiles" description="Manage the default MCP server for each provider." hero />
        <WorkspaceColumns context={<ContextCard title="Local sign-in" icon="info"><p>Profiles are shared through the configured database. Sign-in and connection status belong to this running app on this PC. Remote provider writes and imports are not enabled.</p></ContextCard>}>
          <McpProfilesPanel />
        </WorkspaceColumns>
      </WorkspaceFrame>} />
      <Route path="/projects/:projectId/features/:featureId" element={<ProjectView frame={frame} />} />
      <Route path="/projects/:projectId/:section?" element={<ProjectView frame={frame} />} />
      <Route path="*" element={<WorkspaceFrame {...frame}>
        <PageIntro eyebrow="Local workspace" title="Page unavailable" description="Return to the project directory to continue." action={<Link className="button button-secondary" to="/projects"><Icon name="back" /> All projects</Link>} />
      </WorkspaceFrame>} />
    </Routes>
  )
}

function ProjectPicker({ projects }: { projects: { data?: ProjectList; isSuccess: boolean; isPending: boolean; error: Error | null } }) {
  const client = useQueryClient()
  const navigate = useNavigate()
  const [creating, setCreating] = useState(false)
  const create = useMutation({
    mutationFn: (fields: { name: string; slug: string }) => readJson<Project>(api, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(fields),
    }),
    onSuccess: async (project) => {
      await client.invalidateQueries({ queryKey: projectsKey })
      void navigate(`/projects/${project.id}/features`)
    },
  })

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const fields = new FormData(event.currentTarget)
    create.mutate({ name: String(fields.get('name') ?? ''), slug: String(fields.get('slug') ?? '') })
  }

  return (
    <>
      <PageIntro eyebrow="Workspace / Projects" title="Project" accent="workspace" description="Organize work by feature, record decisions, and see which sources are available." hero
        action={projects.isSuccess ? <button type="button" className="button button-primary" aria-haspopup="dialog" onClick={(event) => {
          event.currentTarget.focus()
          create.reset()
          setCreating(true)
        }}><Icon name="plus" /> New project</button> : undefined} />
      <WorkspaceColumns context={<><SourceContext /><ContextCard title="Where data lives" icon="database"><p>Projects, features and decisions are saved in the configured PostgreSQL database. Creating a project does not contact Azure DevOps or Figma.</p></ContextCard></>}>
        <section className="panel" aria-labelledby="local-projects-title">
          <div className="panel-heading"><span className="icon-tile"><Icon name="grid" /></span><div><h2 id="local-projects-title">Local projects</h2><p>Select a project to open its workspace.</p></div></div>
          {projects.isPending ? <p role="status">Loading projects…</p> : null}
          {projects.error ? <p className="notice error" role="alert">{projects.error.message}</p> : null}
          {projects.isSuccess && !projects.data?.items.length ? <EmptyState title="No projects yet" icon="grid">Create your first project to start organizing local work.</EmptyState> : null}
          {projects.isSuccess && projects.data?.items.length ? <ul className="entity-list" aria-label="Local projects">
            {projects.data.items.map((project) => <li key={project.id}>
              <Link className="entity-link" to={`/projects/${project.id}/features`}><span className="icon-tile"><Icon name="layers" /></span><span className="entity-label"><strong>{project.name}</strong><small>{project.slug}</small></span><Icon name="chevron" /></Link>
            </li>)}
          </ul> : null}
        </section>
      </WorkspaceColumns>
      {creating ? <CreateDialog title="Create a project" description="Give the workspace a name and a short, unique slug."
        submitLabel="Create project" pendingLabel="Creating project…" pending={create.isPending} error={create.error}
        onSubmit={submit} onDismiss={() => setCreating(false)}>
        <div className="field-grid">
          <div className="form-field"><label htmlFor="project-name">Project name</label><input id="project-name" name="name" required maxLength={160} /></div>
          <div className="form-field"><label htmlFor="project-slug">Project slug</label><input id="project-slug" name="slug" required maxLength={80} placeholder="e.g. dishrate" /></div>
        </div>
      </CreateDialog> : null}
    </>
  )
}

function ProjectView({ frame }: { frame: { health: WorkspaceHealth; projects?: ProjectOption[]; projectsLoading: boolean } }) {
  const { projectId, featureId, section } = useParams()
  const project = useQuery({
    queryKey: ['project', projectId],
    queryFn: ({ signal }) => readJson<Project>(`${api}/${encodeURIComponent(projectId ?? '')}`, { signal }),
  })
  if (!project.isSuccess) return <WorkspaceFrame {...frame}>
    <PageIntro eyebrow="Workspace / Projects" title={project.isPending ? 'Opening project…' : 'Project unavailable'} description="Only projects stored in the local workspace can be opened here." />
    {project.isError ? <p className="notice error" role="alert">{project.error.message} <Link to="/projects">Back to projects</Link></p> : <p role="status">Loading project…</p>}
  </WorkspaceFrame>

  const base = `/projects/${project.data.id}`
  const current = featureId ? 'features' : section
  if (!current) return <Navigate to={`${base}/features`} replace />
  return (
    <WorkspaceFrame {...frame} project={project.data}>
      {current === 'features' ? <FeatureWorkspace projectId={project.data.id} featureId={featureId} /> : null}
      {current === 'sync' ? <>
        <PageIntro eyebrow="Project / Activity" title="Sync" accent="history" description="Review synchronization attempts recorded for this project." />
        <WorkspaceColumns context={<><SourceContext connectionPath={`${base}/connections`} /><ContextCard title="About this history" icon="history"><p>This view reads the local attempt journal. Opening it does not start a provider operation.</p></ContextCard></>}>
          <section className="panel" aria-labelledby="sync-attempts-title"><div className="panel-heading"><span className="icon-tile"><Icon name="history" /></span><div><h2 id="sync-attempts-title">Synchronization attempts</h2><p>Run status and timestamps, from the local database.</p></div></div><SyncHistory projectId={project.data.id} /></section>
        </WorkspaceColumns>
      </> : null}
      {current === 'connections' ? <>
        <PageIntro eyebrow="Project / Sources" title="MCP" accent="connections" description="Use the global MCP profiles or override them for this project. Save resource links separately." />
        <WorkspaceColumns context={<ContextCard title="Links and provider access" icon="info"><p>Connecting to MCP does not verify access to every resource. Save a work item or frame link and use Check source to verify it without importing content.</p><Link className="text-link" to="/settings/mcp">Global MCP settings <Icon name="arrow" /></Link></ContextCard>}>
          <McpProfilesPanel projectId={project.data.id} />
          <h2>Project source links</h2>
          <ProviderConnections projectId={project.data.id} />
        </WorkspaceColumns>
      </> : null}
      {!['features', 'sync', 'connections'].includes(current) ? <PageIntro eyebrow="Project" title="Section unavailable" description="Use the workspace menu to continue." /> : null}
    </WorkspaceFrame>
  )
}
