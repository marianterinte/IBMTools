# 03 — UI/UX specification

## Design system

Desktop-first responsive application, English UI. Product identity DEV PRISM AI, small prism mark, dark default theme and optional light. Visual references from earlier concepts are inspirational, not pixel contract. Developer dashboard aesthetic: navy charcoal background #10131C, surfaces #171C29 / #202638, text #E8ECF5, muted #A5AFC2, violet #9B87F5, blue #60A5FA, green #34D399, amber #FBBF24, red #F87171. Use contrast-checked tokens; do not depend on color alone. Fonts system sans and monospace for IDs/code. Density comfortable, optional compact rows; 8px spacing grid; radius 8–12px. Keyboard accessible tabs/dialogs, visible focus, semantic headings, tooltips on icons, aria-live for event updates, no excessive animations. Respect reduced motion. Avoid progress bars when no denominator.

## Shell and routes

Left rail 220px collapsible (project selector, Dashboard, Features, Resources, Activity, Brain, Watchtower, Missions, Chat, Settings). Top bar breadcrumb, search (Ctrl/Cmd+K), sync status, AI/provider indicator, project selector optional. Right inspector 340–420px resizable on graph/explorer. Main canvas scroll independent. On narrow viewport use drawer and stacked inspector; support 1280x720 at minimum demo resolution, target 1440x900. Persist local display preferences, not secrets. All deep links carry project scope; opening external link uses new tab and safe scheme.

| Route | Main purpose | Right inspector |
|---|---|---|
| `/projects` | Project cards, create/import | None |
| `/projects/:id/setup` | Wizard, capability tests | Prerequisites/help |
| `/projects/:id/dashboard` | Overview, activity, findings, missions | Selected item |
| `/projects/:id/features` | Feature list, search, coverage | Summary |
| `/projects/:id/features/:featureId` | Feature Explorer | Resource/edge detail |
| `/projects/:id/resources/:kind` | ADO / design / code / docs | Detail, source link |
| `/projects/:id/brain` | Decisions, graph, links | Rule/evidence inspector |
| `/projects/:id/watchtower` | Findings and impact | Comparison/resolution |
| `/projects/:id/chat/:threadId?` | Scoped conversation and plan | Context/sources |
| `/projects/:id/missions/:missionId` | Plan, DAG, logs, changes, preview | Node/action evidence |
| `/projects/:id/settings` | Connections, provider, policy, budget, workspace | Test diagnostics |

## S01: project setup

Stepper: project basics; ADO/Git; Figma; AI/engine; safety/preview; review & import. Required to create local project: name, local root under registered workspace (or read-only empty project), owner identity. Connection fields described in 06/12. Secret field write-only, last four characters only if safe or simply “Configured”. Test buttons perform real calls with bounded timeout, show permission/capability results and sanitized error. Disable writes until verified. “Skip for now” for noncritical provider. “Import now” starts sync, can cancel. Credentials never echoed. Reopen and rotate without losing resource history. Test read and test write separate; test write explains exact disposable mutation before approval.

## S02: dashboard

Four to six compact cards: open work items, linked features, findings (verified/candidates), active missions, source health. Count definitions: imported scope only, `Open` normalized excluding Done/Removed, counts with last sync; unknown displayed as dash not zero. Main column latest activity timeline with provider icon, event type, entity title, before/after revision, time UTC localized; select opens inspector. Secondary column attention queue (severity, confidence, affected feature, reason) and active missions. Source health row with last successful sync, last attempt, error, retry. “Sync all” disabled while job active unless request queued. Quick prompt opens chat with project context. No auto AI call on loading dashboard.

## S03: Feature Explorer (flagship)

Left list of features with status `Draft / Active / Delivered / Archived`, linked coverage indicators, findings. Create feature manually or from approved plan; features are internal entities, not equivalent to ADO Feature type. Header feature title, purpose, owner optional, status, coverage and freshness. Tabs `Overview`, `Tasks`, `Design`, `Code`, `Docs`, `Decisions`, `Tests`, `Changes`, `Graph`. Overview grouped resource cards + latest finding + linked mission. Do not infer delivery just from task count; show evidence. Empty group: “No linked design yet” + link resource. Each resource row has title, type, revision, provider, verified/inferred link chip, last sync, open original. Selection opens right inspector: body excerpt or full cached view, metadata, relation proof, source version, incoming/outgoing links, related findings and link/remove action. “Analyze impact” is explicit model/algorithm action with coverage and estimated cost disclosure. “New mission” preselects feature and immutable source refs.

Wireframe:

```text
┌ DEV PRISM AI / DishRate / Dish Rating ────────────── Search ─ Sync ┐
│ Nav │ Feature list │ Dish Rating  [Needs attention] │ Inspector   │
│     │              │ Overview Tasks Design Code Docs │ ADO #142   │
│     │ Dish Rating  │ Tasks 4/5     Design v12        │ rev 8       │
│     │ Leaderboard  │ Code 6 paths  Docs 2            │ criteria    │
│     │ Owner Export │ Decisions 3   Tests 18/20       │ linked via  │
│     │              │ [Half-star mismatch]           │ evidence    │
│     │              │ [Analyze impact] [New mission]  │ Open source │
└──────────────────────────────────────────────────────────────────┘
```

