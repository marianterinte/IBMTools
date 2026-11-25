# 06 — Connectors, MCP and synchronization

## Integration architecture

The Local MVP imports online artifacts through provider-specific adapters on the backend. A typed operation selects a discovered and explicitly mapped remote MCP read tool, validates its response and persists a normalized snapshot. No LLM participates. The MCP SDK owns protocol negotiation; the adapter owns authorization scope, tool selection, validation, normalization and provenance. No local MCP bridge, REST substitution, raw tool relay, or fixture can satisfy a required remote MCP read.

Connection configuration: name, approved base URL/host, external scope identifiers, auth kind, secret reference, read/write allowed capabilities, sync strategy, limits. Validate URL scheme/host, no userinfo, no unapproved redirects, no arbitrary private network fetch (see 12), token audience and scope. Scope all queries to explicit project/repo/file. Separate read and write probes. `Available` requires successful capability proof with actual credentials; `Unavailable` has reason; `Unverified` not executable. UI must reflect connection health independent of capabilities.

## Local MVP capability matrix

All rows start `Unverified`. Endpoint URLs, advertised tool names, supported auth flows and field shapes must be recorded from official documentation and the actual G0 probe, not assumed. Scope each connection to a single approved ADO project or Figma file and selected artifacts. Select by known work item ID or file/node URL first; do not require broad discovery/search to prove core import.

| Capability | Route / priority | Minimum useful result / G0 proof |
|---|---|---|
| ADO Boards: selected work item | Remote MCP, required | Authorized online result containing project-scoped work item ID, title, and meaningful work item content (e.g. description, acceptance criteria or provider state). Show provider original; use actual revision when returned and label optional fields as not supplied. |
| Figma: selected frame context | Remote MCP, required | Authorized online result attributable to the selected file and node, human-readable frame context (e.g. contents, annotations or structure) and original link. A screenshot alone, auth success or an echoed URL does not satisfy this row. |
| ADO Wiki: selected page | Remote MCP, auxiliary | Page scope and stable ID or canonical path, readable sanitized page text, original and revision if available. |
| ADO Repos: selected commit | Remote MCP, auxiliary | Repository scope, full commit SHA, original and available metadata. No clone, script execution, arbitrary paths or push required. |
| Figma: rendered preview | Remote MCP, auxiliary | Displayable media attributable to the selected frame and provider version when supplied; separate access and coverage from frame context. |

Only an owner-approved, capability-specific REST/API route may supply auxiliary data; record the ADR and show `REST` separately. No fallback is approved by this document. Never switch automatically on timeout, rate limit, schema mismatch, authentication failure or missing permission. Original links do not count as imported data. If a required read is missing or returns unusable content, mark that provider gate blocked; unaffected work may continue but Local MVP cannot pass.

### Connection lifecycle and tool mapping

1. Select a backend-owned provider profile with an approved HTTPS MCP endpoint. Validate selected remote identifiers and store a redacted connection reference; this is not an arbitrary MCP URL field. Verify the actual supported auth grant and least scopes in G0. Use the system browser for proven delegated authorization; no token is given to the React app.
2. Authenticate and negotiate supported MCP protocol/transport. List tools on the server but expose only normalized capabilities to the UI. Bind each capability to an allowlisted read tool, expected input/output schema and profile/mapping version. Advertisement alone stays `Unverified`.
3. Probe the mapped read with the approved synthetic artifact; validate scope, identity and meaningful fields. Record route, observed result, mapping version and verification time. A verified capability does not imply current connectivity or access to all resources.
4. Reuse the connection-scoped client where supported; dispose it on disconnect, credential or scope change. A changed endpoint, tool schema or mapping invalidates affected proof until re-probed. Discard in-flight results from obsolete connection configurations. Tool instructions and nested links are untrusted data; never execute returned commands or follow arbitrary asset URLs. Use the media policy in [12_SECURITY_AND_OPERATIONS.md](12_SECURITY_AND_OPERATIONS.md).

## External adapter extensions (post-MVP unless explicitly referenced by the matrix)

The following sections describe the expanded adapter, REST and Git paths. Do not read them as replacing the Local MVP remote MCP route or requiring local Git, writes or background provider polling for release.

