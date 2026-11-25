// Spec: Documentation/dev-prism-ai-specs/03_UI_UX_SPEC.md (sync history).
import { useQuery } from '@tanstack/react-query'
import { Badge, EmptyState, Icon } from './WorkspaceUi'

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
  if (history.isError) return <p className="notice error" role="alert">{history.error.message}</p>
  if (!history.data.items.length) return <EmptyState title="No synchronization attempts recorded" icon="history">Remote imports are not enabled in this build.</EmptyState>

  return (
    <ul className="entity-list" aria-label="Synchronization attempts">
      {history.data.items.map((run) => (
        <li className="entity-link run-row" key={run.id}>
          <span className="icon-tile"><Icon name="history" /></span>
          <span className="entity-label"><strong>{run.operation}</strong><small><time dateTime={run.requestedAt}>{new Date(run.requestedAt).toLocaleString()}</time>{run.reasonCode === 'SERVICE_RESTARTED' ? ' · Interrupted by local service restart' : null}</small></span>
          <Badge tone={run.state === 'Succeeded' ? 'good' : ['Failed', 'Partial', 'Interrupted'].includes(run.state) ? 'warning' : 'neutral'}>{run.state}</Badge>
        </li>
      ))}
    </ul>
  )
}
