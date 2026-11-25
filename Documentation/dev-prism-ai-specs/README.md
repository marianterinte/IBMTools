# DEV PRISM AI — implementation specification pack

**Version:** 1.0 | **Date:** 2026-11-25 | **Status:** proposed baseline, ready for gated implementation

**Project Resource Intelligence & Synchronization Management**

One project. Every perspective. One command center.

DEV PRISM AI is a local-first engineering command center that unifies Azure DevOps, Figma, repositories and project documentation into a feature-oriented workspace, detects cross-system inconsistencies, and executes approved changes through AI-assisted workflows and existing coding agents. It is an independent prototype intended for a technical demonstration at IBM, not an official IBM product or a claim of IBM approval.

## How to use this pack

Copy the contents to `docs/spec/` in the new `dev-prism-ai` repository. Start the implementation agent with [AGENTS.md](AGENTS.md) (copy to repository root and adjust relative links to `docs/spec/` if needed) and [00_START_HERE.md](00_START_HERE.md). Do not concatenate the entire pack into every prompt. The roadmap provides mandatory reading per task group. The specifications are in English for consistent technical nomenclature and easier agent use. UI defaults to English.

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
- Real integration for the demonstration: Azure DevOps (Boards + Wiki + Repos), Figma read/preview, local Git, PostgreSQL, one provider, one existing CLI coding engine. Figma write is capability-gated; Atlassian is an extension, not a prerequisite.
- Use typed adapters/REST for synchronized data; MCP for approved model-mediated actions, with explicit alternatives when tools lack safe preconditions. Discover actual capabilities, do not assume all providers can write.
- .NET 10 LTS / ASP.NET Core, React + TypeScript + Vite, PostgreSQL, EF Core, SignalR, React Flow, Git, Docker-based isolated execution, optional pgvector after benchmark. No Azure hosting or Azure OpenAI dependency. One local modular monolith; no microservices.
- Implement a deterministic workflow scheduler with durable state; start with OpenCode adapter only if compatibility spike passes, otherwise use Codex if authorized. Native coding agent is not in scope. Roo Code is a possible tool for building DEV PRISM AI, not a required runtime dependency.
- Project Brain stores approved decisions, typed relationships and immutable source versions. Source systems remain authoritative. AI inference is proposed, not automatically trusted. Watchtower detects drift and explicit policy violations. Memory never silently overrides newer requirements.
- Every external write is reviewed in a bounded, versioned plan. Merge to main requires human authorization and branch policy compliance. Never bypass provider permissions. No automatic cloud deployment for the MVP.
- Measure cost and quality against a fair baseline; never claim token savings or unique market positioning without evidence.

## Specification vs implementation

This is a design deliverable, not a built or tested application. External credentials, exact provider/model compatibility, live ADO/Figma capabilities, Docker host configuration and organization policy must be verified in the first implementation group. The demo uses a dedicated synthetic DishRate repository, not company data. All named task IDs and mock metrics in the examples are fixtures; live IDs come from the connector.
