// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md (local Feature Explorer).
// Uses local artifacts only. Provider content appears here only after an actual import.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { DecisionHighlights, DecisionMemory } from './DecisionMemory'
import { Badge, ContextCard, EmptyState, Icon, PageIntro, SourceContext, WorkspaceColumns } from './WorkspaceUi'

type Feature = { id: string; key: string; name: string; description: string; version: number }
type FeatureList = { items: Feature[]; truncated: boolean }

/** Reads the feature API without treating failures as empty project content. */
async function requestFeature<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { cache: 'no-store', ...init })
  if (response.status === 404) throw new Error('Feature unavailable.')
  if (response.status === 409) throw new Error('A feature already uses that key.')
  if (response.status === 400) throw new Error('Check the feature key, name and description.')
  if (!response.ok) throw new Error('Local feature data is unavailable.')
  return response.json()
}

/** Lists, searches and creates local features, or opens the feature named in the route. */
export function FeatureWorkspace({ projectId, featureId }: {
  projectId: string; featureId?: string
}) {
  const page = `/projects/${projectId}/features`
  const api = `/api/v1${page}`
  return featureId
    ? <FeatureDetail projectId={projectId} featureId={featureId} page={page} api={api} />
    : <FeatureListView key={projectId} projectId={projectId} page={page} api={api} />
}

/** Lists the requested page of features and accepts an explicit local create request. */
function FeatureListView({ projectId, page, api }: {
  projectId: string; page: string; api: string
}) {
  const navigate = useNavigate()
  const client = useQueryClient()
  const [search, setSearch] = useState('')
  const key = ['project', projectId, 'features'] as const
  const features = useQuery({
    queryKey: [...key, search],
    queryFn: ({ signal }) => requestFeature<FeatureList>(`${api}?${new URLSearchParams({ search })}`, { signal }),
  })
  const create = useMutation({
    mutationFn: (data: { key: string; name: string; description: string }) => requestFeature<Feature>(api, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }),
    onSuccess: async (feature) => {
      await client.invalidateQueries({ queryKey: key })
      void navigate(`${page}/${feature.id}`)
    },
  })

  /** Searches only on form submission, avoiding requests for incomplete input. */
  function find(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSearch(String(new FormData(event.currentTarget).get('search') ?? '').trim())
  }

  /** Submits the explicit feature fields within the current project. */
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    create.mutate({
      key: String(data.get('key') ?? ''),
      name: String(data.get('name') ?? ''),
      description: String(data.get('description') ?? ''),
    })
  }

  const base = `/projects/${projectId}`
  const count = features.data?.items.length ?? 0
  const countLabel = search ? (count === 1 ? 'match' : 'matches') : (count === 1 ? 'feature' : 'features')
  return (
    <>
      <PageIntro eyebrow="Project / Features" title="Feature" accent="explorer" description="Keep local features and the decisions behind them in one place." hero
        action={<a className="button button-primary" href="#new-feature"><Icon name="plus" /> New feature</a>} />
      <WorkspaceColumns context={<>
        <SourceContext connectionPath={`${base}/connections`} />
        <ContextCard title="Decision memory" icon="book"><p>Open a feature to draft an operator decision, review its exact wording, and approve it locally.</p></ContextCard>
        <ContextCard title="Project activity" icon="history"><p>Review any attempts recorded in this project's local sync journal.</p><Link className="text-link" to={`${base}/sync`}>View sync history <Icon name="arrow" /></Link></ContextCard>
      </>}>
        <section className="panel" aria-labelledby="feature-list-title">
          <div className="panel-heading"><span className="icon-tile"><Icon name="layers" /></span><div><h2 id="feature-list-title">Local features</h2><p>Search the features saved for this project.</p></div>
            {features.isSuccess ? <Badge tone="accent">{count}{features.data.truncated ? '+' : ''} {countLabel}</Badge> : null}
          </div>
          <form className="search-form" role="search" aria-label="Find local features" onSubmit={find}>
            <div className="search-input"><Icon name="search" /><label className="visually-hidden" htmlFor="feature-search">Search by name or key</label><input id="feature-search" type="search" name="search" maxLength={120} placeholder="Search by name or key" /></div>
            <button className="button button-secondary" type="submit">Search</button>
          </form>
          {features.isPending ? <p role="status">Loading features…</p> : null}
          {features.isError ? <p className="notice error" role="alert">{features.error.message}</p> : null}
          {features.isSuccess && features.data.items.length === 0 ? <EmptyState title={search ? 'No matching features' : 'No local features yet'}>{search ? 'Try a different name or key.' : 'Create a feature below to start recording its decisions.'}</EmptyState> : null}
          {features.isSuccess && features.data.items.length > 0 ? <ul className="entity-list" aria-label="Local features">
            {features.data.items.map((feature) => <li key={feature.id}>
              <Link className="entity-link" to={`${page}/${feature.id}`}><span className="icon-tile"><Icon name="layers" /></span><span className="entity-label"><strong>{feature.name}</strong><small>{feature.key}</small></span><Icon name="chevron" /></Link>
            </li>)}
          </ul> : null}
          {features.data?.truncated ? <p className="notice">Showing the first 100 matches. Narrow your search for more specific results.</p> : null}
        </section>
        <section className="panel" id="new-feature" tabIndex={-1} aria-labelledby="new-feature-title">
          <div className="panel-heading"><span className="icon-tile"><Icon name="plus" /></span><div><h2 id="new-feature-title">Create a feature</h2><p>A local record; no provider operation is triggered.</p></div></div>
          <form onSubmit={submit}>
            <div className="field-grid">
              <div className="form-field"><label htmlFor="feature-name">Name</label><input id="feature-name" name="name" required maxLength={160} /></div>
              <div className="form-field"><label htmlFor="feature-key">Key</label><input id="feature-key" name="key" required maxLength={80} placeholder="e.g. dish-rating" /></div>
            </div>
            <div className="form-field"><label htmlFor="feature-description">Description <span>(optional)</span></label><textarea id="feature-description" name="description" maxLength={2000} rows={3} /></div>
            <button type="submit" className="button button-primary" disabled={create.isPending}><Icon name="plus" />{create.isPending ? 'Creating feature…' : 'Create feature'}</button>
            {create.isError ? <p className="notice error" role="alert">{create.error.message}</p> : null}
          </form>
        </section>
      </WorkspaceColumns>
    </>
  )
}

