# 14 — Implementation roadmap (agent execution checklist)

Start only with explicit implementation authorization. Follow the task contract in [AGENTS.md](AGENTS.md). Read the canonical scope, decision log and listed specs before each group. HARD/COMPLEX items need a small contract/recovery/evidence plan. G0–G3 deliver the Local MVP. Later groups require a separate release decision and do not repair a missing required remote capability. All tasks below are unchecked until evidence exists; no mandatory new FE/BE test suites for Local MVP.

## G0 — Prove remote read capabilities (Local MVP)
Mandatory: 04, 06, 12, 16, 18. Do not code a production provider mapping before probing; small authorized disposable verification is allowed.

- [ ] G0.1 [SIMPLE] Record target laptop, exact available versions and permitted online synthetic scopes; do not copy credentials into artifacts.
- [ ] G0.2 [HARD] Validate ADO Boards remote MCP auth and scoped usable work item read (MVP-SP1).
- [ ] G0.3 [HARD] Validate the Figma remote MCP client flow and usable selected frame context (MVP-SP2).
- [ ] G0.4 [MEDIUM] Record independent Wiki, Repos and preview capabilities, schemas, coverage and explicit fallback decisions, if any.
- [ ] G0.5 [MEDIUM] Pin validated .NET/Node/PostgreSQL/MCP tooling and record HTTPS/vault/local packaging approach (MVP-SP4).
- [ ] G0.6 [MEDIUM] Publish the redacted capability report, contract assumptions and unresolved blockers.

Gate: both required core reads proven remote with actual scoped provider data. Keep this gate blocked if either is unverified. Unrelated local foundation work may proceed without claiming connector success; no fake MCP or implicit REST replacement. No model, write permission, CLI or sandbox probe is needed at G0 for this release.

## G1 — Local foundation and UI shell (Local MVP)
Mandatory: 03, 04, 05, 11, 12, 13.
- [X] G1.1 [MEDIUM] Scaffold Minimal API modules and React/TypeScript/Vite with local same-origin packaging; pin versions and existing build/lint checks. Evidence: `8eaaf69`; Release build, web lint/type/build, verified loopback HTTPS, truthful unready status and API/media 404 behavior.
- [X] G1.2 [HARD] Dedicated PostgreSQL (native or Compose)/migrations with project keys, connection config versions and explicit DB bootstrap. Evidence: `76d275d`; first migration applied, second application was a no-op, orphan connection rejected, project survived a database restart.
- [ ] G1.3 [HARD] Enforce local loopback HTTPS/Host/Origin/Fetch Metadata and project scope without app login; implement provider vault and redaction. Local boundary and native DB smoke evidence are recorded in the [run instructions](../../README.md); provider storage and access evidence are still open.
- [ ] G1.4 [MEDIUM] Establish OpenAPI DTO and HTTP error contracts, auth/readiness and scope-aware query keys.
- [ ] G1.5 [HARD] Persist bounded import jobs and interruption recovery; one local worker and status polling, no outbox/SignalR requirement.
- [ ] G1.6 [MEDIUM] Ship feature/connection/sync navigation with truthful setup/error states and repeatable local instructions.

Partial evidence: `9cb9c3a` and `97a3b5f` for local authentication and scope; `4e6fd69` for OpenAPI and navigation; `95aeaa1` for durable history and recovery. Provider vault, import execution and the complete acceptance walkthrough are still open. See the [repository evidence and setup](../../README.md).

Gate: repeatable local startup, persistence, no pre-pair project data, and relevant boundary checks from M01/M09; no fake connected state.

## G2 — Import real scoped artifacts (Local MVP)
Mandatory: 05, 06, 11, 12, 16.
- [ ] G2.1 [HARD] Implement scoped identities, normalized immutable versions, observations, deduplication and checkpoints.
- [ ] G2.2 [HARD] Implement the validated remote MCP client, safe profile auth, scope, tool mapping, response validation and provenance for Boards.
- [ ] G2.3 [HARD] Add the validated Figma remote mapping for scoped frame context, independently from preview retrieval.
- [ ] G2.4 [MEDIUM] Implement only proven companion reads as scoped; document missing auxiliary capabilities and omit unapproved fallbacks.
- [ ] G2.5 [HARD] Show run/coverage states and enforce disconnection/denial visibility across histories and caches. Verify relevant M02/M03/M05/M06/M08 cases.

