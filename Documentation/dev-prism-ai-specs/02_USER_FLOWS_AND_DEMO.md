# 02 — User flows and demonstration

## F01: setup and connection

1. Start local backend + database + web app. First launch authenticates local operator; show setup checklist and privacy note. Create project `DishRate`, slug and local root (choose registered path through backend allowlist), configure repo remote and default branch, approved preview profile.
2. Choose AI provider profile: OpenAI, compatible endpoint or IBM provider if proven; supply endpoint/model/secret reference, test with minimal structured output and optional tool call. Distinguish chat model from coding engine credentials. An AI test failure must not block browsing.
3. Add ADO organization URL, existing project/repo/team and auth. Discover work item types, states, wiki ID, repo branches; run minimal read and tool discovery. Write ability remains unverified until dedicated test with explicit consent. No assumption that URL implies authentication.
4. Add Figma design URL with file/frame scope. Authenticate and test metadata/preview. Display `Read`, `Write: unavailable/unverified/available` and rate-limit information. A failed Figma connection must not prevent ADO/Git browsing.
5. Add Git local root and remote. Validate canonical path, Git executable, clean worktree status, remote allowlist and branch. Test read only. Configure CLI executable path and version, container execution profile and actual provider capability.
6. Choose sync interval (manual default plus optional 5–15 min for ADO/Git; Figma slower according to limits); first import runs with progress counts. Show per-source status and imported count, not invented percentages. Setup can be resumed; secrets only via secure storage.

Exit: project home shows actual source health, last success and any unavailable capabilities. Settings available later without requiring full reset.

## F02: daily navigation

Open Dashboard, select project, see activity since last visit. Click “Dish Rating”, switch tabs to task, design, code, docs, decisions, tests and changes. Click any resource for content + immutable revision + original link; use “Link resource” to establish a typed relationship. Browse snapshots without network/LLM calls except user-triggered sync or missing preview fetch. Show old data with stale label when offline. Filter graph by relation type and depth; inspect edge provenance. Search by work item ID, title, exact symbol, path or decision. Select a source change to view before/after and affected feature. If no links exist, show empty not “consistent”.

## F03: from idea to backlog

Prompt: “Create a small public-facing DishRate app where users rate dishes at specific restaurants. Plan React, .NET and PostgreSQL. Create a backlog in the configured ADO project and a business-rules page in its Wiki. Do not implement yet.”

Assistant uses read tools to learn available ADO types and project sources. Proposes architecture, feature list and assumptions; asks operator to approve specific business decisions (one rating per user/menu item; public aggregate after at least three ratings; initially integers 1–5; no reviewer email to owner). Draft feature hierarchy and individual work items; map local temporary keys to real IDs after writes. Wiki creation depends on agreed decisions and can reference actual work item IDs only through a typed result reference approved in the plan. Review screen shows source targets, descriptions, acceptance criteria and explicit “Create work items”, “Create page”, “Approve decisions” effects. Approval commits local policy decisions and authorizes bounded external writes; remote calls journaled separately. If ADO succeeds but Wiki fails, show partial outcome and retry/reconcile only the failed step. Link returned remote IDs to feature. No assumed new ADO organization, billing, remote repo or automatic deployment.

## F04: initial feature implementation

Start from dedicated seed repo (DishRate reference). Import a Figma rating frame and link it to initial rating feature. Ask: “Implement the approved integer rating feature for ADO item [select actual ID]. Use the linked design. Run frontend/backend agents in parallel where safe, and prepare a preview and PR.”

