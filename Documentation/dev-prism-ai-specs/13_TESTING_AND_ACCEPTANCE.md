# 13 — Testing, acceptance and measurement

## Test architecture

Use xUnit or equivalent for Domain/Application, real PostgreSQL via Testcontainers or dedicated disposable Compose DB for integration (not EF InMemory), adapter contract tests with sanitized recorded fixtures and opt-in live tests, React Testing Library and Playwright for UI. Use deterministic fake clock/model/CLI for state/failure tests; visibly separate from real integration. No live credentials in normal CI. Live tests only explicit `DEVPRISM_RUN_LIVE_TESTS` and dedicated test project with scoped keys; destructive tests require specific consent. Unit tests fast, integration tests isolated, full demo rehearsal separate. Independent checks not controlled by implementation agent.

## Test matrix

| ID | Scenario | Expected evidence |
|---|---|---|
| T01 | bootstrap fresh DB, restart, migrate prior DB | health and migration logs, persistent records |
| T02 | local auth, CSRF, cross-project IDs in REST/hub/media | 401/403/404, no unauthorized events or content |
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

## UI acceptance (Playwright)

Create/select project; setup skips optional AI; browse fixture mode banner; actual source link and revision visible. Feature tabs preserve selection and show linked groups. Click graph edge opens proof, confirm/reject updates state. Watchtower before/after and coverage; candidate severity distinguishable without color. Chat sends read query, then action proposal; approval shows exact diff and target; stale approval cannot start. Mission DAG shows actual event replay, partial outcome, checks and preview link. Cancel/blocked states actionable. Keyboard navigate rail, dialogs and tabs; focus restored; text alternative to graph. Responsive check at 1280x720 and 1440x900. Screenshot artifacts per major route, do not claim screenshot pixel matching conceptual design. Accessibility basic automated scan + manual keyboard.

## Live gates and demo checklist

G0 proofs recorded: exact engine/model, real provider chat and CLI, ADO read/write authorization and revision safety, Wiki update semantics, Figma read and conditional write, sandbox escape/credential tests. G3 read slice: actual imported tasks, Wiki, selected design and Git commit; no model usage. G4 guard: labeled cases with expected findings, no mutation. G6 external actions: live ADO + Wiki with deliberate partial failure test in fixture and successful live write; verify receipts. G7 engine: actual isolated code change/test and preview. G8 parallel: timestamps and real commits; PR review and merge conditional. G9 full rehearsal twice including app restart and external service failure fallback. Capture source IDs redacted where needed, screenshot, commands and result; never show tokens or internal employer data.

## Benchmark (optional, evidence-driven)

Question: does structured graph/context reduce rediscovery, errors or time versus a normal agent? Use 6–12 representative tasks across features and two repo sizes; same engine/model, initial commit, sources/permissions, time/token budget, independent test suite. Compare A: normal agent with ordinary access; B: agent with well-written AGENTS.md/linked docs; C: same agent with DEV PRISM context pack; optional D: graph+vector. At least 3 repeats per case where budget permits, randomized order and control cold/warm cache separately; record actual input/output/cached tokens, cost, wall time, correctness, human corrections, link recall, false blockers, overhead of indexing/approval/analysis and memory maintenance. If usage unavailable, report coverage and no token claim. Use median and spread, show failures, not cherry-picked best. On tiny repo expect baseline may win; product still offers centralized visibility, traceability and actions. Do not claim superiority over Cursor without equivalent access/configuration and measured results.

## Definition of done

Feature meets explicit requirement, negative tests, real or fixture mode truthfulness, source/decision docs updated, security constraints, no warnings ignored without reason, evidence recorded in roadmap. A group with external blocked capability can be marked partially complete only with explicit blocker and unaffected tests passing; no [X] for unmet live criterion. Before shipping verify zip/spec references, lockfiles, migration path, clean startup and no secrets. Production public deployment is not in scope.
