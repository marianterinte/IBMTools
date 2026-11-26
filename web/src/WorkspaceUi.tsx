import { useId, useLayoutEffect, useRef, type FormEvent, type ReactNode } from 'react'
import { Link, NavLink, useNavigate } from 'react-router'
import { ThemeToggle } from './ThemeToggle'

const icons = {
  prism: 'M12 2 3 7v10l9 5 9-5V7L12 2Z M3 7l9 5 9-5 M12 12v10 M7.5 4.5l9 5v10',
  grid: 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
  layers: 'm12 3 9 5-9 5-9-5 9-5Z M3 12l9 5 9-5 M3 16l9 5 9-5',
  plug: 'M9 3v5m6-5v5M7 8h10v3a5 5 0 0 1-10 0V8Zm5 8v5',
  history: 'M3 12a9 9 0 1 1 2.6 6.4M3 5v7h7m2-5v5l3 2',
  database: 'M4 6c0 1.7 3.6 3 8 3s8-1.3 8-3-3.6-3-8-3-8 1.3-8 3Zm0 0v12c0 1.7 3.6 3 8 3s8-1.3 8-3V6M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3',
  plus: 'M12 5v14M5 12h14',
  arrow: 'M5 12h14m-6-6 6 6-6 6',
  back: 'M19 12H5m6-6-6 6 6 6',
  search: 'M20 20l-4.5-4.5M17 11a6 6 0 1 1-12 0 6 6 0 0 1 12 0Z',
  book: 'M12 6v14M3 4c4-1 7 0 9 2 2-2 5-3 9-2v14c-4-1-7 0-9 2-2-2-5-3-9-2V4Z',
  boards: 'M4 6l7-3v18l-7-3V6Zm7-3 9 4v10l-9 4m3-13h3m-3 4h3',
  design: 'M12 3H8a3 3 0 0 0 0 6h4m0-6h4a3 3 0 0 1 0 6h-4m0-6v12m0-6H8a3 3 0 0 0 0 6h4m0-3a3 3 0 1 1 6 0 3 3 0 0 1-6 0Zm0 3v3a3 3 0 1 1-3-3h3Z',
  info: 'M12 11v5m0-8h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
  refresh: 'M20 7v5h-5M4 17v-5h5m-4-3a8 8 0 0 1 13-3l2 6M4 12l2 6a8 8 0 0 0 13-3',
  chevron: 'm9 18 6-6-6-6',
} as const

type IconName = keyof typeof icons
export function Icon({ name }: { name: IconName }) {
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round"><path d={icons[name]} /></svg>
}

export type ProjectOption = { id: string; slug: string; name: string }
export type WorkspaceHealth = { label: string; tone: 'good' | 'warning' | 'neutral'; checking: boolean; refresh: () => void }

const sections = [
  ['features', 'Features', 'layers'],
  ['connections', 'Connections', 'plug'],
  ['sync', 'Sync history', 'history'],
] as const

export function WorkspaceFrame({ health, projects = [], project, projectsLoading, children }: {
  health: WorkspaceHealth; projects?: ProjectOption[]; project?: ProjectOption; projectsLoading?: boolean; children: ReactNode
}) {
  const navigate = useNavigate()
  const options = project && !projects.some((option) => option.id === project.id) ? [project, ...projects] : projects
  return (
    <div className="workspace-shell">
      <a className="skip-link" href="#workspace-main">Skip to workspace</a>
      <aside className="sidebar" aria-label="Workspace sidebar">
        <Link className="brand" to="/projects">
          <span className="brand-mark"><Icon name="prism" /></span>
          <span>DEV PRISM<span className="brand-ai"> AI</span><small>Project workspace</small></span>
        </Link>
        <div className="project-switch">
          <label className="eyebrow" htmlFor="project-switch">Project</label>
          <div className="select-wrap"><Icon name="grid" />
            <select id="project-switch" value={project?.id ?? ''} onChange={(event) => {
              const id = event.target.value
              void navigate(id ? `/projects/${encodeURIComponent(id)}/features` : '/projects')
            }}>
              <option value="">{projectsLoading ? 'Loading projects…' : 'All projects'}</option>
              {options.map((option) => <option key={option.id} value={option.id}>{option.name}</option>)}
            </select>
          </div>
        </div>
        <nav aria-label="Workspace" className="workspace-nav">
          <span className="eyebrow nav-label">Workspace</span>
          <NavLink to="/projects" end className={!project ? 'nav-current' : undefined} aria-current={project ? undefined : 'page'}><Icon name="grid" /> Projects</NavLink>
          {project ? sections.map(([section, label, icon]) => (
            <NavLink key={section} to={`/projects/${project.id}/${section}`}><Icon name={icon} />{label}</NavLink>
          )) : null}
          <NavLink to="/settings/mcp"><Icon name="plug" /> MCP settings</NavLink>
        </nav>
        <div className="sidebar-footer"><span className="marker" aria-hidden="true" /> <span>Local workspace<small>Single operator</small></span></div>
      </aside>
      <div className="workspace-body">
        <header className="topbar">
          <div className="topbar-title">
            <span className="icon-tile"><Icon name={project ? 'layers' : 'grid'} /></span>
            <div><strong>{project?.name ?? 'Local workspace'}</strong><small>{project?.slug ?? 'DEV PRISM AI'}</small></div>
          </div>
          <div className="topbar-status">
            {project ? <Link className="source-short" to={`/projects/${project.id}/connections`} aria-label="View Azure DevOps and Figma availability"><Icon name="plug" /><span>Sources</span></Link> : null}
            <ThemeToggle />
            <span role="status"><Badge tone={health.tone}>{health.label}</Badge></span>
            <button className="icon-button" type="button" aria-label="Check database status" title="Check database status" disabled={health.checking} onClick={health.refresh}><Icon name="refresh" /></button>
          </div>
        </header>
        <main id="workspace-main" tabIndex={-1} className="workspace-main">{children}</main>
      </div>
    </div>
  )
}

