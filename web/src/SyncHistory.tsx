// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md (sync history).
import { useQuery } from '@tanstack/react-query'

type SyncRun = {
  id: string
  operation: 'Probe' | 'Import'
  state: 'Queued' | 'Running' | 'Succeeded' | 'Partial' | 'Failed' | 'Interrupted' | 'Cancelled'
  requestedAt: string
  reasonCode: string | null
}

/** Browses the local attempt journal without starting any provider operation. */
export function SyncHistory({ projectId }: { projectId: string }) {
  const history = useQuery({
    queryKey: ['project', projectId, 'sync-runs'],
    queryFn: async ({ signal }): Promise<{ items: SyncRun[] }> => {
      const response = await fetch(`/api/v1/projects/${encodeURIComponent(projectId)}/sync-runs`, { signal, cache: 'no-store' })
      if (!response.ok) throw new Error('Local sync history is unavailable.')
      return response.json()
    },
    refetchInterval: (query) => query.state.status === 'success' && query.state.data?.items.some(
      (run) => run.state === 'Queued' || run.state === 'Running',
    ) ? 2_000 : false,
  })

  if (history.isPending) return <p role="status">Loading local history…</p>
  if (history.isError) return <p role="alert">{history.error.message}</p>
  if (!history.data.items.length) return <p>No synchronization attempts recorded. Remote imports are not enabled.</p>

  return (
    <ul aria-label="Synchronization attempts">
      {history.data.items.map((run) => (
        <li key={run.id}>
          <strong>{run.operation}: {run.state}</strong>{' · '}
          <time dateTime={run.requestedAt}>{new Date(run.requestedAt).toLocaleString()}</time>
          {run.reasonCode === 'SERVICE_RESTARTED' ? ' · Interrupted by local service restart' : null}
        </li>
      ))}
    </ul>
  )
}
