// Spec: Documentation/dev-prism-ai-specs/07_PROJECT_BRAIN.md (local memory).
import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Badge, ContextCard, EmptyState, Icon } from './WorkspaceUi'

type Decision = {
  id: string; key: string; revision: number; statement: string; rationale: string
  state: 'Draft' | 'Approved' | 'Superseded'; evidence: 'OperatorStatement'; approvedAt: string | null
}

/** Reads or updates local decision memory without involving outside services. */
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { cache: 'no-store', ...init })
  if (response.status === 409) throw new Error('The decision changed or a draft with that key already exists. Review the current versions.')
  if (response.status === 400) throw new Error('Check the decision key, statement and rationale.')
  if (!response.ok) throw new Error('Local decision memory is unavailable.')
  return response.json()
}

function decisionQuery(projectId: string, featureId: string) {
  const path = `/api/v1/projects/${encodeURIComponent(projectId)}/features/${encodeURIComponent(featureId)}/decisions`
  return { path, options: queryOptions({
    queryKey: ['project', projectId, 'features', featureId, 'decisions'] as const,
    queryFn: ({ signal }) => request<{ items: Decision[]; truncated: boolean }>(path, { signal }),
  }) }
}

/** Reuses the same project-scoped cache as the editable decision history. */
export function DecisionHighlights({ projectId, featureId }: { projectId: string; featureId: string }) {
  const decisions = useQuery(decisionQuery(projectId, featureId).options)
  const approved = decisions.isSuccess ? decisions.data.items.filter((item) => item.state === 'Approved') : []
  return <ContextCard title="Approved decisions" icon="book">
    {decisions.isPending ? <p role="status">Loading decisions…</p> : null}
    {decisions.isError ? <p>No decision summary available.</p> : null}
    {decisions.isSuccess && !approved.length ? <p>{decisions.data.truncated ? 'No approved decisions in the versions shown.' : 'No decisions approved for this feature yet.'}</p> : null}
    {approved.length ? <ul className="compact-list" aria-label="Approved decision highlights">{approved.slice(0, 3).map((item) => (
      <li key={item.id}><Badge tone="good">{item.key} · v{item.revision}</Badge><p>{item.statement}</p></li>
    ))}</ul> : null}
    <a className="text-link" href="#decision-memory">View decision memory <Icon name="arrow" /></a>
  </ContextCard>
}

function focusReview(node: HTMLElement | null) {
  node?.focus()
}

/** Displays approved memory and requires a separate confirmation for the exact draft. */
export function DecisionMemory({ projectId, featureId }: {
  projectId: string; featureId: string
}) {
  const [review, setReview] = useState<{ draft: Decision; baseline: Decision | null } | null>(null)
  const [draftKey, setDraftKey] = useState('')
  const [statement, setStatement] = useState('')
  const [rationale, setRationale] = useState('')
  const client = useQueryClient()
  const { path, options } = decisionQuery(projectId, featureId)
  const key = options.queryKey
  const decisions = useQuery(options)

  /** Builds the authorized request for an explicit local mutation. */
  function post(target: string, body: unknown) {
    return request<Decision>(target, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
  }

  const draft = useMutation({
    mutationFn: (values: { key: string; statement: string; rationale: string }) => post(path, values),
    onSuccess: async () => {
      setDraftKey(''); setStatement(''); setRationale('')
      await client.invalidateQueries({ queryKey: key })
    },
  })
  const approve = useMutation({
    mutationFn: (reviewed: NonNullable<typeof review>) => post(`${path}/${reviewed.draft.id}/approve`, {
      expectedBaselineId: reviewed.baseline?.id ?? null,
    }),
    onSuccess: () => setReview(null),
    onError: () => setReview(null),
    onSettled: () => client.invalidateQueries({ queryKey: key }),
  })

  /** Saves a draft only; approval remains a different user action. */
  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    draft.mutate({ key: draftKey, statement, rationale })
  }

  return (
    <section className="panel" id="decision-memory" tabIndex={-1} aria-labelledby="decision-title">
      <div className="panel-heading"><span className="icon-tile"><Icon name="book" /></span><div><h2 id="decision-title">Decision memory</h2><p>Operator statements only; no provider writes.</p></div></div>
      {decisions.isPending ? <p role="status">Loading decision history…</p> : null}
      {decisions.isError ? <p className="notice error" role="alert">{decisions.error.message}</p> : null}
      {decisions.isSuccess ? (
        <>
          {decisions.data.items.length === 0 ? <EmptyState title="No decisions recorded" icon="book">Save a draft to start this feature's decision history.</EmptyState> : (
            <ul className="decision-list">
              {decisions.data.items.map((entry) => (
                <li className="decision-item" key={entry.id}>
                  <div className="decision-title"><strong>{entry.key} · v{entry.revision}</strong><Badge tone={entry.state === 'Approved' ? 'good' : entry.state === 'Draft' ? 'accent' : 'neutral'}>{entry.state}</Badge></div>
                  <p className="record-text">{entry.statement}</p><p className="record-text secondary">{entry.rationale}</p>
                  {entry.approvedAt ? <p className="secondary">Approved <time dateTime={entry.approvedAt}>{new Date(entry.approvedAt).toLocaleString()}</time></p> : null}
                  {entry.state === 'Draft' ? (
                    <button className="button button-secondary" type="button" disabled={approve.isPending} onClick={() => {
                      approve.reset()
                      setReview({ draft: entry, baseline: decisions.data.items.find((row) => row.key === entry.key && row.state === 'Approved') ?? null })
                    }}>Review {entry.key} v{entry.revision}</button>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
          {decisions.data.truncated ? <p className="notice">Only the first 100 versions are shown.</p> : null}
          {review ? (
            <section className="review-panel" aria-labelledby="decision-review-title" ref={focusReview} tabIndex={-1}>
              <h3 id="decision-review-title">Approve {review.draft.key} v{review.draft.revision} locally</h3>
              <p className="record-text">{review.draft.statement}</p><p className="record-text secondary">{review.draft.rationale}</p>
              {review.baseline ? <p>Replaces v{review.baseline.revision}: {review.baseline.statement}</p> : <p>No previous approved version in the versions shown.</p>}
              <div className="button-row"><button className="button button-primary" type="button" disabled={approve.isPending} onClick={() => approve.mutate(review)}>Confirm local approval</button>
                <button className="button button-secondary" type="button" disabled={approve.isPending} onClick={() => setReview(null)}>Cancel</button></div>
            </section>
          ) : null}
          {approve.isError ? <p className="notice error" role="alert">{approve.error.message}</p> : null}
          <form className="form-divider" onSubmit={save}>
            <h3>Draft a decision</h3>
            <div className="form-field"><label htmlFor="decision-key">Decision key</label><input id="decision-key" required maxLength={80} placeholder="e.g. BR-006" value={draftKey} onChange={(e) => setDraftKey(e.target.value)} /></div>
            <div className="form-field"><label htmlFor="decision-statement">Statement</label><textarea id="decision-statement" rows={3} required maxLength={2000} value={statement} onChange={(e) => setStatement(e.target.value)} /></div>
            <div className="form-field"><label htmlFor="decision-rationale">Rationale</label><textarea id="decision-rationale" rows={3} required maxLength={2000} value={rationale} onChange={(e) => setRationale(e.target.value)} /></div>
            <button className="button button-primary" type="submit" disabled={draft.isPending}><Icon name="plus" />{draft.isPending ? 'Saving draft…' : 'Save draft'}</button>
            {draft.isError ? <p className="notice error" role="alert">{draft.error.message}</p> : null}
          </form>
        </>
      ) : null}
    </section>
  )
}
