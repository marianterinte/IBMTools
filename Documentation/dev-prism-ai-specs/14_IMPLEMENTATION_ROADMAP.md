# 14 — Implementation roadmap (agent execution checklist)

All tasks initially unchecked. Execute sequentially for DEV PRISM AI itself. Each group has at most eight tasks. Read group specs plus `00_START_HERE.md`, `AGENTS.md` and decision log before work; re-read after compaction. Difficulty tags: SIMPLE = localized; MEDIUM = multiple files with known contracts; HARD = cross-module or integration; COMPLEX = security/orchestration/uncertain external capability. For HARD/COMPLEX expand into implementation subplan with exact evidence before coding. Do not mark [X] without command/result/changed paths or real receipt. Each group ends with a review gate. Owner may stop after P0-A and still have useful tool.

## G0 — Capability spikes and decisions (blocking dependencies)
Mandatory: 04, 06, 09, 10, 12, 16, 18. No production code until interfaces validated; small disposable spikes allowed.

- [ ] G0.1 [SIMPLE] Establish repo and docs; record platform/toolchain versions, scope and authorized sandbox accounts; no secrets.
- [ ] G0.2 [HARD] Probe actual provider (chat, structured output/tool calling, streaming, usage, rate limit) with approved synthetic test; record compatible model/config and cost coverage.
- [ ] G0.3 [COMPLEX] Probe OpenCode CLI in isolated disposable repo using provider, JSON event parsing, permission/cancel, actual edit/test and secret boundaries; fallback Codex only if authorized. If no engine, block G7+.
- [ ] G0.4 [HARD] ADO MCP and REST capability/read tests; discover process types, work item revision checks, PR scope/policy, auth; authorized disposable create/update + reread if allowed.
- [ ] G0.5 [HARD] Wiki read/write proof with version/ETag or code wiki workflow; record fallback.
- [ ] G0.6 [HARD] Figma scoped read/preview and rate limits; test custom client auth, optional disposable write if permitted, else manual handoff. No dependency on Figma write.
- [ ] G0.7 [COMPLEX] Validate sandbox and preview isolation with negative tests (host file, secret, egress, process kill), pick platform; if fails disable real coding.
- [ ] G0.8 [MEDIUM] Document results, pin versions, ADRs and capability report; confirm release gates and blockers with owner.

Gate: actual version/proof or explicit unavailable status for each. Do not fabricate API capability. Read slice may proceed if coding blocked. If Figma read live unavailable, manual snapshot works but live R03 remains unmet.

## G1 — Local foundation and UI shell (P0-A)
Mandatory: 03, 04, 05, 11, 12, 13.
- [ ] G1.1 [MEDIUM] Scaffold .NET solution/modules, React/Vite TS and tests; lock versions, lint, formatting, docs.
- [ ] G1.2 [HARD] PostgreSQL Compose, migrations base entities, connection config, project isolation constraints and test DB.
- [ ] G1.3 [HARD] Local auth/pairing, session, CSRF, Host/origin restrictions, per-project authorization, secure secret adapter and redaction.
- [ ] G1.4 [MEDIUM] REST conventions, ProblemDetails, validation, health, correlation and idempotency foundation.
- [ ] G1.5 [HARD] Durable jobs, leases, outbox and SignalR replay skeleton with restart tests.
- [ ] G1.6 [MEDIUM] UI shell/routes/theme/project list/setup/settings, connection capability state and loading/empty/error patterns.
- [ ] G1.7 [MEDIUM] Bootstrap scripts, README run/test commands, real startup and security tests T01–T03/T33.

Gate: fresh checkout starts local UI/API/DB, persists project, unauthorized access denied. No fake connected state.

## G2 — Normalized sync and real source adapters (P0-A)
Mandatory: 05, 06, 11, 12, 16.
- [ ] G2.1 [HARD] Resource identities/snapshots/change events, cursors, sync lease and coverage, fixture adapter tests.
- [ ] G2.2 [HARD] ADO Boards scoped reads, process discovery, metadata and links; actual live import proof.
- [ ] G2.3 [MEDIUM] Wiki read tree/content/versions, sanitize and snapshot; actual live read proof.
- [ ] G2.4 [HARD] Git local read, commit/tree/diff and optional ADO PR read; path/sensitive file policy tests.
- [ ] G2.5 [HARD] Figma scoped metadata/preview with cache, rate-limit and manual import fallback; actual live read proof if available.
- [ ] G2.6 [HARD] MCP connection manager stdio/HTTP validated capability discovery, safe read tool wrapper; no write yet.
- [ ] G2.7 [MEDIUM] Sync UI status, activity feed and stale/partial/auth states; test T04–T09/T37 read.

