# DEV PRISM AI — implementation specification pack

**Version:** 1.1 | **Date:** 2026-11-25 | **Status:** expanded design baseline; provider/host proofs and implementation pending

**Project Resource Intelligence & Synchronization Management**

One project. Every perspective. One command center.

DEV PRISM AI is a local-first engineering command center that unifies Azure DevOps, Figma, repositories and project documentation into a feature-oriented workspace, detects cross-system inconsistencies, and executes approved changes through AI-assisted workflows and existing coding agents. It is an independent prototype intended for a technical demonstration at IBM, not an official IBM product or a claim of IBM approval.

## How to use this pack

Start with [00_START_HERE.md](00_START_HERE.md), [01_PRODUCT_AND_SCOPE.md](01_PRODUCT_AND_SCOPE.md) and [18_TRACEABILITY_AND_DECISIONS.md](18_TRACEABILITY_AND_DECISIONS.md). For the Local MVP, read the `Local MVP` sections first; older broader design sections describe later phases. When implementation is authorized, use [AGENTS.md](AGENTS.md) and [14_IMPLEMENTATION_ROADMAP.md](14_IMPLEMENTATION_ROADMAP.md) to select the active group. The specifications and UI are in English for consistent terminology. Design examples and later-stage contracts are not completed features.

| File | Purpose |
|---|---|
| [00_START_HERE.md](00_START_HERE.md) | Canon, scope, invariants, execution protocol |
| [01_PRODUCT_AND_SCOPE.md](01_PRODUCT_AND_SCOPE.md) | Personas, requirements, differentiation, priorities |
| [02_USER_FLOWS_AND_DEMO.md](02_USER_FLOWS_AND_DEMO.md) | End-to-end DishRate demonstration and operational flows |
| [03_UI_UX_SPEC.md](03_UI_UX_SPEC.md) | Screen-by-screen UI, behavior, visual language |
| [04_ARCHITECTURE.md](04_ARCHITECTURE.md) | Local .NET + React architecture, module boundaries, runtime |
| [05_DATA_MODEL.md](05_DATA_MODEL.md) | PostgreSQL schema, constraints, state machines, retention |
| [06_CONNECTORS_AND_SYNC.md](06_CONNECTORS_AND_SYNC.md) | ADO, Figma, Git, Wiki, MCP, synchronization |
| [07_PROJECT_BRAIN.md](07_PROJECT_BRAIN.md) | Graph, decisions, evidence, retrieval and context budget |
| [08_WATCHTOWER_AND_GUARD.md](08_WATCHTOWER_AND_GUARD.md) | Change monitoring, impact, policy checks, conflict resolution |
| [09_CHAT_AND_ACTIONS.md](09_CHAT_AND_ACTIONS.md) | AI providers, chat, typed tools, plans, approval and action journal |
| [10_AGENT_ORCHESTRATION.md](10_AGENT_ORCHESTRATION.md) | CLI adapter, DAG, worktrees, integration, PR, preview |
| [11_API_AND_EVENTS.md](11_API_AND_EVENTS.md) | REST contracts, SignalR events, errors, examples |
| [12_SECURITY_AND_OPERATIONS.md](12_SECURITY_AND_OPERATIONS.md) | Threat model, sandbox, secrets, audit, resilience, local setup |
| [13_TESTING_AND_ACCEPTANCE.md](13_TESTING_AND_ACCEPTANCE.md) | Test strategy, acceptance scenarios, measurable demo |
| [14_IMPLEMENTATION_ROADMAP.md](14_IMPLEMENTATION_ROADMAP.md) | Ordered groups, dependencies, checks and gates |
| [15_DISH_RATE_REFERENCE.md](15_DISH_RATE_REFERENCE.md) | Separate target app, fixtures, feature contracts, demo scripts |
| [16_TECH_VALIDATION_AND_REFERENCES.md](16_TECH_VALIDATION_AND_REFERENCES.md) | Official docs, external capability spikes, fallback decisions |
| [17_AGENT_PROMPTS.md](17_AGENT_PROMPTS.md) | Runtime prompt contracts and copy-ready implementation kickoff |
| [18_TRACEABILITY_AND_DECISIONS.md](18_TRACEABILITY_AND_DECISIONS.md) | Requirement mapping, risks, design decisions, open items |
| [AGENTS.md](AGENTS.md) | Persistent implementation-agent instructions |

## Decisions at a glance

- DEV PRISM AI is a software-project control plane, not a new IDE, generic prompt scorer, or a new coding engine. Its core is a read model plus evidence-backed change plans, not a chat history RAG.
- Feature Explorer is the primary experience. Show real work items, selected Figma frames, code references, docs, decisions, tests, relations and freshness together. Browse without any model calls.
- **Local MVP:** DEV PRISM AI and PostgreSQL run locally; Azure DevOps Boards and Figma are real online services read via real remote MCP. Show stable identity, source, freshness, immutable snapshots, local feature links and approved decisions after restart. Wiki, Repos and rendered Figma previews are separate companion capabilities; prove them independently.
- **No silent fallback:** typed remote MCP adapters do not use an LLM for import. Only an explicit owner-approved auxiliary REST route is permitted, with separate provenance; never replace a required core MCP read with it.
- **MVP stack:** .NET 10 LTS / ASP.NET Core Minimal API, React + TypeScript + Vite, PostgreSQL + EF Core, one embedded background worker and local HTTP status polling. No Azure hosting dependency or separate web/worker runtime in packaged mode.
- **Later:** structured Watchtower, richer graph/context, AI chat, SignalR where justified, approved writes, isolated workflow scheduler/CLI, draft PR, and conditional merge. Figma writes and provider-specific operations require separate proof. Native coding engine, pgvector and Atlassian are not MVP requirements.
- Project Brain stores approved decisions, typed relationships and immutable source versions. Source systems remain authoritative. AI inference is proposed, not automatically trusted. Watchtower detects drift and explicit policy violations. Memory never silently overrides newer requirements.
- Every external write is reviewed in a bounded, versioned plan. Merge to main requires human authorization and branch policy compliance. Never bypass provider permissions. No automatic cloud deployment for the MVP.
- Measure cost and quality against a fair baseline; never claim token savings or unique market positioning without evidence.

## Specification vs implementation

This is a design deliverable, not a built or tested application. G0 verifies the remote read capabilities and host; G3 demonstrates the real import and restart scenario through PRISM. Use authorized DishRate content hosted in real test services, not company data. Named sample IDs are fixtures, never assumed provider results. The Local MVP requires manual evidence, not new FE/BE unit or integration test suites. AI/CLI and permission to write are later validation tracks.
