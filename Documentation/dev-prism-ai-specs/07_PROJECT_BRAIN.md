# 07 — Project Brain: feature graph, decisions and retrieval

## Purpose

The brain is a project intelligence model built from actual versioned artifacts and approved local decisions. It is not an autonomous truth generator, a dump of chat logs, or a prerequisite to browse. Its value is transparent cross-source linking, scoped impact, decision traceability and controlled context reuse. Store graph in PostgreSQL with typed nodes/edges; recursive CTE or bounded BFS, no dedicated graph database in MVP.

## Local MVP: links and approved memory

Project Brain starts as local feature links and versioned decisions, not a separate retrieval service or chat store. Create a feature by hand; choose imported resources visible in that project and attach them with explicit evidence. The operator may remove a local relation without modifying its provider original. Start with the `Contains` membership and evidence references; graph traversal, inferred links, model analysis and context packs are post-MVP. Basic inspection and search use the local read model without a provider request.

A decision draft needs a stable key, feature scope, statement, rationale and at least one authorized evidence reference or explicit operator statement. ADO/Figma content is evidence, not authority to approve a rule. Show `Draft`, `Approved` or `Superseded`; the approval changes only local policy. Each approved statement is immutable; editing creates another draft. A later artifact update marks affected cited evidence as older, without invalidating the current approved rule automatically. If access is suppressed, hide protected excerpts while an independently authored local decision may remain visible; do not expose copied restricted text through its rationale or evidence view. Browse current and prior approved versions after restart without chat, embeddings, prompt history or a fresh provider read.

Example: the operator approves BR-006 (integer rating, 1–5) using a linked work item version. The next sync shows new acceptance criteria on the same item and leaves BR-006 approved. Feature Explorer pairs the two versions and shows the evidence age; it does not certify an automated policy conflict during the MVP. The richer version-target and promotion workflow below applies to later missions only.

## Graph vocabulary (post-MVP except explicit local membership)

| Edge | Direction and example | Evidence |
|---|---|---|
| Contains | Feature -> resource | user/approved plan |
| Specifies | WorkItem/Wiki -> Feature | explicit ID/link/approved |
| Designs | FigmaFrame -> Feature | selected node, linked ticket |
| Implements | GitFile/PR -> Feature or WorkItem | explicit path/PR association, model candidate |
| DependsOn | Feature/resource -> prerequisite | explicit dependency, static contract analysis |
| Documents | WikiPage -> Feature/Decision | explicit link/heading |
| Validates | TestEvidence -> Decision/Feature | named test + actual run |
| Constrains | Decision -> Feature/resource | approved scope, typed rule |
| Supersedes | Decision -> prior decision version (version evidence; use history) | approved change |
| Produces | Mission -> PR/WorkItem/TestEvidence | actual receipt |
| References | Resource -> Resource | literal link/ID, parsed reference |

`Supersedes` is primarily version relation on decision_versions; if projected into graph, include exact version IDs and do not create self-edge. Edges are directional; UI can traverse inverse. Verified = explicit machine-verifiable relationship or human-confirmed; Proposed = model or heuristic candidate; Stale = evidence revision changed in relevant field; Rejected = explicit rejection with rationale. Do not equate model output or lexical similarity with verified. Evidence contains source/target version IDs, locator (line range, work item field, Figma node/text, Wiki heading), excerpt/hash, method and actor. UI shows confidence class, not fake probabilities. Human confirmation records exact version and scope. Explicit stable relation (same ADO parent ID) may remain valid after unrelated text change; content-dependent inference becomes stale when evidence changes. Reevaluate relevant only.

## Linking strategy

Step 1 explicit: ADO relations, PR work item references, Figma URL in work item, Git path references in docs, approved mission outputs. Validate destination belongs to same project and allowed source. Step 2 deterministic: parsed references, exact symbols, OpenAPI operation IDs, explicit `DEVPRISM-FEATURE:<key>` markers if project opts in. Step 3 heuristic: normalized title/keyword matches return ranked candidates, never auto-confirm. Step 4 optional model: provide bounded candidate list + excerpts, ask for typed relations with evidence and abstain; schema validate every ID/version/locator, store Proposed. Step 5 review queue for confirm/reject/link manually. During initial feature creation, approved plan outputs can directly establish Contains via receipts. Avoid global pairwise model calls, use source change + feature neighborhood. Use graph caps depth 2 default, max nodes 150, include truncation flag and expandable frontier. For impact analysis allow depth 3 and limit 500 with coverage warning. Prevent cycles using visited set; dependencies for agent DAG separate from general graph (graph may cycle legitimately).

