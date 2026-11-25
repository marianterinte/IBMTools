# 01 — Product requirements and scope

## Product statement

DEV PRISM AI is a feature-centric workspace for software engineers that connects distributed project artifacts, detects drift between sources and executes authorized changes across them. Core user question: “What is the current state of this feature, what is affected if I change it, and can I implement that change without losing consistency?”

PRISM = Project Resource Intelligence & Synchronization Management. Product name: DEV PRISM AI. Repository slug: `dev-prism-ai`. Independent prototype; IBM is the intended presentation audience, not part of branding or a required integration.

## Primary user and jobs

Single developer/technical lead working locally on multiple projects, starting from a configured ADO project and Git repository. Jobs: inspect a feature without switching tools; see what changed since last visit; find why a rule exists; investigate inconsistent design, docs and code; propose coordinated changes; delegate coding while reviewing outcomes; resume a mission after restart. A secondary audience is other developers viewing a demo; they should be able to tell live evidence from synthetic illustrations.

## Local MVP release contract

The operator runs DEV PRISM AI locally for the IBM presentation; the source providers and their MCP servers are remote. Use dedicated, authorized DishRate assets hosted in the real Azure DevOps and Figma services. The presentation is not an IBM integration, public deployment or claim of endorsement. “Real provider” describes the transport and source; “synthetic content” describes the demo data. These are independent labels.

| Gate | Required observable outcome |
|---|---|
| G0 provider proof | A real remote MCP read returns a scoped ADO work item and a scoped Figma frame with usable context. A handshake, opening a browser URL, or replaying a fixture does not qualify. |
| G3 release | Link the ADO and Figma resources to one feature, open the evidence and originals, approve a local versioned decision, observe one changed artifact and no duplicate on unchanged resync, and recover the feature after restart without a network or model call to browse it. |
| Companion reads | Try a page from ADO Wiki, an ADO Repos commit reference and a rendered Figma preview where available. Put each in the capability report even if unavailable. A manual link to an original is not an imported resource. |
| Non-goals | Cloud hosting, employer accounts/data, autonomous changes, external writes, LLM-dependent navigation, scheduled polling, graph visualization, semantic drift and CLI agents are not required for this release. |

## Requirements

| ID | Capability / observable acceptance | Priority |
|---|---|---|
| R01 | Create/select a project, configure scoped remote connections, and check their core read capabilities independently. Cached navigation needs no AI key or provider request. | P0-A |
| R02 | Import actual ADO work items via remote MCP. Display stable identity, title, original link, available fields, provenance, freshness and revision or its absence. | P0-A required |
| R03 | Import usable context for a selected Figma frame via remote MCP. Show its identity, original link, content, observed version and access. Rendered preview is separate. | P0-A required text/context; preview auxiliary |
| R04 | Read one ADO Repos commit reference (repo and SHA) when supported; local Git tree, code browser and PR views come later. | P0-A auxiliary / later |
| R05 | Read a selected ADO Wiki page when supported, with sanitized content, provenance, and revision or its absence. | P0-A auxiliary |
| R06 | Feature Explorer groups linked resources and decisions. The operator can add/remove typed local links; unavailable coverage is not reported as empty or complete. | P0-A |
| R07 | Show basic per-feature version history and observed changes. Interactive dependency graph and global activity navigation are separate extensions. | P0-A history / post-MVP graph |
| R08 | Draft, approve and supersede a local versioned decision with rationale and evidence. Automated conflict checks come later. | P0-A memory / G4 conflict checks |
| R09 | Watchtower maps impact and distinguishes stale/unknown from verified mismatch. P0-A displays change history and freshness without classifying drift. | G4 post-MVP |
| R10 | Chat scoped to current project/feature; read queries cite source revisions, action requests propose reviewable plans. | P0-B |
| R11 | Create initial backlog from one prompt; approve individual proposed writes; actual ADO create/verify and link results to feature. | P0-B |
| R12 | Update ADO and Wiki from same mission with exact diffs, preconditions, receipts and partial failure handling. | P0-B |
| R13 | Delegate coding to one compatible CLI, isolated working copy, real file diff/test evidence and local preview. | P0-B |
| R14 | Push integration branch, create PR, link work items, preview and explicitly approve merge subject to provider policies. | P0-B/P1 merge |
| R15 | Two concurrent agents on independent FE/BE work; dependency graph and integration checks. QA as separate agent optional. | P1 |
| R16 | Pre-plan and pre-merge policy check, deterministic supported checks and semantic candidates. | P0-B |
| R17 | Observe agent runs, sync and actions, measured token usage when available, time and evidence. | P0-B |
| R18 | Optional approved Figma write when capability is validated; otherwise manual handoff and refresh. | P1 conditional |
| R19 | Restart and recover persisted data and state without duplicate remote writes or pretending agents are still running. | P0-B |
| R20 | Generic connector extension, Jira/Confluence and optional memory MCP server without core rewrites. | P2 |
| R21 | Compare default agent workflow against structured context on same tasks; optional pgvector based on evidence. | P2 / benchmark after P1 |

## Product boundaries

Local MVP is a single-operator, project-scoped read workspace with local editing of feature metadata, links and decisions. Remote resources are provisioned manually in sandbox accounts; the target DishRate app need not run or exist as working software for the Local MVP. Existing-engine coding, search expansion and controlled remote mutations belong to the later product. “Start from scratch” never means automatically creating organizations, billing or cloud accounts.

Out of scope for MVP: full Figma editor, full IDE, replacing ADO/Confluence, generic all-provider integration, remote multi-user tenancy, cloud deployment automation, autonomous main merges, production database writes, universal business rule checker, arbitrary MCP code execution, agent self-modification, prompt scoring, payroll/person ranking. Do not mirror entire company environments. Do not send all project data to models. No mandatory RAG/vector database; lexical and graph retrieval first.

## Differentiation (hypothesis)

Compared to an IDE assistant, the organizing entity is the feature, not the file or conversation. Compared to a read-only aggregator, the workflow can propose and carry out a controlled change with journal and actual code. Compared to a generic memory system, knowledge links to live versioned sources, and selected policies become gates. Similar capabilities exist in other products; no uniqueness claim. The demo must show value even when agent coding and AI calls are off: cached browsing, graph, source diff, explicit relation, deterministic guard.

## Extended product success criteria (post-MVP where not in release contract above)

- User can find a task, Figma frame, relevant file, rule and wiki page from one feature page and open originals within two clicks after selecting the feature.
- On known half-star drift fixture, report affected API/model/UI/docs with evidence, distinguish verified facts from AI guesses and do not change anything until approved.
- A live authorized action creates/updates a work item and Wiki page, each with actual remote ID/revision and reread proof. Repeated request is reconciled safely.
- Run at least one real isolated coding task; full demo has two concurrent branches with nonoverlapping ownership and final combined build/test. Preview shows actual branch SHA. PR creation/merge status based on actual provider response.
- Restart preserves linked decisions and mission evidence; new conversation can retrieve the approved rule without old chat transcript.
- Show metrics from real events: elapsed time, observed usage where reported, data coverage and checks. No promised cost savings. Benchmarks include overhead, caching and missing telemetry.

## Release labels

**Local MVP** = P0-A live remote read and persistent local memory. **Controlled Actions** = P0-B including safe engine, journal and recovery. **Engineering Demo** = P1 parallel path and optional supported design write. **Experimental** = any unproven adapter/semantic checker. Label disabled operations with the reason and next step.
