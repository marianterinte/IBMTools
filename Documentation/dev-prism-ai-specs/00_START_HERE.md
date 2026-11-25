# 00 — Start here: canonical instructions

## Mission

Deliver a Local MVP that runs on the operator's laptop and imports real Azure DevOps Boards and Figma artifacts through authorized remote MCP servers. Inspect them together in Feature Explorer, attach evidence, approve a local decision, and recover the same state after restart. Imported DishRate content is synthetic, but the projects, accounts and online provider calls are real. No AI provider is required. The longer-term product can propose and execute approved cross-system changes; do not design the first presentation around those later capabilities.

The pack describes the desired target, not existing source code. Inspect the actual repository before writing code. Do not assume a prior scaffold exists. Do not use unrelated project files as requirements. The example project DishRate is distinct from the control plane.

## Authority and consistency

1. Explicit owner decisions override proposed defaults in this pack; record them in `docs/spec/18_TRACEABILITY_AND_DECISIONS.md` or an ADR. Security and external provider restrictions cannot be waived by model output.
2. This file defines invariant behavior. The product document defines scope; architecture, data and API documents define technical contracts. The roadmap defines order. When they disagree, stop that affected task, record discrepancy and propose a concrete resolution; do not invent two competing contracts.
3. Treat design examples, task IDs, mock metrics and sample JSON as examples. Do not hardcode their values in production logic. Use fixtures only in clearly labeled demo mode.
4. Verify vendor behavior using installed versions, official docs and capability tests in group G0. Never assert a connector or agent works just because a package exists.

## Mandatory invariants

| ID | Invariant |
|---|---|
| INV-01 | Browsing cached task/design/code/doc/graph views uses no LLM. Explicit analysis may use a model and disclose cost/coverage. |
| INV-02 | External sources retain authority for external content; project policies approved locally are authoritative for DEV PRISM AI gates. Keep source, revision, freshness and permissions visible. |
| INV-03 | All links and findings distinguish verified, inferred, rejected, stale and unknown. A missing source never becomes a negative assertion. |
| INV-04 | No external write without server-side authorized approval of exact target, payload/diff, version, operation and risk. Read-only imports need connection consent, not individual action approval. |
| INV-05 | No blind retry of ambiguous writes, no silent partial success, no fictitious rollback across systems. Journal and reconcile. |
| INV-06 | AI and imported text are untrusted; model suggestions never directly mutate remote resources, Git main, credential storage or policy state. |
| INV-07 | Isolated agent workspaces, bounded process execution, explicit path and command policies, credentials separated from child processes. Git worktrees alone are not a sandbox. |
| INV-08 | Merge main requires independent check evidence, current target SHA, provider policy compliance and explicit human approval. No force push or bypass. |
| INV-09 | Every record and request scoped to project and operator authorization; no cross-project leakage. No secret in logs, prompts, git history, UI, fixtures or plain DB columns. |
| INV-10 | Persistent jobs, execution events and action receipts survive service restart. Interrupted runs must reconcile before resuming. |
| INV-11 | Honest status, no fabricated percentages, costs, provider IDs, successful calls, test results or model reasoning. Show observed state and unavailable data. |
| INV-12 | No Azure cloud hosting requirement. Demo only uses dedicated non-confidential data and user-authorized external services. |

## Scope gates

**P0-A = Local MVP (G0–G3).** Local single-operator app and PostgreSQL; authorized read-capability probes and manual import via remote MCP from ADO Boards and a scoped Figma frame; Feature Explorer with local links, immutable content history, freshness, and an explicitly approved local decision. Reimport without duplicates, show a changed source revision, and recover browseable state after restart. The app does not write to any provider. Local project settings, links and decision approvals are still editable.

**MVP companion capabilities:** ADO Wiki and ADO Repos references are separate read capabilities; the Figma rendered preview is separate from frame context. Include them in the demo only when proven. For an auxiliary operation only, a specific REST/API fallback may be added after owner approval and documented per capability. Never substitute it for a required MCP read, silently change routes, or present fallback/fixture evidence as remote MCP proof. See [connector matrix](06_CONNECTORS_AND_SYNC.md#local-mvp-capability-matrix).

**Post-MVP:** G4 adds structured Watchtower/impact and context builder; G5–G7 (P0-B) add chat, authorized external actions and isolated single-agent runs; G8 (P1) adds parallelism, controlled merge and optional Figma write; G9 describes the expanded demo. G10/P2 covers vector retrieval, additional connectors, deployments and team use. These are design targets, not dependencies of the Local MVP. Basic change history belongs in MVP; automatic drift classification does not.

## Agent execution protocol

Work in roadmap order and only after the owner explicitly authorizes implementation. Use the task contract in [AGENTS.md](AGENTS.md); for HARD/COMPLEX work, add affected contracts, failure cases and recovery before editing. After compaction or restart, re-read this file, the current group, decision log and affected specs. Do not start post-MVP work to compensate for a blocked required MCP capability.

Use `[ ]` / `[X]` for actual roadmap status. Add evidence under completed tasks: changed paths or commit, observable verification result, source or version, relevant failure case and limitations. `Verified fixture` never satisfies required remote MCP acceptance. A task with missing required evidence remains unchecked and names the blocker. The MVP does not require new FE/BE unit or integration test suites; the manual acceptance gates and security boundary checks still apply. Do not commit credentials or personal data.

## When implementation is authorized

- Prove real remote read for ADO Boards and Figma using dedicated, authorized online assets. Record whether Wiki, Repos and preview are available independently. Do not start with AI, CLI or write probes.
- Implement the local foundation, bounded manual import and feature-first read experience in G1–G3. Keep fixtures separate from connected data.
- Close Local MVP only after the manual acceptance run in [13_TESTING_AND_ACCEPTANCE.md](13_TESTING_AND_ACCEPTANCE.md#local-mvp-acceptance-run-m01m10). Expand scope only in a separate decision.