Gate: real resources in cache, second sync deduped, offline cache labeled; no model calls, no external writes.

## G3 — Feature Explorer and Brain (P0-A)
Mandatory: 01, 03, 05, 07, 11, 13.
- [ ] G3.1 [MEDIUM] Feature CRUD and graph nodes/edges, project constraints and history.
- [ ] G3.2 [HARD] Explicit relation extraction (ADO links, Figma refs, Git refs) and candidate review, evidence revisions.
- [ ] G3.3 [MEDIUM] Resource list/detail and original links for ADO/Wiki/Git/Figma, source version history.
- [ ] G3.4 [HARD] Feature Explorer tabs, graph view with bounded traversal, inspector, manual linking and review.
- [ ] G3.5 [MEDIUM] Dashboard metrics/activity/freshness, local search and global keyboard navigation.
- [ ] G3.6 [HARD] Decision version/source registry and explicit approve/supersede UI, local policy lifecycle.
- [ ] G3.7 [MEDIUM] Tests T10–T14 including no LLM browse, graph isolation/staleness, restart and screenshots.

Gate: P0-A core read workspace usable. Real data and feature links, no model needed. Demonstrate daily navigation.

## G4 — Watchtower and context engine (P0-A completion)
Mandatory: 07, 08, 05, 15, 13.
- [ ] G4.1 [HARD] Change-triggered scoped analysis jobs with dedupe, graph impact traversal and coverage.
- [ ] G4.2 [HARD] Typed policy catalog WT-001/002/003/006/007, structured rating policy and evidence parsing; candidate/unknown semantics.
- [ ] G4.3 [MEDIUM] Findings state machine, evidence, dismiss/reopen/resolve logic with actual checks.
- [ ] G4.4 [HARD] Watchtower list/detail before/after graph and resolution proposal entry point.
- [ ] G4.5 [HARD] Context builder mandatory constraints, role-specific graph/lexical retrieval, budget + omissions, pack hash and preview.
- [ ] G4.6 [MEDIUM] T13–T17 fixtures, no false resolved findings, no unbounded context, no AI for deterministic checks.

Gate: demonstrate half-star mismatch from changed source and explicit contract. P0-A release; optional AI unavailable still useful.

## G5 — AI chat and reviewable plans (P0-B)
Mandatory: 09, 11, 07, 08, 12, 17.
- [ ] G5.1 [HARD] Provider profile, credential handling, tested chat/stream adapter, usage/limits and cancellation.
- [ ] G5.2 [HARD] Chat threads/messages, bounded read tools, citations and prompt-injection defense; no mutation tools.
- [ ] G5.3 [COMPLEX] Typed plan schema, DAG validation, resource refs and immutable canonical hash, source manifests, dependency/target/risk validation.
- [ ] G5.4 [HARD] Plan draft/review UI, exact diffs, selection constraints, server approval/version/expiry/revoke, stale detection.
- [ ] G5.5 [MEDIUM] Chat UI streaming/reconnect, tool evidence and source inspector, context budget controls.
- [ ] G5.6 [HARD] Optional semantic candidate analyzer (explicit budget) with source ID validation; never auto-approve.
- [ ] G5.7 [MEDIUM] Tests T18–T20/T34, new chat conflict and read-only demo.

Gate: model can propose actual source-grounded plan, cannot execute or approve. No writes until G6.

## G6 — External action executor (P0-B)
Mandatory: 05, 06, 09, 11, 12, 13.
- [ ] G6.1 [COMPLEX] Durable action attempts, approval dispatch/preflight, optimistic versions, unknown reconciliation and bounded retries.
- [ ] G6.2 [HARD] ADO create/update + actual parent-child IDs, process state mapping, revision guards and reread receipts.
- [ ] G6.3 [HARD] Wiki create/update with ETag/current version and reread, sanitized diff.
- [ ] G6.4 [HARD] Cross-system dependencies, partial outcomes, manual handoff and resync; no automatic rollback.
- [ ] G6.5 [MEDIUM] Mission action journal UI, retry/reconcile and audit, actual remote IDs and versions.
- [ ] G6.6 [HARD] One-prompt DishRate backlog + Wiki + approved local decisions; real live dedicated project proof.
- [ ] G6.7 [MEDIUM] T21–T24/T35, simulated timeout/partial and real successful verified writes.