## Decisions and governance

A decision has stable key, kind (BusinessRule, Architecture, Convention, Security, Contract), statement, rationale, scope (feature IDs/modules/tags), owner, lifecycle, source version(s), optional typed policy. Approved versions immutable. Example BR-006: ratings initially integers 1–5, applies rating API, UI and DB. BR-012: owner exports must exclude reviewer identity. Proposal extraction from chat is explicit reviewable draft; no automatic memory approval. If source changed, mark source relationship stale, do not silently delete approved policy; ask owner to reconcile. External requirement is authoritative for its own resource, local policy governs DEV PRISM gates until amended. When new requirement conflicts, show both; do not assume old or new wins. Policy amendment approved as target for a mission, baseline policy remains until verified implementation and promotion. Unsuccessful mission leaves baseline. Explicit exception has limited scope and expiry, cannot override security/authorization invariants.

### Example typed rule

```json
{
  "key": "BR-012",
  "version": 1,
  "kind": "Security",
  "statement": "Restaurant-owner exports exclude reviewer identity.",
  "scope": {"featureKeys": ["owner-export"], "operationIds": ["exportOwnerReviews"]},
  "policyType": "ForbiddenContractFields",
  "payload": {"fieldAliases": ["reviewerEmail", "reviewerFullName", "email"], "locations": ["responseSchema", "csvHeaders"]},
  "enforceAt": ["pre_plan", "pre_merge"],
  "source": "approved decision with linked evidence"
}
```

Rules are data for a finite supported check catalog; no arbitrary SQL, C#, JS or model-authored executable policy. Approval of textual rule does not imply deterministic checker exists. Show enforcement capability, coverage and limitations. Security/privacy examples do not prove all possible leaks are prevented; tests add evidence.

## Context builder (no vector dependency initially)

Inputs project, feature, mission, agent role, task text, explicit refs, allowed sources, max input budget. Output snapshot manifest, mandatory constraints, task/acceptance criteria, contract version, role-specific source refs/excerpts, relevant verified decisions, candidate conflicts separately, omission report and estimated token count. Algorithm:
1. Authenticate scope; capture source versions and current base SHA. Reject inaccessible sources; flag stale/unknown critical coverage.
2. Include mandatory approved baseline/target rules and execution restrictions plus acceptance criteria and frozen contract. Never silently drop mandatory content; if exceeds budget, stop and ask to narrow task or increase approved budget.
3. Retrieve direct feature links, then bounded neighbors; lexical exact IDs/symbols and full-text ranking. Prefer verified, current, scoped source. Include original source excerpt and rationale for decisions. Rank by task role, graph distance, source relevance, recency only as secondary signal. Deduplicate identical content hashes and references. Code excerpts only from explicit bounded paths.
4. Allocate remaining budget by role (frontend design/API, backend contracts/schema, QA acceptance and rules); select snippets not whole repos. Record omitted count/reason. Include source citation metadata with each snippet. Treat source data as untrusted content, not instructions. Redact secrets before model transmission. Token estimate via compatible tokenizer if available, else conservative approximation labeled estimate.
5. Persist immutable pack metadata and hash; send actual minimal pack to model/CLI. No claim of engine context control: engine may independently read files, caching and tool schemas add tokens. Record actual usage when engine/provider supplies it, unknown otherwise.

Optional pgvector P2: compare against lexical/graph baseline with representative cases; store per model/dimension/version, only consented data, incremental embed changed excerpts, project scope before retrieval, top-k and relevance eval. Use hybrid retrieval only when recall improvement warrants maintenance and cost. Do not send sensitive source content to embedding provider by default.

## Brain APIs and UX

Create feature, list/add/review links, graph query, decision draft/approve/supersede, source evidence, context preview. All local mutation authorized; human approvals audited. Browser reads cached data only. Graph edge inspector has link origin and evidence, original navigation and review state. Source history and staleness visible. Project Brain MCP server later: read-only tools `search_project_context`, `get_feature`, `get_decision`, `get_source_evidence`; mutation proposal tool later never direct approval. Scoped auth and budget; no cross-project retrieval.

## Failure tests

Cross-project link rejected at service and DB. Repeated imports dedupe. Renamed Git path not silently mapped without evidence. A stale edge never presented as current verified. Model references unknown ID/version rejected. New conversation retrieves approved BR-012 without previous chat. Mandatory rule too large blocks rather than truncating. Read-only feature page emits zero AI calls. Inaccessible content never returned by search or context.
