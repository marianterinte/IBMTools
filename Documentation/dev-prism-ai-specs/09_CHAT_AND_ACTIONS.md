# 09 — Chat, model providers and cross-system actions

## Provider abstraction

Start with one tested AI provider using an authorized API key; OpenAI or approved IBM endpoint. IBM's exact API format, model names, rate limits, data handling and compatibility are unknown. Require probe for chat, streaming, tool calling or structured-output support, usage reporting, max context and allowed model. If tool calling unsupported, schema-validated plan JSON can be attempted with bounded repair; if unreliable, disable action planning for that profile. Never assume an API key implies CLI compatibility. Provider profiles have endpoint allowlist, model, credential reference, max request/mission tokens, concurrency and timeout. Embeddings separate and optional. No Azure required. User subscription does not imply API credits.

## Chat behavior

Each thread scoped to project and optionally feature; user can explicitly attach resource/decision IDs or select from Explorer. On message, authenticate, rate limit, save request with idempotency key, classify intent: read/explain, analyze, propose change, coding mission. Read path can query current cached facts without LLM for exact searches. For conversational queries construct bounded context pack (07), tool list for only approved read capabilities, model system instructions (17), then stream. Limit tool rounds (default 6), tool output size, elapsed time and total context; cancel when user cancels or budget exhausted. Tool calls may refresh only explicitly scoped sources. No model access to raw secrets, unrestricted DB or shell. Store sources and actual tool receipts with assistant message. If facts unavailable state what is missing; do not cite fabricated IDs. Read and write requests visibly different. Chat cannot approve its own plan or manipulate policies.

## Model tool facade

These names are internal normalized tools, not claimed vendor tool names:

| Tool | Arguments (validated) | Behavior |
|---|---|---|
| `search_resources` | query, kind, featureId, limit | local scoped index, max 50 |
| `get_resource` | resourceId, versionId optional | sanitized bounded snapshot |
| `get_feature_graph` | featureId, depth, limit | graph + coverage |
| `get_decisions` | featureId, query, states | approved and separately draft rules |
| `get_findings` | featureId, status | evidence summaries |
| `get_diff` | resourceId, beforeVersion, afterVersion | bounded normalized diff |
| `build_context_pack` | scope, role, budget | references and omissions |
| `propose_change_plan` | typed plan draft | validate and save draft only |

Raw MCP tool catalog stays server-side; read calls exposed through controlled wrappers or explicitly capability-mapped schema. Write tools never presented as freely callable functions. Tool responses data, not instructions. Reject cross-project IDs, arbitrary URL/path, unknown enums, invalid references or model-generated approval. No loop on repeated identical failing calls.

## Plan format and validation

Plan version immutable after creation. JSON canonicalized using deterministic key ordering/number representation before SHA-256 hash. Include project/mission ID, source manifest, relevant connection/profile versions, base commit, intended target branch, policy baseline and target versions, created/expires timestamps, budget, steps, dependencies, expected outcomes, risk, required checks and exact mutation payload. Server derives IDs and risk classification. Example (fixture IDs shorthand for readability):

```json
{
  "title": "Support half-star ratings",
  "sources": [{"resourceId":"R-ADO-142","version":"8"},{"resourceId":"R-FIG-12","version":"v12"}],
  "baseSha": "<actual git SHA>",
  "policyTarget": {"key":"BR-006","draftVersion":2},
  "steps": [
    {"key":"ado","kind":"AdoUpdate","target":"R-ADO-142","expectedVersion":"8","patch":{"acceptanceCriteria":"Allow 1–5 in 0.5 steps"},"dependsOn":[]},
    {"key":"contract","kind":"FreezeContract","dependsOn":["ado"]},
    {"key":"frontend","kind":"AgentRun","role":"Frontend","allowedPaths":["web/src/ratings/"],"dependsOn":["contract"]},
    {"key":"backend","kind":"AgentRun","role":"Backend","allowedPaths":["src/Api/","src/Domain/","src/Infrastructure/"],"dependsOn":["contract"]},
    {"key":"integrate","kind":"IntegrateAndCheck","dependsOn":["frontend","backend"]},
    {"key":"review","kind":"HumanReview","dependsOn":["integrate"]}
  ]
}
```