Gate: actual authorized writes and reread, ambiguity safe. This alone can be demoed independently of coding engine.

## G7 — Single coding engine, integration and preview (P0-B)
Mandatory: 10, 12, 05, 09, 15, 17.
- [ ] G7.1 [COMPLEX] Hardened workspace/runner and Git broker, paths, approved profiles, sandbox and process cancellation. Require G0 safety evidence.
- [ ] G7.2 [HARD] Implement validated CLI adapter, event parsing, actual usage/unknown and timeout, fixture runner tests.
- [ ] G7.3 [HARD] Scheduler task/attempt state, leases, contract freeze, single agent job and independent diff validation/commit.
- [ ] G7.4 [HARD] Independent build/test/guard at exact SHA, protected path and policy checks; store evidence.
- [ ] G7.5 [HARD] Local preview profile/health/start/stop, isolation and exact commit; UI link.
- [ ] G7.6 [HARD] Mission Control UI DAG/logs/checks/diff and cancellation/restart recovery.
- [ ] G7.7 [HARD] Approved push + draft PR creation and work item link, actual reread, no merge yet.
- [ ] G7.8 [MEDIUM] T25/T28–T34/T36 PR; actual single feature implementation, preview, draft PR.

Gate: P0-B release. One real safe coding task and evidence; if CLI unavailable explicitly block this gate, retain earlier capabilities.

## G8 — Parallel agents and controlled completion (P1)
Mandatory: 08, 09, 10, 12, 13, 15.
- [ ] G8.1 [COMPLEX] DAG scheduler concurrency, dependency readiness, path ownership, budgets and conflict preflight.
- [ ] G8.2 [HARD] FE/BE worktrees/copies from frozen baseline, optional QA independent; prove actual concurrent timestamps.
- [ ] G8.3 [COMPLEX] Integration commit order, conflict stop/recovery, rerun independent tests and contract guard; main advance handling.
- [ ] G8.4 [HARD] Separate explicit merge approval, remote SHA/branch policy check, verified merge and ADO update/Wiki follow-up/policy promotion.
- [ ] G8.5 [HARD] Figma optional write adapter only if G0 verified safe capability, else complete manual handoff flow.
- [ ] G8.6 [MEDIUM] T26–T30/T36–T39, restart, partial recovery and no unauthorized merge.

Gate: full parallel demo, PR merge conditional on real policy. Never fake unsupported Figma write.

## G9 — Demo, polish and release readiness
Mandatory: 02, 03, 13, 15, 18.
- [ ] G9.1 [MEDIUM] DishRate seed/fixtures and approved preview profile, real ADO/Figma demo resources, no employer data.
- [ ] G9.2 [HARD] Rehearse F01–F06 twice, capture actual evidence and plan variants, restart and guard scenario.
- [ ] G9.3 [MEDIUM] Accessibility/responsive checks, UI screenshots, error/empty states, performance fixture and safe retention.
- [ ] G9.4 [MEDIUM] Live service outage fallback clearly labeled, reset script only local demo state, no automatic remote delete.
- [ ] G9.5 [MEDIUM] Run full tests, review security, docs/ADR/manifest, final limitations and evidence summary.
- [ ] G9.6 [SIMPLE] Prepare presentation runbook and export redacted sample evidence, record exact release capabilities.

Gate: demo readiness report states real/fixture/conditional capabilities and remaining blockers. No claimed deployment or token savings without evidence.

## G10 — Optional evolution (not MVP)
- [ ] G10.1 [HARD] Fair context benchmark, optional pgvector only if gains justified.
- [ ] G10.2 [HARD] Jira/Confluence adapters and capability/security tests.
- [ ] G10.3 [HARD] Own Project Brain MCP server and optional VS Code companion.
- [ ] G10.4 [COMPLEX] Team/remote deployment, RBAC, webhooks, pipelines, cloud target deployment separately designed and approved.

## Evidence template (append below each task on completion)

```
Status: PASS / BLOCKED / PARTIAL
Changed files / commit:
Commands and exit codes:
Tests (fixture vs live):
Evidence path / actual provider receipt (redacted):
Security and negative checks:
Remaining risk / deviations / owner decision:
```