export function WorkspaceColumns({ children, context }: { children: ReactNode; context: ReactNode }) {
  return <div className="workspace-columns"><div className="workspace-content">{children}</div><aside className="context-rail" aria-label="Workspace context">{context}</aside></div>
}

export function PageIntro({ eyebrow, title, accent, description, action, hero }: {
  eyebrow: string; title: string; accent?: string; description: string; action?: ReactNode; hero?: boolean
}) {
  return (
    <header className={hero ? 'page-intro hero' : 'page-intro'}>
      <div className="intro-copy">
        <p className="eyebrow">{eyebrow}</p>
        <h1>{title}{accent ? <> <span>{accent}</span></> : null}</h1>
        <p className="intro-description">{description}</p>
      </div>
      {action ? <div className="intro-action">{action}</div> : null}
      {hero ? <svg className="hero-art" viewBox="0 0 320 230" aria-hidden="true" fill="none">
        <defs><linearGradient id="prism-line" x1="25" y1="215" x2="245" y2="20" gradientUnits="userSpaceOnUse"><stop stopColor="#574781" stopOpacity=".12"/><stop offset="1" stopColor="#aa82ff" stopOpacity=".75"/></linearGradient></defs>
        <path stroke="url(#prism-line)" d="M4 182 166 89l151 87M4 204l162-93 151 87M53 218l113-65 113 65m-177 0 64-37 64 37M53 158l102 59m-55-86 148 86m-49-109L60 189m188-54-143 83m192-54-94 54M102 68l64-37 64 37v74l-64 37-64-37V68ZM102 68l64 37 64-37m-64 37v74m-32-130 64 37v75m-32-56 64 37m-128 0 64-37"/>
        <path fill="#8860ef" fillOpacity=".12" d="m102 68 64 37v74l-64-37Z"/>
        <path fill="#bb9cff" fillOpacity=".13" d="m166 31 64 37-64 37-64-37Z"/>
        <circle cx="230" cy="68" r="3" fill="#be9dff"/><circle cx="102" cy="142" r="2.5" fill="#9677ff"/><circle cx="166" cy="105" r="3" fill="#be9dff"/>
      </svg> : null}
    </header>
  )
}

/** A mounted form opens modally and uses the browser's focus and background isolation. */
export function CreateDialog({ title, description, submitLabel, pendingLabel, pending, error, onSubmit, onDismiss, children }: {
  title: string; description: string; submitLabel: string; pendingLabel: string; pending: boolean; error: Error | null
  onSubmit: (event: FormEvent<HTMLFormElement>) => void; onDismiss: () => void; children: ReactNode
}) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const titleId = useId()
  const descriptionId = useId()

  useLayoutEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    dialog.showModal()
    return () => dialog.close()
  }, [])

  return (
    <dialog ref={dialogRef} className="panel create-dialog" aria-labelledby={titleId} aria-describedby={descriptionId}
      onCancel={(event) => {
        event.preventDefault()
        if (!pending) onDismiss()
      }}>
      <div className="panel-heading">
        <span className="icon-tile"><Icon name="plus" /></span>
        <div><h2 id={titleId}>{title}</h2><p id={descriptionId}>{description}</p></div>
      </div>
      <form onSubmit={(event) => {
        if (pending) event.preventDefault()
        else onSubmit(event)
      }}>
        {children}
        {error ? <p className="notice error" role="alert">{error.message}</p> : null}
        <div className="create-dialog-actions">
          <button type="button" className="button button-secondary" disabled={pending} onClick={onDismiss}>Cancel</button>
          <button type="submit" className="button button-primary" disabled={pending}><Icon name="plus" />{pending ? pendingLabel : submitLabel}</button>
        </div>
      </form>
    </dialog>
  )
}

export function Badge({ children, tone = 'neutral' }: { children: ReactNode; tone?: 'good' | 'warning' | 'neutral' | 'accent' }) {
  return <span className={`badge badge-${tone}`}><span aria-hidden="true" />{children}</span>
}

export function ContextCard({ title, icon, children }: { title: string; icon: IconName; children: ReactNode }) {
  return <section className="panel context-card"><h2><Icon name={icon} />{title}</h2>{children}</section>
}

export function EmptyState({ title, children, icon = 'layers' }: { title: string; children: ReactNode; icon?: IconName }) {
  return <div className="empty-state"><span className="icon-tile large"><Icon name={icon} /></span><strong>{title}</strong><p>{children}</p></div>
}

const sources = [
  { name: 'Azure DevOps', area: 'Boards work items', icon: 'boards' },
  { name: 'Figma', area: 'Design frame context', icon: 'design' },
] as const

export function SourceContext({ connectionPath }: { connectionPath?: string }) {
  return (
    <ContextCard title="Source availability" icon="plug">
      <div className="source-list">
        {sources.map((source) => <div className="source-row" key={source.name}>
          <span className="icon-tile"><Icon name={source.icon} /></span>
          <div><strong>{source.name}</strong><small>{source.area}</small></div>
        </div>)}
      </div>
      <p>Check the connection and individual source links manually. Saving a link does not import content.</p>
      <Link className="text-link" to={connectionPath ?? '/settings/mcp'}>{connectionPath ? 'View connections' : 'MCP settings'} <Icon name="arrow" /></Link>
    </ContextCard>
  )
}
