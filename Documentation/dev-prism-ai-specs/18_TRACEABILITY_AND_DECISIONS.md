# 18 — Traceability, decisions and risks

## Owner-established direction vs proposed implementation defaults

The owner clarified that only DEV PRISM AI and its persistence run locally. ADO and Figma must be real online services reached through real remote MCP; a local MCP fixture is not an acceptable substitute. The presentation at IBM uses authorized synthetic DishRate assets, not employer data or branding. The owner also rejected new FE/BE unit and integration test suites as standard task deliverables and authorized documentation work before implementation. The wider product vision (chat, controlled changes, coding agents, impact analysis) remains intact. The selected technical defaults below specify the baseline without asserting vendor capabilities or authorizing code work.

## Proposed ADR register

| ID | Decision | Rationale / alternative |
|---|---|---|
| ADR-01 | ASP.NET Core Minimal API on .NET 10 LTS; React + TypeScript + Vite | Backend owns policy and persistence; same-origin local SPA; no separate web server in packaged mode. |
| ADR-02 | Local PostgreSQL + EF Core/Npgsql; relational resources, versions and links | Atomic checkpoints and cross-project constraints; no graph DB, vector DB or cache service. Reopen if Docker-free setup becomes required. |
| ADR-03 | Remote MCP for required read ingestion, deterministic adapters without model calls | Mandatory for ADO Boards and Figma frame context. Auxiliary-only REST fallback needs a recorded approval; none assumed. |
| ADR-04 | Feature is first-class local entity | source tool navigation secondary; links evidence-based |
| ADR-05 | Lexical/graph context first; pgvector optional | test incremental value rather than assume savings |
| ADR-06 | Existing CLI, OpenCode candidate, Codex alternate | avoid building coding engine; provider compatibility gate |
| ADR-07 | Deterministic durable DAG scheduler | workflow state/recovery owned by product; optional framework later |
| ADR-08 | Separate approval for actual external diff and merge | explicit side effects and branch policy; no implicit auto merge |
| ADR-09 | Worktree/copy plus real sandbox | Git isolation not a security boundary |
| ADR-10 | Figma read core; write conditional/manual handoff | vendor auth, permission and safe version guard uncertain |
| ADR-11 | Immutable source versions + approved decision lifecycle | traceability, conflict rather than silent overwrite |
| ADR-12 | G0–G3 Local MVP; richer Watchtower, chat and coding after release | First prove live read, feature links and memory without AI/CLI dependencies. |
| ADR-13 | Laptop app with real remote services and synthetic online assets | No hosted DEV PRISM service, no employer data, no misleading fixture claims. |
| ADR-14 | English specs and UI, local dark-first shell | Maintain existing technical vocabulary and UX continuity. |
| ADR-15 | Single local worker, persisted runs and bounded status polling | Restart-safe without SignalR, separate worker or timed provider polling in MVP. Revisit for streaming chat or multi-worker deployment. |
| ADR-16 | Manual evidence and existing checks for MVP acceptance | Consistent task template, observable negative cases, no mandatory new FE/BE test suites. |
| ADR-17 | Remote source authority separate from approved local decisions | Imports cannot rewrite local policy. Unknown/denied/transient access states remain distinct. |

ADR-01–03 and ADR-12–17 are the documentation baseline for the Local MVP. Vendor behavior is unverified until G0 evidence is recorded; later roadmap defaults remain provisional. For a change record alternatives, consequences, owner/date and affected requirements. For auxiliary fallbacks record the exact capability, alternate route, provenance label and what still does not qualify as a remote MCP read. Update linked contracts instead of maintaining competing versions.

## Requirement traceability