## S04: resource explorers

ADO: filter type/status/area/assignee/text, paginated list, detail sanitized rich text, acceptance criteria, parent/child links, work item history available from snapshots, “Link to feature”, “Propose update” opens action draft (not direct provider editing). Create item button uses same approval engine. Display actual process-specific type/state names, normalized state secondary. Figma: selected files/pages/frames in tree, preview zoom/pan, source version and frame ID, extracted text/metadata, original link. Never pretend full interactive Figma editor; disable canvas write if unavailable. Render safe cached thumbnails, refresh on request; expired preview placeholder with last success. Git: tree at selected commit, branch selector, file view with line numbers/monospace, diff before/after, commit/PR list with provider status. Binary/large files metadata only. No arbitrary terminal field. Docs: Wiki page tree, sanitized markdown preview, version compare, search headings, proposed edit side-by-side. All content treated as untrusted; do not render raw HTML/scripts.

## S05: Brain / graph

Tabs Decisions, Graph, Link Review, Context Packs. Decision table ID, kind, scope, lifecycle, effective vs target version, source and enforcement. Create/edit creates draft version, approve explicitly, supersede with reason. Show history and affected resources. Graph default root selected feature, depth 1–2, max 150 nodes (config bounded), expand per node, filter relation and resource types; use React Flow, deterministic layout and legend. Node colors by type, edge dashed inferred, solid verified, stale marked; accessible alternative list. Right panel displays relation type, evidence excerpt/ref, confidence class and human confirmation/reject; no raw model confidence percentage required. Unknown edges are absent, not proof of no relationship. Context Pack view shows included rules and resource excerpts, omitted reason, estimated tokens, actual usage if reported; copy/export sanitized approved pack. Search/index is local and not a model call.

## S06: Watchtower

Finding list grouped by feature, severity `Blocker / High / Medium / Info`, evidence class `Verified / Candidate / Unknown`, status `Open / Acknowledged / Resolved / Dismissed / Stale`. Show coverage and last scan. Detail has left before/right after for source revision, rule excerpt, affected graph, proof references, severity rationale and recommended action. Actions: acknowledge, dismiss with reason and version scope, request refresh, create proposed remediation mission. Verified policy violation can block; semantic candidate requires review; stale critical source blocks execution only under explicit gate policy. Resolution reruns check; no manual “resolved” pretending fixed. Reopen on relevant source change. Show zero findings vs no checks executed distinctly.

## S07: chat and plan approval

Chat header project/feature, model, selected scope, budget. Thread list; new chat does not load full history. Attach explicit source refs, no unrestricted local upload in MVP. Messages stream text safely; tool cards show read/analysis progress, source refs and actual result. Write requests generate plan card, never execute implicitly. Source citations clickable to immutable snapshot inspector. Plan view: steps grouped by source, dependencies, operation, target, before/after diff, precondition/version, risk, estimated cost, rollback/recovery limitations. User can deselect only optional independent steps; invalid dependent selection rejected. Edit drafts produces new plan version/hash. Approve exact plan via dialog with scope and risks; typed separate approval for merge; clear cancel. UI distinguishes “Proposed”, “Approved”, “Running”, “Partial”, “Unknown”. Stale plan prompts refresh/replan. Human approval is required even if model says safe. Streaming interruption offers reconnect; no duplicate prompt submission (client idempotency key).

## S08: Mission Control

Header mission title, version, stage, budget, base SHA, controls cancel/pause/resume where permitted. Tabs Plan, Agents/DAG, Activity, Changes, Checks, External actions, Preview. DAG nodes show dependency state and role, start/end, attempt, workspace branch, observed events, no fabricated 70% progress. Selecting node opens logs (redacted, virtualized, tail/pause), output artifact references, diff, error. Agent card shows actual model/engine, duration, tokens known/unknown, exit code; no hidden reasoning. Integration tab shows merge order, conflicts, source HEAD, test results with actual commands and logs. PR button requires validated evidence and approval; merge separate. External action table shows per-step target/revision/receipt and retry/reconcile controls. Preview tab uses link to allowlisted localhost port; optional iframe only if isolated origin and sandbox policy permit; otherwise open external browser. Start/stop status and SHA. Display previous run stale if SHA changed.

## S09: settings and global states

Connections list per source with Read/Write capability matrix, disabled reason, test/reconnect/disable. Provider profiles expose model and usage, budget defaults, no raw key. Workspace path registration, engine version, sandbox check, allowed preview profiles. Policies and sync interval, retention, delete project (local-only, explicit confirmation; no remote deletion). Audit log export redacted. Global: loading skeleton, empty, stale, rate limited with retry time, auth required, forbidden, partial, offline, unexpected error with correlation ID. Buttons never fake success; real API mutation result or clear fixture mode banner. React error boundary and reconnect indicator. Keyboard tab order, accessible dialogs with focus restoration, text equivalents for graph, at least 44px primary touch target where practical.
