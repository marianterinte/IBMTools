// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md and 11_API_AND_EVENTS.md.
// Routes for viewing and creating actual local projects, not provider fixtures.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent } from 'react'
import { Link, NavLink, Route, Routes, useNavigate, useParams } from 'react-router'
import { SyncHistory } from './SyncHistory'

type Project = { id: string; slug: string; name: string; version: number }
type ProjectList = { items: Project[] }
const api = '/api/v1/projects'
const projectsKey = ['operator-projects'] as const

/** Checks application status before deserializing the corresponding local project response. */
async function readJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { cache: 'no-store', ...init })
  if (response.status === 401) throw new Error('Local session is locked. Reload to check pairing.')
  if (response.status === 404) throw new Error('Project unavailable.')
  if (response.status === 409) throw new Error('A project already uses that slug.')
  if (response.status === 400) throw new Error('Check the project name and slug.')
  if (!response.ok) throw new Error('Local project data is unavailable. Check the database setup.')
  return response.json()
}

/** Defines navigable project routes, mounted only for an authenticated local session. */
export function ProjectWorkspace({ csrfToken }: { csrfToken: string }) {
  return (
    <section className="setup" aria-labelledby="projects-title">
      <h2 id="projects-title">Project workspace</h2>
      <Routes>
        <Route path="/" element={<ProjectPicker csrfToken={csrfToken} />} />
        <Route path="/projects" element={<ProjectPicker csrfToken={csrfToken} />} />
        <Route path="/projects/:projectId/:section?" element={<ProjectView />} />
        <Route path="*" element={<p>Page unavailable. <Link to="/projects">Projects</Link></p>} />
      </Routes>
    </section>
  )
}

/** Lists persisted projects and provides a minimal create form. */
function ProjectPicker({ csrfToken }: { csrfToken: string }) {
  const client = useQueryClient()
  const navigate = useNavigate()
  const projects = useQuery({ queryKey: projectsKey, queryFn: ({ signal }) => readJson<ProjectList>(api, { signal }) })
  const create = useMutation({
    mutationFn: (fields: { name: string; slug: string }) => readJson<Project>(api, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-DevPrism-CSRF': csrfToken },
      body: JSON.stringify(fields),
    }),
    onSuccess: async (project) => {
      await client.invalidateQueries({ queryKey: projectsKey })
      void navigate(`/projects/${project.id}/features`)
    },
  })

  /** Submits the explicit local project fields, never an owner or provider identity. */
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const fields = new FormData(event.currentTarget)
    create.mutate({ name: String(fields.get('name') ?? ''), slug: String(fields.get('slug') ?? '') })
  }

  return (
    <>
      {projects.isPending ? <p role="status">Loading projects…</p> : null}
      {projects.isError ? <p role="alert">{projects.error.message}</p> : null}
      {projects.isSuccess ? (
        <>
          {projects.data.items.length ? (
            <ul>{projects.data.items.map((p) => <li key={p.id}><Link to={`/projects/${p.id}/features`}>{p.name}</Link></li>)}</ul>
          ) : <p>No local projects yet.</p>}
          <form onSubmit={submit}>
            <div className="pairing-field"><label htmlFor="project-name">Project name</label><input id="project-name" name="name" required maxLength={160} /></div>
            <div className="pairing-field"><label htmlFor="project-slug">Project slug</label><input id="project-slug" name="slug" required maxLength={80} placeholder="dishrate" /></div>
            <button type="submit" disabled={create.isPending}>Create project</button>
            {create.isError ? <p role="alert">{create.error.message}</p> : null}
          </form>
        </>
      ) : null}
    </>
  )
}

const descriptions = {
  features: 'Feature Explorer is not available in this build.',
  connections: 'No provider connection has been verified through this application.',
}

/** Opens one authorized project and explicitly labels sections not yet implemented. */
function ProjectView() {
  const { projectId, section = 'features' } = useParams()
  const project = useQuery({
    queryKey: ['project', projectId],
    queryFn: ({ signal }) => readJson<Project>(`${api}/${encodeURIComponent(projectId ?? '')}`, { signal }),
  })
  if (project.isPending) return <p role="status">Loading project…</p>
  if (project.isError) return <p role="alert">{project.error.message} <Link to="/projects">Projects</Link></p>

  const base = `/projects/${project.data.id}`
  return (
    <>
      <p><Link to="/projects">All projects</Link></p>
      <h3>{project.data.name}</h3>
      <nav className="workspace-nav" aria-label="Project">
        <NavLink to={`${base}/features`}>Features</NavLink>
        <NavLink to={`${base}/connections`}>Connections</NavLink>
        <NavLink to={`${base}/sync`}>Sync history</NavLink>
      </nav>
      {section === 'sync' ? <SyncHistory projectId={project.data.id} /> : (
        <p>{Object.hasOwn(descriptions, section) ? descriptions[section as keyof typeof descriptions] : 'Project section unavailable.'}</p>
      )}
    </>
  )
}