| Requirement | Spec | Roadmap | Acceptance |
|---|---|---|---|
| R01 setup | 03/04/06/12 | G0–G1 | M01/M09 |
| R02 ADO | 06 | G0/G2–G3, G6 writes later | M02/M05, T35 later |
| R03 Figma | 06 | G0/G2–G3, G8 writes later | M03/M10, T37 write later |
| R04 Repos/Git/PR | 06/10 | G2 auxiliary, G7 later | M10, T36 later |
| R05 Wiki | 06 | G2 auxiliary, G6 writes later | M10, T35 later |
| R06 feature explorer | 03/07 | G3 | M04/M07/M09 |
| R07 changes/graph | 03/05/07 | G2–G3 history, G4 graph later | M05/M06, T11/T33 later |
| R08 decisions | 05/07 | G3/G4 | M04/M07, T14 later |
| R09 monitoring | 08 | G4 after Local MVP | T15–T17 later |
| R10 chat | 09/17 | G5 | T18–T20 |
| R11 backlog | 02/09 | G6 | T22/T35 |
| R12 cross-system writes | 06/09 | G6 | T21–T24/T35 |
| R13 engine/preview | 10/12 | G7 | T25/T28/T31/T32 |
| R14 PR/merge | 10 | G7/G8 | T30/T36 |
| R15 parallel | 10 | G8 | T26/T27/T29 |
| R16 guard | 08/10 | G4/G7/G8 | T15/T28/T38 |
| R17 telemetry | 09/11 | G5/G7 | T33/T34 |
| R18 Figma write | 06 | G8 | T37/T39 |
| R19 recovery | 04/05/09 | G1/G6/G7 | T05/T22/T32 |
| R20 extensibility | 04/06/07 | G10 | adapter contracts |
| R21 benchmark | 13 | G10 | controlled experiment |

## Risk register and decision gates

| Risk | Impact | Mitigation / stop condition |
|---|---|---|
| IBM endpoint incompatible with CLI | coding unavailable | G0 real test, authorized alternate; keep read/action product |
| Figma access/rate limit/custom client restrictions | partial source or no writes | scoped read, cache, manual snapshot/handoff, no false live status |
| ADO process/permissions differ | cannot create/transition/merge | discover fields, exact provider policy; no bypass; leave PR in review |
| Agent code executes malicious behavior | host or secrets exposed | mandatory verified sandbox and least credentials; block real coding if unavailable |
| Graph relations inaccurate | misleading impact | evidence, human review, stale/unknown, explicit link first |
| Semantic checks hallucinate | false blockers | candidate labels, deterministic finite checks, human confirmation |
| External writes partial or duplicate | inconsistent project | journal, optimistic version, reconciliation, approved compensation |
| Token cost higher than baseline | budget/time loss | no model browsing, explicit limits, measure, optional RAG only if justified |
| Too much scope before demo | incomplete product | P0-A first, P0-B next; parallel/Figma write stretch; prevalidated fallback |
| Live service outage | failed demo | fixture replay labeled, real receipts from rehearsal, no fake live calls |
| Confidential employer data | policy/legal risk | synthetic dedicated assets, policy approval before real usage |
| Target public deployment with fixture auth | security risk | local only; separate production requirements and review |

## Open verification and decision items

| ID | Close with | Effect while open |
|---|---|---|
| V-01 ADO remote MCP | Redacted proof of supported remote transport, approved auth and a live scoped Boards read. | Blocks ADO MCP acceptance. The locally hosted ADO MCP server and REST are not substitutes. |
| V-02 Figma remote MCP | Redacted proof of custom client access, scopes, real frame content and observable revision/coverage or documented absence. | Blocks Figma MCP acceptance if the core read fails. Preview is independent. |
| V-03 Companions | Separate results for Wiki, Repos and rendered preview; one recorded decision per requested fallback. | Core can proceed; no implicit fallback. |
| V-04 Host and toolchain | Pin exact .NET SDK, Node LTS, package/SDK and PostgreSQL versions after proving local startup and provider compatibility. | Baseline architecture is selected; versions and Windows host setup remain unverified. |
| V-05 Later tracks | Independent AI/CLI/merge/sandbox approvals and capability proof when those tracks are scheduled. | No effect on Local MVP. |

Do not include keys or high-sensitivity identifiers in chat or evidence. Record only approved, sanitized evidence and use the secure setup path for credentials.

## Evidence register (initial)

No application source, runtime tests, provider credentials, live calls, code execution or cloud deployment verified by this specification package. Only official documentation research and design authoring performed. Each roadmap completion must append actual evidence. Demo mockups are conceptual, names and numbers are illustrative. This honest baseline is essential to distinguish the desired product from the current implementation.
