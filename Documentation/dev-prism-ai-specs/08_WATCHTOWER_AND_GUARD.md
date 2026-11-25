# 08 — Watchtower and Memory Guard

## Responsibilities

Watchtower detects source changes and maps affected feature graph. Memory Guard runs checks at planning and integration/pre-merge. They share findings and evidence, not separate conflicting truth stores. Aim: show actionable inconsistencies, not automatically judge everything. Deterministic checks are reproducible; semantic checks are candidate findings pending evidence/human review.

## Check catalog (MVP)

| Key | Trigger | Method / evidence | Classification |
|---|---|---|---|
| WT-001 | changed linked resource | compare version/hash, show changed fields + neighbors | verified change, info; not necessarily mismatch |
| WT-002 | linked requirement/design explicitly changes rating step | compare normalized approved requirement/confirmed design annotation with current OpenAPI `integer`/`number` and known target contract | verified mismatch when structured evidence exists; candidate otherwise |
| WT-003 | new plan includes owner export fields | compare typed forbidden aliases + operation ID | verified policy conflict for known fields; semantic candidate for aliases outside catalog |
| WT-004 | contract changed | compare selected OpenAPI request/response type/required field against frozen contract | verified breaking-change candidate with diff; block when mission policy requires compatibility |
| WT-005 | ADO closed but no linked PR/test | show absence within configured import scope | candidate or unknown; not proof of no implementation |
| WT-006 | docs mention old rating rule | exact structured marker or approved decision reference/version; model text compare optional | verified stale marker or semantic candidate |
| WT-007 | critical source stale/unavailable | last successful sync and configured coverage/age | unknown; gate if policy requires fresh source |
| WT-008 | proposed agent output writes protected/unowned path | Git diff and workspace policy | verified violation, block integration |
| WT-009 | required test suite missing/failing | independent command results at target SHA | verified failure/block |
| WT-010 | owner export contract/CSV headers contain forbidden fields | parse OpenAPI and actual test CSV header fixture at integration SHA | verified violation for checked fields; does not prove total privacy |

Implement specific parsers and fixtures, not arbitrary “AI finds every issue”. For half-star support define structured `RatingPolicy` fields `{minimum:1, maximum:5, step:0.5, representation:"decimal"}` from approved rule/explicit requirement. For demo initial `step:1`, OpenAPI integer, C# integer. The guard detects integer vs target half-star step. Figma preview visual inference alone not definitive. Optional semantic analyzer receives current and before excerpts, approved decisions and candidate resources; output typed findings with evidence IDs, proposed severity, explanation and uncertainty; validate IDs/locators; never auto-block solely on LLM output unless operator-approved policy demands human review of candidate (not a verified violation).

## Trigger and processing

On committed resource change, queue scope analysis by project/resource/version/check catalog version. Traverse verified edges first, candidate edges separately; depth and node caps with coverage. Determine relevant checks by artifact kind and active feature policies. Dedupe by `(check, feature, normalized violation key, affected target)`; update last seen and evidence for same condition, not create flood. If check reruns on current inputs and violation gone, mark resolved with new evidence; if source inaccessible or evidence stale, mark stale/unknown instead. Dismissal requires reason, actor and evidence version; reopen when changed. Use debounced batch for bursts; per-project analysis budget, no repeated semantic calls on unchanged hashes. Findings do not automatically write source systems.

## Stages and enforcement

**Pre-planning:** collect current versions; evaluate explicit rules and data quality; list affected source areas; block if confirmed contradiction with active rule until approved amendment/exception, or mandatory data inaccessible. Proposed issues allow human clarification. **Pre-execution:** revalidate plan hash, approval, source revisions, selected target policy and permissions, no mutation if stale. **Pre-integration:** verify changed paths/commit lineage, required tests, contract hash, no unapproved policy changes. **Pre-merge:** independent checks on exact integration HEAD against current main target and target policies; if base/head or relevant source changed, re-run/reapprove. **Post-write/merge:** reread external artifacts; recompute relevant findings, update decision only when required verified steps and promotion approval satisfied. Wiki/ADO lag remains visible. No automatically resolved finding based on a model claim.

Severity: Blocker = explicit verified gate failure (or mandatory freshness not met); High = material verified mismatch or high-impact candidate marked candidate; Medium = likely issue; Info = observed change. Evidence class separate. The UI must explain why blocked and how to proceed. Approving change plan does not override branch protection, authorization or privacy policy restrictions. Exception must be explicit, allowed, versioned and scoped. Stale evidence or unknown required checks prevents `VerifiedSucceeded`.

## Remediation workflow

Finding detail -> show evidence and coverage -> user chooses `Analyze impact`, `Propose remediation`, `Acknowledge`, `Dismiss with reason`, `Refresh`. Plan generator uses linked source versions, produces typed proposed changes with per-step before/after and dependencies; user can inspect. Example half-star: target decision amendment, ADO criteria, contract freeze, backend migration, frontend controls, tests, PR/preview, merge review, Wiki update and decision promotion, final resync. Design already updated, no duplicate mutation. If Figma not supported, manual handoff step stays pending. Any partial failure visible. Never update all source systems by default without explicit plan.

## Measurement

Measure findings precision on synthetic labeled fixtures (true/false positives), coverage unknown vs checked, time to locate affected resources, number of manual link corrections, overhead/model usage. Compare manual/IDE flow with same sources and task. Do not use synthetic scenario to assert all Cursor agents miss issue. Main demo evidence: current design/ADO change triggers deterministic/confirmed drift before implementation; graph and plan inspectable; tests verify actual resolution. A developer can benefit without model or coding run.