Actual persisted target references use UUIDs/typed external identity; agent paths validated against registered project policy. Remaining push/PR/merge/wiki/promote steps generated as separate approved continuation or explicitly staged plan with distinct approval gates. A plan cannot approve a future unknown diff for external mutation. Typed result placeholders permitted only for narrow IDs (e.g. child parent ID from known create step); validate type and same project after resolution, no arbitrary templating or shell substitution. Changes to payload, approved step set, target, source revision, base SHA, policy or capability invalidate affected approval. Required checks cannot be removed by model. DAG acyclic; each required dependency present; no unsupported actions. Estimate cost only if known rates and workload, label estimate.

## Approval model

Operator sees exact before/after, target link, operation, current revision, permissions, dependency, risk and reversibility. Approval POST includes plan ID/hash, version, selected steps and explicit intent; backend binds actual authenticated actor, scope and expiration (default 30 minutes to start, revalidate each step; long-running agent may complete but external approval cannot outlive its expiry). Start only authorized steps; allow cancel before dispatch. Merge requires second approval after checks tied to exact head/base and branch policy. Agent-generated code diff is unknown at planning; approval for isolated edits only, later approval for external push/PR based on actual diff. User may approve policy target draft for mission but promotion to active only after verified implementation and separate explicit grant. Revocation prevents not-yet-dispatched operations; in-flight calls may finish; UI explains. Never treat chat wording alone as authorization to mutate all systems.

## Action journal and failure protocol

1. Validate scoped connection/capability, current target revision and payload against approval. Stale -> Conflict; no dispatch. Preflight reads current state and permission.
2. Persist `Started` attempt + request hash/idempotency marker before sending provider call. Use bounded timeout and typed adapter. Record sanitized response and observed remote ID/revision. Re-read target; verify intended fields/hash or actual PR state. Only then `VerifiedSucceeded`. Enqueue sync/outbox. If provider lacks comparable state, manual review, not success.
3. 4xx validation/403 -> Failed; 409/version mismatch -> Conflict; 429 confirmed not applied -> retry only with Retry-After and valid grant; timeout/connection reset/ambiguous 5xx -> Unknown. Never assume remote did not apply. Reconcile using target revision, marker or exact source/target PR; if can't disambiguate, human review. Reconcile read is safe. On retry make new attempt, keep prior evidence.
4. DAG downstream blocked on nonverified prerequisite. Independent authorized steps may finish unless cancellation/policy says stop. Mission `PartiallySucceeded` when some required operations verified and others failed/unknown. Show exact successful changes and unresolved actions. No global rollback across ADO/Figma/Git/Wiki. Compensation is a new approved version-checked plan (e.g. revert Wiki text or create revert PR); don't delete work items automatically.
5. Resync source(s), update graph links from actual receipts, record audit event. For manual Figma handoff, show `WaitingForHuman` then refresh and verify desired condition, or record explicit waived optional action. Never count manual instruction as executed edit.

## Usage controls

Default max concurrent AI chat/analysis 2, agent 2, per-mission configurable token/cost ceiling, max tool calls and time. Estimate prompt size and remaining context; stop before exceeding mandatory context. Use provider actual input/output/cached tokens if reported; never double-count engine aggregate and sub-events. Include planner, semantic analysis, retries, embedding and coding costs when observable. Unknown usage shown and prevents autonomous continuation beyond conservative capped run count/time. User may raise budget with explicit new grant. No background AI analysis by default. Audit records model ID/provider, prompt pack hash, timings and usage, not hidden reasoning or raw secret.
