# 11 — REST API and event contracts

## API conventions

Base `/api/v1`; JSON camelCase, UTF-8, ISO UTC times, UUIDs as strings, enums documented strings. Project routes scoped `/projects/{projectId}` below. Use authenticated local session, CSRF protection for cookie-authenticated writes, explicit CORS only dev origin. Authorization enforced on each handler and service. OpenAPI generated and checked into contracts or generated client; do not expose secret-bearing request schemas in logs. List response `{items, nextCursor, coverage?, asOf?}`, max page 100; stable sort + opaque cursor. Return 202 + job/mission ID for asynchronous work, 201 for created local draft. `Idempotency-Key` for mutation requests; same key/different request hash -> 409. `If-Match` or explicit expected version for mutable local records. Remote target preconditions separately enforced. Use standard ProblemDetails with code/correlation ID and sanitized detail; no stack trace or tokens.

## Local MVP HTTP contract

ASP.NET Core Minimal API is the only application API. The React app consumes explicit DTOs generated or checked against the versioned OpenAPI document; never import provider SDK types into the browser. Keep one canonical JSON schema per request/response. In packaged mode API, auth and SPA are same-origin. Use UTC timestamps, UUID internal IDs and client-visible enum strings; `null` denotes unavailable values, not zero or a synthetic revision. Authentication, antiforgery and error semantics follow [12_SECURITY_AND_OPERATIONS.md](12_SECURITY_AND_OPERATIONS.md#local-mvp-security-contract).

The MVP implements this inventory only. Project subpaths are relative to `/api/v1/projects/{projectId}`; authenticated project endpoints return either scoped results or a non-disclosing `404` for inaccessible identifiers. A denied known capability is represented independently of route-level authorization.

| Operation | Request and result |
|---|---|
| Readiness and local session | Readiness only at `/api/v1/health`; pairing/session bootstrap, session status, sign-out via same-origin auth routes. No provider secrets or project data before pairing. |
| List/create projects; get project | Stable project UUID + slug/name. Project data never shared across IDs. |
| List/create/update `/connections`; disconnect | Provider profile and typed scope/target IDs, configuration version, explicit change intent, no arbitrary tool names, headers or endpoints. Return capabilities, redacted approved scope, authorization state and last observed health; never credentials or signed links. Disconnect requires confirmation. |
| `POST /connections/{id}/probe` and `/connections/{id}/sync` | Expected config version; sync reads the configured selection only. Accept the request as `202` with run ID and status URL after persistence; return the existing run for a repeated active identical request. Reject stale version or `SYNC_BUSY` where the single worker is occupied by another connection. |
| `GET /sync-runs/{id}` and `GET /sync-runs` | Scoped run status/history; state, timestamps, requested/observed target counts, result counts, per-capability coverage and safe reason codes; include `retryAt` when supplied. Reading status is a local DB operation. |
| `GET /resources`, resource detail, `/versions`, `/diff`, `/preview` | Cached list/detail and versions. Optional filters: kind, feature, text, cursor; cap page size at 100. Exact immutable IDs for diff. Preview serves only authorized cached media; unavailable means no image, never an implicit provider fetch. |
| List/create `/features`; get a feature and its grouped `/resources` | Feature key/name, differently labeled resource groups including coverage, summaries of local decisions, latest observations and changes. Use optimistic versioning for edits. |
| Add/remove typed feature links | Target an existing resource within the same project; include observed version for evidence when available. Duplicate link is idempotent; unlink never deletes the resource/version. |
| List/draft/approve/supersede `/decisions` | Draft includes feature scope, statement, rationale and evidence references. Approval specifies draft version and expected baseline; server uses the paired operator as actor and returns approved version/reference. No external write is associated with this operation. |

**Field-level contracts:** Resource summaries carry resource ID, kind, title, original URL if permitted, latest immutable version ID, observed time, access/coverage and transport (`RemoteMcp`, `Rest`, or `Fixture`, plus `Manual` for local-only evidence). Provider revision, when present, is a separate nullable string. Run outcomes distinguish job state from per-capability access and health; a hub event or percent is not needed. Public problem codes add `SYNC_BUSY`, `CONNECTION_CHANGED`, `REAUTH_REQUIRED`, `SOURCE_ACCESS_DENIED`, and `PREVIEW_UNAVAILABLE` where applicable. Provider failure after a `202` belongs in the run result, not a retrofit of the acceptance response. Use explicit concurrency tokens for connection and decision changes; a mismatch returns `409` and the UI refetches. Do not publish mixed provider-auth redirects as JSON errors; provider authorization initiation/callback is a separate vetted browser flow.

**Client refresh policy:** poll a queued/running run roughly every two seconds while its view is active (bounded backoff after transient local failure). Stop on terminal state. On reconnect or return to the tab, fetch current local state, then invalidate only the affected project, connection, feature and resource queries. App reload does not re-send the mutation or trigger import. SignalR and replay are post-MVP.

## Extended endpoint inventory (post-MVP where not listed above)

Unless stated, paths relative to `/api/v1/projects/{projectId}`.

| Method/path | Body/query | Result |
|---|---|---|
| GET `/api/v1/health` | none | readiness only, no private config |
| GET/POST `/api/v1/projects` | filter / name, slug, workspace registration | project list / create |
| GET/PATCH `/` | versioned project settings | summary / updated version |
| GET/POST `/connections` | kind, endpoint, scope, secret input via secure store | list / draft connection |
| PATCH `/connections/{id}` | version + config | updated, capability invalidation |
| POST `/connections/{id}/probe` | read or explicit disposable write test | async job + capability report |
| POST `/connections/{id}/sync` | scope, full or incremental | sync job ID |
| POST `/sync` | optional selected connection IDs | grouped jobs |
| GET `/sync-runs` | connectionId, cursor | run histories |
| GET `/dashboard` | optional activity cursor | counts, coverage, freshness, findings |
| GET `/resources` | kind, featureId, search, state, cursor | scoped normalized list |
| GET `/resources/{id}` | versionId optional | source + snapshot + permissions |
| GET `/resources/{id}/versions` | cursor | immutable history |
| GET `/resources/{id}/diff` | before, after version IDs | bounded structured diff |
| GET `/resources/{id}/preview` | versionId | authorized cached media; missing -> explicit fetch |
| GET/POST `/features` | search / key, title, description | list / create |
| GET/PATCH `/features/{id}` | expected version | overview / update local feature |
| GET `/features/{id}/resources` | kind, link state, cursor | grouped references |
| GET `/graph` | rootNodeId, depth, limit, edgeTypes | nodes, edges, truncated, coverage |
| POST `/graph/edges` | from/to, relation, evidence | proposed or user-confirmed local edge |
| POST `/graph/edges/{id}/review` | expectedVersion, confirm/reject, reason | reviewed edge |
| GET/POST `/decisions` | filters / draft statement + scope + source | list / draft |
| POST `/decisions/{id}/versions` | supersedes + draft fields | new immutable draft |
| POST `/decisions/{id}/approve` | versionId, expected baseline, rationale | approved local rule or approved target |
| POST `/context-packs/preview` | feature, role, refs, budget | bounded pack, estimate, omission report |
| GET `/findings` | filters, cursor | evidence class + status + coverage |
| GET `/findings/{id}` | none | detail, evidence, impacted nodes |
| POST `/findings/{id}/review` | version, acknowledge/dismiss, reason | new state; no manual false resolution |
| POST `/analysis` | scope, mode deterministic/semantic, budget | analysis job |
| GET/POST `/chat/threads` | featureId / title | thread list / create |
| GET `/chat/threads/{id}/messages` | cursor | messages + citations |
| POST `/chat/threads/{id}/messages` | content, refs, model profile, budget | accepted message/job; streamed events |
| GET/POST `/missions` | filter / requestText, featureId | list / draft mission |
| GET `/missions/{id}` | none | state, current plan, summary |
| POST `/missions/{id}/plans` | validated draft or edit | immutable plan version + hash |
| GET `/missions/{id}/plans/{planId}` | none | steps, diffs, preconditions |
| POST `/missions/{id}/approve` | planId/hash, selected steps, intent | approval receipt |
| POST `/missions/{id}/start` | approvalId, planId/hash | enqueue, 202 |
| POST `/missions/{id}/cancel` | reason | accepted cancel intent |
| POST `/missions/{id}/resume` | reconciled state, new approval as required | new attempt/job |
| GET `/missions/{id}/tasks` | none | DAG + attempt summary |
| GET `/missions/{id}/runs/{runId}/logs` | afterSequence, limit | redacted log events |
| GET `/missions/{id}/changes` | none | actual commit/diff summary |
| GET `/missions/{id}/checks` | commitSha | checks and evidence |
| GET `/missions/{id}/actions` | none | attempts/receipts |
| POST `/missions/{id}/actions/{attemptId}/reconcile` | reason | safe read reconciliation job |
| POST `/missions/{id}/preview/start` | profileId, commitSha | job after policy validation |
| POST `/missions/{id}/preview/stop` | previewId | stop owned runtime |
| GET `/events` | afterSequence, limit | durable event replay |
| GET `/usage` | missionId, date range | known usage + unknown count + estimate flags |
| GET `/audit` | filters, cursor | redacted audit entries |

Push/PR/merge and policy promotion use approved typed plan continuations, not arbitrary one-shot mutation endpoints. Provider credentials use dedicated write-only input, never returned; revoke/delete operations explicit and audited. Add project deletion only after safety tests. Unsupported P2 routes not advertised as functional.

## Example feature read

```json
{
  "id":"<uuid>", "key":"dish-rating", "name":"Dish Rating", "state":"Active",
  "coverage":{"workItems":"Current","design":"Stale","code":"Current","docs":"Current"},
  "groups":[{"kind":"WorkItem","count":5,"verifiedLinks":4,"proposedLinks":1}],
  "findings":{"verified":1,"candidates":1,"unknownChecks":1},
  "asOf":"2026-11-25T19:00:00Z", "version":3
}
```

Example approval: `{planId:"<uuid>",planHash:"sha256:...",selectedStepKeys:["ado","wiki"],intent:"Approve exact external changes"}`. Actor comes from authenticated session; never client supplied. Server validates dependencies, scope, freshness and hash. Start returns `{missionId,jobId,state:"Queued"}` not success claim.

## SignalR and durable events (post-MVP)

Hub `/hubs/project-events`. Authenticate connection; subscribe only authorized project and optional mission. Event envelope `{schemaVersion:1,sequence:123,eventId,projectId,entityType,entityId,type,occurredAt,correlationId,payload}`. Types: `sync.started`, `sync.progress`, `sync.completed`, `resource.changed`, `connection.healthChanged`, `edge.reviewed`, `finding.changed`, `decision.changed`, `chat.messageDelta`, `chat.completed`, `plan.created`, `approval.granted`, `mission.stateChanged`, `task.stateChanged`, `agent.log`, `agent.usage`, `action.stateChanged`, `check.completed`, `preview.stateChanged`. Durable state changes persisted; transient text deltas may be non-durable but final message persisted, no lost final state. Logs separate sequence per run and bounded; avoid huge payloads. UI on reconnect calls replay after last durable sequence, dedupes by event ID, refetches impacted query cache. If retention gap, return reset-required and re-fetch full state. Delivery at least once, not exact once. Never broadcast credentials/raw prompt/private content to another project.

## Error codes

`VALIDATION_FAILED` 400; `AUTH_REQUIRED` 401; `FORBIDDEN_SCOPE` 403; `NOT_FOUND` 404 (avoid leaking inaccessible IDs); `VERSION_CONFLICT`, `PLAN_STALE`, `IDEMPOTENCY_CONFLICT`, `DEPENDENCY_BLOCKED` 409; `CAPABILITY_UNAVAILABLE`, `SANDBOX_REQUIRED`, `BUDGET_EXCEEDED` 422; `RATE_LIMITED` 429 with retryAt; `PROVIDER_UNAVAILABLE` 503; `UNKNOWN_REMOTE_OUTCOME` 409 with reconciliation link; `INTERNAL_ERROR` 500. Example ProblemDetails includes `type`, `title`, `status`, `code`, `correlationId`, `detail`, optional `retryAt`, `currentVersion`, `evidenceRef`. No provider secret or full response body. Never convert errors to successful 200 with silent failure.
