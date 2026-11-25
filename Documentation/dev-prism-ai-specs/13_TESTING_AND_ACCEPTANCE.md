# 13 — Testing, acceptance and measurement

## Verification policy

Do not require new FE/BE unit, integration or E2E suites as Local MVP deliverables. The required proof is a repeatable manual acceptance run, relevant existing build/type/lint/migration checks when available, and a concrete failure or degraded state for each applicable task. Fixtures can support recovery/negative checks with explicit labels; they never satisfy core provider reads. No keys or live provider calls in routine build checks. Always use authorized, dedicated demonstration sources.

## Local MVP acceptance run (M01–M10)

Run on the target laptop before presentation. Save a short redacted record for each step: environment, project/connection reference, expected outcome, actual outcome, status (`Verified live`, `Verified locally`, `Verified fixture`, `Blocked` or `Not applicable` for auxiliary only), timestamp and relevant screenshot/run/version reference. Do not capture tokens, signed asset URLs or unauthorized content. G0 proof verifies providers, G3 verifies the application. No step is already verified by this document.

| ID | Walkthrough | Pass condition |
|---|---|---|
| M01 | Start the packaged app and open or configure a project directly. | UI/API on loopback HTTPS, DB ready, no login/pairing or credentials returned. Prove a project written through the API is in the configured dedicated DB. Reject foreign browser origins and mutations without the packaged Origin; verify cross-project identifiers are not disclosed. |
| M02 | Select and probe a real Boards item, then sync it. | Scoped, usable work item visible; persisted run proves Remote MCP route and actual identity. An original link/handshake does not qualify. |
| M03 | Select and probe a real Figma frame, then sync it. | Scoped usable frame context visible; actual file/node or verified binding and Remote MCP route. Missing image does not fail a usable frame read. |
| M04 | Create Dish Rating, link both resources, draft and approve a rule with evidence. | One page groups the resources, shows originals and revisions/absence, and presents the exact locally approved version. No provider mutation. |
| M05 | Sync unchanged, then edit a disposable source through its provider UI and sync again. | Same identity, no duplicate on unchanged input, new version when content changes, before/after visible, local rule unchanged. |
| M06 | Resync with the other source unavailable or interrupt an active run. | Preserve committed data; show independent coverage and last success; interrupted work never appears complete. Use controlled scenarios for transient faults and record whether real or fixture. |
| M07 | Restart app and reopen feature with provider unavailable. | Existing links, allowed snapshots and approved rule persist; reads issue no provider/model calls and show last observation, not claimed new connectivity. |
| M08 | Disconnect a configured demo connection or exercise confirmed denial; then reconnect with fresh proof. | Affected payload disappears from detail/search/history/media and evidence surfaces; links and permissible local audit remain. Re-exposure requires a fresh successful scoped read. |
| M09 | Review auth/origin/scope and untrusted rendering using controlled benign negative cases. | Unpaired/other-project reads and mutations denied, unsafe provider-supplied rendering not executed, credential handling stays backend-only. Document what was actually checked; important gaps stay blocked. |
| M10 | Exercise Wiki, Repos and preview individually if configured. | Record actual proof or unavailability and label routes. Any auxiliary REST fallback must be separately approved and cannot satisfy M02/M03. |

A pass requires M01–M09, with M02/M03 proven live through PRISM. M10 must be reported but missing companion reads do not block the release. Offline presentation fallback or previous rehearsal captures are not evidence that a failed live call worked. Check primary keyboard path, status announcements and desktop layout while performing the walkthrough.

## Extended test matrix (post-MVP scenarios and optional automation reference)

The following coverage catalog supports later tracks; it does not mandate new test suites for each change. High-risk writes/merge and isolation must have separate independent verification before enabling them.

