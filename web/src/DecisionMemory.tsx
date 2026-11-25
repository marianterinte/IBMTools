// Spec: Documentation/dev-prism-ai-specs/07_PROJECT_BRAIN.md (local memory).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'

type Decision = {
  id: string; key: string; revision: number; statement: string; rationale: string
  state: 'Draft' | 'Approved' | 'Superseded'; evidence: 'OperatorStatement'; approvedAt: string | null
}

/** Reads or updates local decision memory without involving outside services. */
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { cache: 'no-store', ...init })
  if (response.status === 409) throw new Error('The decision changed or a draft with that key already exists. Review the current versions.')
  if (response.status === 400) throw new Error('Check the decision key, statement and rationale.')
  if (response.status === 401) throw new Error('Local session is locked. Reload to check pairing.')
  if (!response.ok) throw new Error('Local decision memory is unavailable.')
  return response.json()
}

/** Displays approved memory and requires a separate confirmation for the exact draft. */
export function DecisionMemory({ projectId, featureId, csrfToken }: {
  projectId: string; featureId: string; csrfToken: string
}) {
  const [review, setReview] = useState<{ draft: Decision; baseline: Decision | null } | null>(null)
  const [draftKey, setDraftKey] = useState('')
  const [statement, setStatement] = useState('')
  const [rationale, setRationale] = useState('')
  const client = useQueryClient()
  const key = ['project', projectId, 'features', featureId, 'decisions'] as const
  const path = `/api/v1/projects/${projectId}/features/${featureId}/decisions`
  const decisions = useQuery({ queryKey: key, queryFn: ({ signal }) => request<{ items: Decision[]; truncated: boolean }>(path, { signal }) })

  /** Builds the authorized request for an explicit local mutation. */
  function post(target: string, body: unknown) {
    return request<Decision>(target, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-DevPrism-CSRF': csrfToken },
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
    <section aria-labelledby="decision-title">
      <h4 id="decision-title">Local decisions</h4>
      <p>Decisions here are operator statements. They do not update Azure DevOps or Figma.</p>
      {decisions.isPending ? <p role="status">Loading decision history…</p> : null}
      {decisions.isError ? <p role="alert">{decisions.error.message}</p> : null}
      {decisions.isSuccess ? (
        <>
          {decisions.data.items.length === 0 ? <p>No decisions recorded.</p> : (
            <ul>
              {decisions.data.items.map((entry) => (
                <li key={entry.id}>
                  <strong>{entry.key} v{entry.revision} · {entry.state}</strong>
                  <p>{entry.statement}</p><p>{entry.rationale}</p>
                  {entry.approvedAt ? <p>Approved <time dateTime={entry.approvedAt}>{new Date(entry.approvedAt).toLocaleString()}</time></p> : null}
                  {entry.state === 'Draft' ? (
                    <button type="button" onClick={() => {
                      approve.reset()
                      setReview({ draft: entry, baseline: decisions.data.items.find((row) => row.key === entry.key && row.state === 'Approved') ?? null })
                    }}>Review {entry.key} v{entry.revision}</button>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
          {decisions.data.truncated ? <p>Only the first 100 versions are shown.</p> : null}
          <form onSubmit={save}>
            <div className="pairing-field"><label htmlFor="decision-key">Decision key</label><input id="decision-key" required maxLength={80} placeholder="BR-006" value={draftKey} onChange={(e) => setDraftKey(e.target.value)} /></div>
            <div className="pairing-field"><label htmlFor="decision-statement">Statement</label><textarea id="decision-statement" required maxLength={2000} value={statement} onChange={(e) => setStatement(e.target.value)} /></div>
            <div className="pairing-field"><label htmlFor="decision-rationale">Rationale</label><textarea id="decision-rationale" required maxLength={2000} value={rationale} onChange={(e) => setRationale(e.target.value)} /></div>
            <button type="submit" disabled={draft.isPending}>Save draft</button>
            {draft.isError ? <p role="alert">{draft.error.message}</p> : null}
          </form>
        </>
      ) : null}
      {review ? (
        <section className="setup" aria-labelledby="decision-review-title">
          <h4 id="decision-review-title">Approve {review.draft.key} v{review.draft.revision} locally</h4>
          <p>{review.draft.statement}</p><p>{review.draft.rationale}</p>
          {review.baseline ? <p>Replaces v{review.baseline.revision}: {review.baseline.statement}</p> : <p>No previous approved version.</p>}
          <button type="button" disabled={approve.isPending} onClick={() => approve.mutate(review)}>Confirm local approval</button>{' '}
          <button type="button" disabled={approve.isPending} onClick={() => setReview(null)}>Cancel</button>
        </section>
      ) : null}
      {approve.isError ? <p role="alert">{approve.error.message}</p> : null}
    </section>
  )
}
