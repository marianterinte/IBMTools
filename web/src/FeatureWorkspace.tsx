// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md (local Feature Explorer).
// Uses local artifacts only. Provider content appears here only after an actual import.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Link, useNavigate } from 'react-router'

type Feature = { id: string; key: string; name: string; description: string; version: number }
type FeatureList = { items: Feature[]; truncated: boolean }

/** Reads the feature API without treating failures as empty project content. */
async function requestFeature<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { cache: 'no-store', ...init })
  if (response.status === 401) throw new Error('Local session is locked. Reload to check pairing.')
  if (response.status === 404) throw new Error('Feature unavailable.')
  if (response.status === 409) throw new Error('A feature already uses that key.')
  if (response.status === 400) throw new Error('Check the feature key, name and description.')
  if (!response.ok) throw new Error('Local feature data is unavailable.')
  return response.json()
}

/** Lists, searches and creates local features, or opens the feature named in the route. */
export function FeatureWorkspace({ projectId, featureId, csrfToken }: {
  projectId: string; featureId?: string; csrfToken: string
}) {
  const page = `/projects/${projectId}/features`
  const api = `/api/v1${page}`
  return featureId
    ? <FeatureDetail projectId={projectId} featureId={featureId} page={page} api={api} />
    : <FeatureListView key={projectId} projectId={projectId} page={page} api={api} csrfToken={csrfToken} />
}

/** Lists the requested page of features and accepts an explicit local create request. */
function FeatureListView({ projectId, page, api, csrfToken }: {
  projectId: string; page: string; api: string; csrfToken: string
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
      headers: { 'Content-Type': 'application/json', 'X-DevPrism-CSRF': csrfToken },
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

  return (
    <section aria-labelledby="feature-list-title">
      <h4 id="feature-list-title">Features</h4>
      <form onSubmit={find}>
        <div className="pairing-field"><label htmlFor="feature-search">Search by name or key</label><input id="feature-search" name="search" maxLength={120} /></div>
        <button type="submit">Search</button>
      </form>
      {features.isPending ? <p role="status">Loading features…</p> : null}
      {features.isError ? <p role="alert">{features.error.message}</p> : null}
      {features.isSuccess && features.data.items.length === 0 ? <p>{search ? 'No matching features.' : 'No local features yet.'}</p> : null}
      {features.isSuccess && features.data.items.length > 0 ? (
        <ul>{features.data.items.map((feature) => (
          <li key={feature.id}><Link to={`${page}/${feature.id}`}>{feature.name}</Link> · {feature.key}</li>
        ))}</ul>
      ) : null}
      {features.data?.truncated ? <p>Showing the first 100 matches. Narrow the search for more specific results.</p> : null}
      <h4>Create a feature</h4>
      <form onSubmit={submit}>
        <div className="pairing-field"><label htmlFor="feature-name">Name</label><input id="feature-name" name="name" required maxLength={160} /></div>
        <div className="pairing-field"><label htmlFor="feature-key">Key</label><input id="feature-key" name="key" required maxLength={80} placeholder="dish-rating" /></div>
        <div className="pairing-field"><label htmlFor="feature-description">Description (optional)</label><textarea id="feature-description" name="description" maxLength={2000} /></div>
        <button type="submit" disabled={create.isPending}>Create feature</button>
        {create.isError ? <p role="alert">{create.error.message}</p> : null}
      </form>
    </section>
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
  return (
    <section aria-labelledby="feature-detail-title">
      <p><Link to={page}>All features</Link></p>
      <h4 id="feature-detail-title">{feature.data?.name ?? 'Feature details'}</h4>
      {feature.isPending ? <p role="status">Loading feature…</p> : null}
      {feature.isError ? <p role="alert">{feature.error.message}</p> : null}
      {feature.isSuccess ? (
        <>
          <p>{feature.data.key} · Local revision {feature.data.version}</p>
          {feature.data.description ? <p>{feature.data.description}</p> : null}
          <p>Remote source coverage is not yet verified. No imported artifact is implied by this feature.</p>
        </>
      ) : null}
    </section>
  )
}