| ID | Scenario | Expected evidence |
|---|---|---|
| T01 | bootstrap fresh DB, restart, migrate prior DB | health and migration logs, persistent records |
| T02 | deployment-specific auth/CSRF where applicable, local boundary, cross-project IDs in REST/hub/media | 401/403/404, no unauthorized events or content |
| T03 | invalid connection URL, redirects, secret logs | blocked network/path, no token disclosure |
| T04 | read sync same page twice, reordered data | stable identity/version, one change event |
| T05 | crash between page and cursor commit | safe replay, no lost resources |
| T06 | partial list, 403, 429, actual deletion | stale/inaccessible/archived distinguished, cursor and retry correct |
| T07 | Figma preview URL expired, limited seat, unsupported write | manual refresh or fallback label, no fake write |
| T08 | ADO process different types/states | discovered fields, valid type mapping; no hardcoded Scrum assumptions |
| T09 | Git file path traversal/symlink/binary/ignored secret | blocked or metadata only, no untrusted execution |
| T10 | cached dashboard/features with model provider disabled | zero model calls, usable browse/graph/search |
| T11 | cross-project edge and unknown model source ID | rejected by service/DB, no fabricated link |
| T12 | relevant evidence revision changes | edge stale, candidate reviewed; verified explicit link retained if unchanged |
| T13 | context budget smaller than mandatory rules | stop and explain; no truncation of mandatory policy |
| T14 | approved BR-012 persists across restart/new thread | cited decision/version, no prior chat required |
| T15 | half-star change vs integer API fixture | finding with before/after evidence, exact affected links, no mutation |
| T16 | ambiguous Figma image and unknown scope | candidate/unknown, not verified claim |
| T17 | dismiss, changed revision, resolve after actual fix | scoped dismissal/reopen; resolution requires check proof |
| T18 | malicious prompt/tool description instructs write/secret | no action dispatch; untrusted content not instructions |
| T19 | model invalid plan schema, cycle, unapproved paths | validation errors, no execution |
| T20 | approval hash mismatch, expired/revoked, altered selected steps | denied, no external side effect |
| T21 | external ADO revision changes before write | conflict and fresh review required |
| T22 | live create with timeout after application (fixture simulate) | Unknown -> read reconciliation -> verified, no duplicate create |
| T23 | ADO success, Wiki failure | partial state, receipts preserved, only failed step retry after reconciliation |
| T24 | action revocation/cancellation during network | no further calls, in-flight reconciled, truthful status |
| T25 | CLI actual single edit and test in sandbox | real diff, process exit, independent check, no external writes |
| T26 | two independent tasks concurrently | overlapping timestamps, distinct workspace refs, common SHA |
| T27 | cyclic/overlapping ownership tasks | reject or serialize; no unsafe fan-out |
| T28 | engine succeeds but protected test changed | integration block, diff evidence |
| T29 | integration conflict, failing build/test | no push/merge; conflict shown; preserved work |
| T30 | main moved after checks or branch policy unsatisfied | stale/blocked merge, no bypass |
| T31 | preview start/health/stop, collision, unsafe compose | isolated runtime, correct SHA, no unsafe command |
| T32 | stop agent child process tree, worker restart | interrupted state, stopped sandbox, no duplicate run/write |
| T33 | SignalR reconnect/dedupe and retention gap | correct final UI and replay/reset |
| T34 | tokens missing or budget exceeded | unknown displayed, budget enforcement, no invented cost |
| T35 | real ADO work item + Wiki write + reread | actual IDs/revisions, receipt and verified intended fields |
| T36 | real PR + separate human merge | actual remote SHA/state; conditional on permitted branch policies |
| T37 | Figma live read and optional write | actual version/preview; write only if verified and explicitly approved |
| T38 | privacy export contract/CSV header regression | known fields forbidden, tests independently run; no blanket privacy proof |
| T39 | manual Figma handoff | not marked done until user edits and refresh confirms evidence |
| T40 | retention and access revocation | cache hidden, purge per policy, referenced evidence handled safely |

## Extended UI acceptance (manual or optional automation)

Create/select project; setup skips optional AI; browse fixture mode banner; actual source link and revision visible. Feature tabs preserve selection and show linked groups. Click graph edge opens proof, confirm/reject updates state. Watchtower before/after and coverage; candidate severity distinguishable without color. Chat sends read query, then action proposal; approval shows exact diff and target; stale approval cannot start. Mission DAG shows actual event replay, partial outcome, checks and preview link. Cancel/blocked states actionable. Keyboard navigate rail, dialogs and tabs; focus restored; text alternative to graph. Responsive check at 1280x720 and 1440x900. Screenshot artifacts per major route, do not claim screenshot pixel matching conceptual design. Accessibility basic automated scan + manual keyboard.

## Later live gates and demo checklist

G4: labeled checks with evidence and no mutation. G5–G7: validate actual model and CLI separately, approved disposable provider writes with revision safety and reconciliation, isolated code change and independently verified build/test and preview. G8: actual concurrency, PR and conditional merge; optional Figma write only if permitted. G9 rehearses the extended presentation separately. None are prerequisites for G3 approval.

## Benchmark (optional, evidence-driven)

Question: does structured graph/context reduce rediscovery, errors or time versus a normal agent? Use 6–12 representative tasks across features and two repo sizes; same engine/model, initial commit, sources/permissions, time/token budget, independent test suite. Compare A: normal agent with ordinary access; B: agent with well-written AGENTS.md/linked docs; C: same agent with DEV PRISM context pack; optional D: graph+vector. At least 3 repeats per case where budget permits, randomized order and control cold/warm cache separately; record actual input/output/cached tokens, cost, wall time, correctness, human corrections, link recall, false blockers, overhead of indexing/approval/analysis and memory maintenance. If usage unavailable, report coverage and no token claim. Use median and spread, show failures, not cherry-picked best. On tiny repo expect baseline may win; product still offers centralized visibility, traceability and actions. Do not claim superiority over Cursor without equivalent access/configuration and measured results.

## Definition of done

Each task meets its observable outcome, records verification plus a relevant negative/degraded scenario, and does not claim more than verified. Update source/decision docs, avoid unresolved boundary breaks, and store redacted evidence in the roadmap. Missing required outcomes remain unchecked with an explicit blocker. Before release check doc references, lockfiles, migration, clean startup, local boundary and no secrets. No new mandatory FE/BE test suites or public deployment.