/** Opens a persisted feature and distinguishes missing import support from an empty source. */
function FeatureDetail({ projectId, featureId, page, api }: {
  projectId: string; featureId: string; page: string; api: string
}) {
  const feature = useQuery({
    queryKey: ['project', projectId, 'features', 'detail', featureId],
    queryFn: ({ signal }) => requestFeature<Feature>(`${api}/${encodeURIComponent(featureId)}`, { signal }),
  })
  const featureData = feature.isSuccess ? feature.data : undefined
  return (
    <>
      <PageIntro eyebrow="Project / Features / Detail" title={featureData?.name ?? 'Feature details'} description="A local feature and its versioned decision memory."
        action={<Link className="button button-secondary" to={page}><Icon name="back" /> All features</Link>} />
      <WorkspaceColumns context={<>
        {featureData ? <>
          <ContextCard title="Feature record" icon="layers"><dl className="record-fields"><div><dt>Key</dt><dd>{featureData.key}</dd></div><div><dt>Local revision</dt><dd>{featureData.version}</dd></div></dl></ContextCard>
          <DecisionHighlights projectId={projectId} featureId={featureId} />
        </> : null}
        <SourceContext connectionPath={`/projects/${projectId}/connections`} />
      </>}>
        {feature.isPending ? <p className="panel" role="status">Loading feature…</p> : null}
        {feature.isError ? <p className="notice error" role="alert">{feature.error.message}</p> : null}
        {featureData ? <>
          <section className="panel" aria-labelledby="feature-overview-title"><div className="panel-heading"><span className="icon-tile"><Icon name="layers" /></span><div><h2 id="feature-overview-title">Feature overview</h2><p>Stored in this workspace only.</p></div><Badge tone="accent">Local record</Badge></div>
            <p className="record-text">{featureData.description || 'No description has been recorded.'}</p>
          </section>
          <DecisionMemory key={featureId} projectId={projectId} featureId={featureId} />
        </> : null}
      </WorkspaceColumns>
    </>
  )
}