1. Resolve work item, actual commit, frame version, accepted decisions, existing tests and source coverage. If data stale, refresh; if critical unavailable, block or explicitly allow reduced read-only planning, never claim verified.
2. Memory Guard evaluates typed rules and semantic conflicts. User reviews plan, roles, budget, file ownership, base SHA, test suite and external actions. Approve execution. Contract phase produces `contracts/rating.openapi.yaml` or versioned artifact, approved API request/response semantics and tests; freeze contract version and common commit before branching.
3. Scheduler launches frontend and backend tasks in separate isolated worktrees/copies after contract ready; optional QA only writes test directory. Mission Control shows start/stop events, logs, token usage if available and changed paths. If backend discovers contract change, pause dependents and replan; no unilateral change to frozen contract.
4. Integration worker combines commits in deterministic order, reports conflicts, reruns independent tests and guard. Show actual diff and test output. Launch local preview in isolated runtime from integration SHA; show URL and start/stop state. Preview must not run arbitrary command from ticket.
5. Operator approves push/PR after reviewing evidence. Create PR linked to actual ADO items; use draft when supported. Merge only after branch policies + explicit separate merge approval tied to current base/head. Update ADO and Wiki after approved merge action (or leave status “In review” if not merged). Publish approved decisions and links, not every agent statement.

## F05: show why centralization matters

Change linked Figma frame in Figma to allow 4.5 stars and/or update the ADO acceptance criteria to include half-star ratings. Do not edit code. Trigger sync. On dashboard show Figma/ADO changes and Feature Explorer affected resource graph. Watchtower compares the previous frame metadata/explicit requirement, current OpenAPI type `integer` and active approved policy BR-006. It may classify visible half-star control as semantic candidate until supported by explicit design text/contract evidence. Open finding: source versions, linked paths, affected UI, API, tests, Wiki, DB. Show the real before/after. No coding agent launched yet.

Ask “Support half-star ratings everywhere; update the affected docs and tasks, implement and test, then show me the result.” Review plan: policy amendment from integer to steps of 0.5, ADO criteria update, code/migration, tests, Wiki update after merge, optional design update if necessary (already changed in this example). A new policy version is proposed; it becomes approved target for this mission after human authorization; old rule remains active for baseline until verified implementation and explicit promotion. Display target/baseline distinction. Validate 0.5 step, 1–5 range, preserve previous ratings, migration and chart/aggregate behavior. Implement independent FE/BE with frozen contract and integration tests. If Wiki write fails after merge, show `PartiallySucceeded`, pending reconciliation. If Figma write unsupported, create manual handoff with intended change and verify after external edit.

## F06: new conversation, conflicting requirement

Restart app and open a new chat, no prior messages loaded. Request “Export customer reviews with customer email for owners.” Retrieve approved BR-012 and its source, current relevant API and linked future design/task. Show blocker or semantic warning with exact evidence, affected sources and choice: keep privacy rule and remove fields; explicitly amend policy (requires owner decision; do not encourage privacy violation); scoped exception subject to policy and permission. For demo choose privacy-safe export. Deterministic pre-merge guard checks owner response/CSV headers against approved forbidden aliases; test verifies no identifier data. Do not claim this proves total privacy. Show source decision predates new ticket; cite IDs/revisions. Demonstrate cross-source guard even if Cursor might also detect it.

## F07: restart, failure and intervention

Cancel running mission: stop launching nodes; terminate owned processes safely; preserve commits and logs; external calls in flight become `Unknown` until reconcile. On backend restart, queued jobs rehydrate from DB, previously running jobs become `Interrupted` unless proven alive and attachable. UI replay events from cursor. Resuming starts a new attempt with current versions/approval check; never blindly rerun writes or merges. Operator may resolve Git conflicts manually in integration workspace and submit new evidence for checks; blocked graph nodes remain blocked. A stale plan must be regenerated and reapproved.

## Suggested 12–18 minute presentation (timing target, not measured)

0–2 min: problem fragmentation; show connected workspace and real source links. 2–5: one prompt to approved ADO backlog and Wiki; show receipts. 5–8: select feature and launch prepared implementation; show actual concurrent starts and worktree diffs, real preview or previously recorded live evidence if slow. 8–12: change design/criteria, sync, graph impact, review half-star cross-system plan. 12–15: restart/new chat and privacy conflict; explain versioned rule and guard. 15–18: cost/coverage, architecture, limitations and future work. Keep a prevalidated fixture replay clearly labeled if remote services unavailable; never disguise recorded/fixture data as live. Public deployment is optional after demo, not a prerequisite.
