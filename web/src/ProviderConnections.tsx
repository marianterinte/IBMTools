import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { Icon } from './WorkspaceUi'
import { McpReadCheck } from './McpSettings'

const providers = [
  {
    id: 'AzureDevOpsBoards',
    title: 'Azure DevOps',
    area: 'Boards work items',
    icon: 'boards',
    hint: 'Paste a project or a specific Boards work item link.',
    placeholder: 'https://dev.azure.com/organization/project/_workitems/edit/123',
  },
  {
    id: 'Figma',
    title: 'Figma',
    area: 'Design file or frame',
    icon: 'design',
    hint: 'Paste a design file link, or copy a link to a specific frame to select that frame.',
    placeholder: 'https://www.figma.com/design/ExampleFileKey/Design?node-id=1-2',
  },
] as const

type Provider = (typeof providers)[number]
type Selection = { id: string; provider: Provider['id']; sourceUrl: string }
type SelectionList = { items: Selection[]; truncated: boolean }

async function request(path: string, init?: RequestInit) {
  const response = await fetch(path, { cache: 'no-store', ...init })
  if (response.status === 400) throw new Error('Enter a supported HTTPS link for this provider.')
  if (response.status === 404) throw new Error('This project or source link is no longer available. Refresh the page to see the latest list.')
  if (response.status === 409) throw new Error('This link conflicts with existing connection data.')
  if (!response.ok) throw new Error('Unable to access saved source links. Try again.')
  return response
}

export function ProviderConnections({ projectId }: { projectId: string }) {
  const api = `/api/v1/projects/${encodeURIComponent(projectId)}/source-selections`
  const queryKey = ['project', projectId, 'source-selections'] as const
  const client = useQueryClient()
  const selections = useQuery({
    queryKey,
    queryFn: async ({ signal }): Promise<SelectionList> => (await request(api, { signal })).json(),
  })
  const refresh = () => client.invalidateQueries({ queryKey })

  return <>
    {selections.isPending ? <p role="status">Loading saved links…</p> : null}
    {selections.isError ? <p className="notice error" role="alert">{selections.error.message}</p> : null}
    {selections.data?.truncated ? <p className="notice">Showing the first 100 saved links for this project.</p> : null}
    {providers.map((provider) => (
      <ProviderLinkForm key={`${projectId}:${provider.id}`} provider={provider} projectId={projectId} api={api} refresh={refresh}
        links={selections.data?.items.filter((item) => item.provider === provider.id) ?? []} loaded={selections.isSuccess} />
    ))}
  </>
}

function ProviderLinkForm({ provider, projectId, api, refresh, links, loaded }: {
  provider: Provider; projectId: string; api: string; refresh: () => Promise<void>; links: Selection[]; loaded: boolean
}) {
  const fieldId = useId()
  const [status, setStatus] = useState('')
  const [url, setUrl] = useState('')
  const change = useMutation({
    mutationFn: async (operation: { url: string } | { remove: string }) => {
      if ('remove' in operation) {
        await request(`${api}/${encodeURIComponent(operation.remove)}`, { method: 'DELETE' })
        return
      }
      await request(api, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ provider: provider.id, sourceUrl: operation.url }),
      })
    },
    onSuccess: async (_, operation) => {
      if ('url' in operation) setUrl('')
      await refresh()
      setStatus('remove' in operation ? 'Link removed.' : 'Link saved.')
    },
  })

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (change.isPending || !url.trim()) return
    setStatus('')
    change.mutate({ url: url.trim() })
  }

  return <section className="panel" aria-labelledby={`${fieldId}-title`}>
    <div className="panel-heading">
      <span className="icon-tile"><Icon name={provider.icon} /></span>
      <div><h2 id={`${fieldId}-title`}>{provider.title}</h2><p>{provider.area}</p></div>
    </div>
    <form onSubmit={save}>
      <div className="form-field">
        <label htmlFor={fieldId}>{provider.title} URL</label>
        <input id={fieldId} type="url" value={url} onChange={(event) => setUrl(event.target.value)} required maxLength={2048}
          placeholder={provider.placeholder} aria-describedby={`${fieldId}-hint`} disabled={change.isPending} autoComplete="off" spellCheck={false} />
        <small id={`${fieldId}-hint`} className="secondary">{provider.hint}</small>
      </div>
      <button className="button button-primary" type="submit" disabled={change.isPending}>
        {change.isPending && change.variables && 'url' in change.variables ? 'Saving link…' : 'Save link'}
      </button>
    </form>
    {change.isError ? <p className="notice error" role="alert">{change.error.message}</p> : null}
    {status ? <p className="notice" role="status">{status}</p> : null}
    {links.length ? <ul className="compact-list saved-source-links" aria-label={`Saved ${provider.title} links`}>
      {links.map(({ id, sourceUrl }) => <li key={id}>
        <a className="text-link" href={sourceUrl} target="_blank" rel="noopener noreferrer">{sourceUrl}</a>
        <McpReadCheck projectId={projectId} provider={provider.id} selectionId={id} />
        <button className="button button-secondary" type="button" disabled={change.isPending}
          aria-label={`Remove link ${sourceUrl}`} onClick={() => { setStatus(''); change.mutate({ remove: id }) }}>
          {change.isPending && change.variables && 'remove' in change.variables && change.variables.remove === id ? 'Removing…' : 'Remove'}
        </button>
      </li>)}
    </ul> : loaded ? <p className="notice">No links saved for this provider yet.</p> : null}
  </section>
}