### ADO Boards and Repos

Provision organization/project/repository manually. Discover project process and work item types/state fields; never assume Scrum/Agile labels or acceptance criteria field exists. Use OAuth/Entra or documented local authentication, a scoped PAT only for authorized disposable demo if policy permits. Configure organization, project ID/name, team optional, repo ID, default branch. Auth provider must return correct token audience and expiry. Use official local MCP server `microsoft/azure-devops-mcp` as candidate, restrict domains and pin version; discover actual tools at runtime. Validate MCP credentials separately from REST. Prefer typed REST for periodic reads and safe mutations requiring revision checks. See 16.

Read requirements:
- Query scoped work items using WIQL or supported API; page/batch IDs within provider limits. Fetch fields, relation links and revision; normalize descriptions/criteria to sanitized text with separate renderable Markdown/HTML. Include provider state plus normalized state, type, parent, assigned user display if permitted, tags, changed date, source URL. Do not store email/identity beyond necessary scope.
- Discover work item process metadata for create/update. Map feature hierarchy using allowed types; create parent before children; if type unsupported, propose alternative with owner approval. Work item create receipt real ID and revision. Use approved correlation marker (e.g. `devprism:<operation-id>` tag or description marker only if allowed) for deduplication; if marker impossible, require manual reconciliation after ambiguous create. Never retry unknown create automatically.
- Work item update: version precondition e.g. JSON Patch test `/rev` where supported; diff exact fields, validate allowed states and permissions, preserve unrelated fields. Never use bypassRules. Verify reread actual revision and intended field values. If changed externally, return Conflict with before/latest and replan.
- Repos: read branches, commits, PR title/status/head/target/policies when permitted; map PRs to work items via provider relations or verified explicit links. Push only approved branch and remote using Git broker. PR creation and completion via verified MCP capability or REST; honor policy, no bypass flags, merge only after separate approval. Existing draft PR reuse by known ID or unique source/target + marker; if ambiguous, stop. Do not mark ticket Done on PR creation. Discover correct state transition after merge.

## ADO Wiki (P0)

Select Wiki ID/type and page root. Fetch page tree and content with revision/ETag (or Git commit for code wiki); page identity stable where available, otherwise scope + normalized path. Sanitize rendering; preserve code blocks. Write proposed exact content diff; use supported If-Match/ETag or Git precondition, reject stale; create only if absent, check known path and operation marker; verify reread content hash. For code wiki may require Git PR instead of direct write; capability probe decides. Never overwrite unrelated sections generated from stale snapshot. Demo uses project wiki `/Business Rules/Ratings`, pre-created root if required. ADO Wiki and Git credentials separate as needed.

## Figma (P0 read, P1 conditional write)

Input specific design file URL, optional node URL; parse file key and node ID with validated official domain. Configure OAuth or narrowly scoped developer token if policy permits, actual seat/file rights. Read selected file/page/node metadata (not whole organization), relevant text, component props, dimensions, version and last modified; preview via official image render endpoint for selected frames, size cap and temporary URL handling. Cache binary only if allowed, local safe storage; do not persist signed URL in model context. No generic HTML embedding of Figma. Rate limits depend on seat, plan, endpoint; manual refresh and slow polling default, honor Retry-After, avoid full file reload if version unchanged. If API read blocked, offer explicit manual import of exported PNG + metadata with label `Manual snapshot`, not connected/live.

MCP: discover remote `https://mcp.figma.com/mcp` support, auth and tool schemas. Read design context and assets only for linked frames. Tool write exists in official remote integration but requires access/seat and working client; do not promise successful arbitrary native edit. Probe in disposable duplicate frame with explicit approval, verify before/after and record allowed operation kinds. If write preconditions/verification unavailable, mark manual handoff or experimental only. For MVP demo user changes design in Figma, then DEV PRISM AI syncs it. Handoff includes frame link, proposed change, version and confirmation; verify on refresh before marking done. A design image alone is not proof of an interaction rule: use explicit annotations/text or human confirmation.

## Local Git (P0)