Gate: actual remote MCP source data in cache for both required providers, second sync deduped, offline cache labeled, and no external writes or model dependency.

## G3 — Feature Explorer and local decisions (Local MVP release)
Mandatory: 01, 02, 03, 05, 07, 11, 13.
- [ ] G3.1 [MEDIUM] Create/list features and locally attach/detach a resource with immutable evidence references.
- [ ] G3.2 [MEDIUM] Show grouped resources, original links, scope/route/freshness, detail and before/after history; no automated drift claim.
- [ ] G3.3 [HARD] Draft, approve and supersede versioned local decisions; preserve source authority, audit, and hidden-evidence semantics.
- [ ] G3.4 [MEDIUM] Execute M01–M10 on the target laptop, capture redacted evidence and rehearse the Local MVP flow.

Partial evidence: `a908caa` for local feature metadata and `fe84e37` for versioned decisions surviving process restart. These do not verify imported evidence, attachment, or the live demonstration. Required read proofs and their remaining prerequisites are listed in the [remote read runbook](../local-mvp-remote-read-runbook.md).

Gate: Local MVP release requires the required acceptance checks in [13_TESTING_AND_ACCEPTANCE.md](13_TESTING_AND_ACCEPTANCE.md#local-mvp-acceptance-run-m01m10). Close missing companion capabilities explicitly rather than silently relabeling them as verified.

## G4 — Watchtower, graph and context engine (post-MVP)
Mandatory: 07, 08, 05, 15, 13.
- [ ] G4.1 [HARD] Extend explicit graph relations beyond feature membership; show bounded traversal and evidence. Add change-triggered scoped analysis with dedupe and coverage.
- [ ] G4.2 [HARD] Typed policy catalog WT-001/002/003/006/007, structured rating policy and evidence parsing; candidate/unknown semantics.
- [ ] G4.3 [MEDIUM] Findings state machine, evidence, dismiss/reopen/resolve logic with actual checks.
- [ ] G4.4 [HARD] Watchtower list/detail before/after graph and resolution proposal entry point.
- [ ] G4.5 [HARD] Context builder mandatory constraints, role-specific graph/lexical retrieval, budget + omissions, pack hash and preview.
- [ ] G4.6 [MEDIUM] T13–T17 fixtures, no false resolved findings, no unbounded context, no AI for deterministic checks.

Gate: demonstrate a half-star mismatch with structured evidence; Local MVP remains independently useful without it.

## G5 — AI chat and reviewable plans (P0-B)
Mandatory: 09, 11, 07, 08, 12, 17.
- [ ] G5.1 [HARD] Prove an authorized model with SP-01, then implement provider profile, credential handling, chat/stream, usage/limits and cancellation.
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
- [ ] G6.2 [HARD] Prove permitted write semantics with authorized SP-03, then add ADO create/update, actual parent-child IDs, state mapping, revision guards and reread receipts.
- [ ] G6.3 [HARD] Wiki create/update with ETag/current version and reread, sanitized diff.
- [ ] G6.4 [HARD] Cross-system dependencies, partial outcomes, manual handoff and resync; no automatic rollback.
- [ ] G6.5 [MEDIUM] Mission action journal UI, retry/reconcile and audit, actual remote IDs and versions.
- [ ] G6.6 [HARD] One-prompt DishRate backlog + Wiki + approved local decisions; real live dedicated project proof.
- [ ] G6.7 [MEDIUM] T21–T24/T35, simulated timeout/partial and real successful verified writes.

Gate: actual authorized writes and reread, ambiguity safe. This alone can be demoed independently of coding engine.

## G7 — Single coding engine, integration and preview (P0-B)
Mandatory: 10, 12, 05, 09, 15, 17.
- [ ] G7.1 [COMPLEX] Require then-current SP-02/SP-05 safety and engine evidence, then implement the hardened runner and Git broker. A Local MVP provider probe is not sandbox proof.
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
- [ ] G8.5 [HARD] Figma optional write adapter only after separately authorized SP-04 proof of safe write capability, else complete manual handoff flow.
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
Outcome/status: Verified locally / Verified live / Verified fixture / Blocked
Changed files / commit:
Observable condition and actual result:
Relevant failure/degraded check:
Existing commands and exit codes, if used:
Evidence reference / provider proof (route, revision, redacted):
Remaining risk / deviations / owner decision:
```