Register canonical local path and authorized remote; require repository root, not arbitrary directory. Read `git status`, branch/HEAD, `ls-tree`, `show`/`cat-file` with safe argument list, `diff` against last snapshot commit. Do not execute hooks or repository scripts in read adapter; no shell interpolation, ignore dangerous paths, cap output. Exclude `.env`, keys, binaries, vendor dirs, generated/build and ignored secrets by policy. Track paths at commit; use default branch remote tracking and local working tree separately labeled; do not mix uncommitted content with immutable baseline. Git fetch requires configured network authorization; never silently reset user's dirty worktree. Model sees only bounded file excerpts. PR associations require verified links or candidate review. Agent worktrees/copies see 10/12.

## Atlassian (P2)

Rovo MCP official remote endpoint and actual auth scopes allow selected Jira/Confluence operations, subject to org settings. Build `AtlassianConnector` via same resource and action contracts. Use REST for synchronization with pagination, content version and space/project allowlist. Confluence update requires current page version, preserve format and verify. Jira process-specific transitions and field schema. No initial dependency; demonstrate architecture extensibility through interface contract tests, not fake buttons. Other sources (GitHub, GitLab, pipelines, DB schema) P2.

## Sync algorithm and failure behavior

Local MVP defaults: operator starts a bounded sync for one connection and its configured targets. ADO/Figma calls occur only for explicit probe/sync/reconnect actions, never on cached browsing or after restart automatically. Read a bounded batch of selected targets; require complete, authorized, documented listing semantics before using absence to infer archival. Where no source revision or cursor exists, re-read selected targets and deduplicate by a normalized content hash. Record imported, updated, unchanged, skipped and failed counts from observed results; never infer full coverage from one sample. Persist route per observation, including approved auxiliary fallback, not merely per connection. Use the MVP checkpoint/recovery contract in [04_ARCHITECTURE.md](04_ARCHITECTURE.md#local-mvp-runtime-and-boundaries).

1. Acquire per-connection lease; persist run and starting cursor. If already running, coalesce manual request or queue follow-up.
2. Pull bounded pages using provider cursor/watermark. Stable identity includes project, connection and kind. Normalize, sanitize, hash content excluding volatile fields. For Git use commit and blob; ADO rev; Wiki ETag/version; Figma file/version + relevant node hash. Identical normalized hash does not emit repeated change, but update observed freshness/version where needed.
3. In transaction upsert resource, immutable version, pointer, change row/outbox and cursor. A cursor must only advance after committed page. Use overlapping changed-date windows and dedupe to avoid clock/pagination misses; periodic complete scoped reconciliation. Provider cursor opaque; restart can resume or reread safely. Process partial pages without declaring full coverage.
4. Complete full scan marks absent resources archived only if authoritative listing covered entire configured scope. 401/403 -> auth/permission status, hide inaccessible content, do not mark deleted. 404 for known item requires distinguishing deletion from access failure; uncertain -> unknown. 429 -> scheduled backoff and old data with stale label. Timeout/network -> degraded, preserve last success.
5. Event-driven deterministic checks on changed fields and affected edges. No LLM for routine sync. Optional semantic analysis queued only on explicit request or configured budget, dedupe by snapshot hash + check version.
6. Update last success only for successful scope; record partial coverage and failure. Push events after commit; UI reads updated state. Manual refresh same algorithm. Polling only while app running; webhooks optional P2 (do not expose local server publicly just to receive callbacks).

## MCP manager evolution (post-MVP extensions)

The Local MVP uses only verified remote profiles for its required reads; stdio, custom server setup, model-mediated tool wrappers and mutation mapping are later extensions.

Use official C# SDK verified in G0. Support stdio for trusted local executable with argument allowlist and minimal environment; HTTP streamable transport with approved HTTPS hosts and OAuth as supported by SDK/version. Explicit user connection setup, handshake, list tools with schema hash, capability map, timeout, cancellation, size limits. Tool descriptions/results untrusted, sanitize and classify; do not feed all schemas indiscriminately to model. Read tools allowed by scope; mutation tools only via ActionService typed mapping. On schema or server version change invalidate write capability until reverified. Never let agent install a server or modify connection from a ticket. Show provider original and read/write status. Keep OAuth tokens server-side; validate issuer/audience/redirect and no secret forwarding. If SDK lacks needed auth, document fallback to REST and do not build unsafe bypass.
