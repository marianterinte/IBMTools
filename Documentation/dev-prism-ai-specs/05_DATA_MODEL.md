# 05 — PostgreSQL data model and state

## Conventions

UUID internal IDs; UTC `timestamptz`; `bigint` local revision or sequence where needed; JSONB for bounded provider-specific payloads (not a substitute for core typed columns). External IDs strings, scoped by connection and kind. `project_id` required on all project data, including child tables; enforce same-project relations through composite foreign keys `(project_id,id)` or equivalent DB constraints. Unique natural identities per connection; index foreign keys and lookup/status/time columns. Use EF Core migrations, real PostgreSQL integration tests, no EnsureCreated for deployed DB. Use explicit enums/check constraints and concurrency token. Soft archive external resources, do not delete evidence referenced by decisions/actions. Source URLs sanitized and never hold access tokens.

## Projects and integrations

| Table | Fields / constraints |
|---|---|
| projects | id, owner_id, slug unique per owner, name, description, status, created_at, updated_at, version |
| connections | id, project_id, kind (`Ado`, `Figma`, `Git`, `Wiki`, `Atlassian`, `CustomMcp`), name, endpoint, external_scope JSONB, auth_kind, secret_ref nullable, enabled, health, last_attempt_at, last_success_at, version; no plaintext credential |
| connection_capabilities | connection_id, capability_key, state (`Available`,`Unavailable`,`Unverified`), auth_scope_summary, verified_at, evidence_ref, limitations JSONB; unique connection/key |
| provider_profiles | id, project_id, provider_kind, endpoint, model, secret_ref, capability_json, budget_json, version |
| workspace_profiles | id, project_id, registered_root, remote_url, default_branch, engine_path_ref, sandbox_profile, preview_profile JSONB, version; canonical path verified outside DB |
| sync_cursors | connection_id, stream_kind, opaque_cursor, watermark, last_complete_generation, version; unique connection/stream |
| sync_runs | id, project_id, connection_id, started_at, finished_at, status, counts JSONB, error_code, retry_at, cursor_before/after, coverage JSONB |

## Resources, versions and graph

| Table | Fields / constraints |
|---|---|
| resources | id, project_id, connection_id, kind (`WorkItem`,`FigmaFrame`,`GitFile`,`GitCommit`,`PullRequest`,`WikiPage`,`TestEvidence`,`ApiContract`,...), external_id, title, canonical_url, current_version_id nullable, lifecycle (`Active`,`Archived`,`Inaccessible`), last_seen_at; unique(project,connection,kind,external_id) |
| resource_versions | id, project_id, resource_id, source_revision, content_hash, captured_at, source_updated_at nullable, normalized JSONB, body_text nullable, storage_ref nullable, coverage JSONB; unique(resource_id,source_revision,content_hash); immutable; hash canonical normalized payload excluding volatile timestamps/temporary preview URLs |
| features | id, project_id, key unique per project, name, description, state, version, created_at, updated_at |
| graph_nodes | id, project_id, kind (`Feature`,`Resource`,`Decision`,`Mission`), feature_id/resource_id/decision_id/mission_id nullable with exactly one matching kind; unique target per project |
| graph_edges | id, project_id, from_node_id, to_node_id, relation_type, state (`Proposed`,`Verified`,`Rejected`,`Stale`), origin (`Explicit`,`Human`,`Deterministic`,`Model`), evidence_json, source_version_id nullable, target_version_id nullable, created_at, verified_at, version; unique active relation triple per project, indexes from/to/state |
| edge_history | id, project_id, edge_id, old_state,new_state,evidence_json,actor,created_at |
| resource_changes | id, project_id, resource_id, before_version_id nullable, after_version_id nullable, change_kind, changed_fields JSONB, observed_at, sync_run_id, dedupe_key unique per project |

`graph_nodes` stable identity; `resource_versions` immutable proof. Use `Feature -> Resource` relation `Contains`; no redundant authoritative membership table. Git file identity = repo-relative normalized path, version = commit SHA plus blob SHA; rename suggestion from Git diff only when confirmed, retain old path history. Figma file/frame identity = file key + stable node ID. Wiki identity = stable page ID if supported else canonical path + wiki ID; path rename handled explicitly. Git external remote provider PR distinct from local file. Resource current_version FK must point to same resource; create in transaction, handle circular migration by nullable pointer then set. Full scan marks missing only after complete authorized coverage, not on partial sync/403. A deleted resource is archived; access revoked becomes inaccessible with cached content hidden from UI/model and subject to purge policy.

## Decisions, context and findings

| Table | Fields / constraints |
|---|---|
| decisions | id, project_id, key unique (`BR-012`), kind, title, current_approved_version_id nullable, scope JSONB, archived_at |
| decision_versions | id, project_id, decision_id, version_no unique per decision, statement, rationale, policy_type nullable, policy_payload JSONB, enforcement_stages, state (`Draft`,`Approved`,`Superseded`,`Rejected`,`Invalidated`), approved_by nullable, approved_at, supersedes_id nullable, created_at; approved content immutable |
| decision_sources | id, project_id, decision_version_id, resource_version_id nullable, message_id nullable, mission_id nullable, actor_id nullable, excerpt, locator JSONB; at least one legitimate source; no fabricated source |
| context_packs | id, project_id, mission_id nullable, agent_run_id nullable, requested_scope, included_refs JSONB, omitted_summary JSONB, snapshot_hash, estimated_tokens nullable, actual_tokens nullable, created_at; redact stored excerpts; reference source versions |
| findings | id, project_id, feature_id nullable, check_key, fingerprint, severity, evidence_class, status, title, explanation, coverage JSONB, first_seen_at,last_seen_at,resolved_at, version; unique active fingerprint per project |
| finding_evidence | id, project_id, finding_id, resource_version_id nullable, decision_version_id nullable, locator, observed_value JSONB, proof_type, excerpt |
| finding_events | id, project_id, finding_id, action, actor, reason, timestamp, previous_status, new_status |
| analysis_runs | id, project_id, trigger, check_catalog_version, scope, coverage, status, usage_id nullable, created_at, completed_at |

Use Postgres full-text search `tsvector` for human-language titles/body/decision statements and trigram or escaped ILIKE for identifiers/paths as appropriate. GIN indexes. Restrict search to visible project/source. Optional pgvector table only after benchmark with embedding model/dimension/version and per-project filters; no embedding data sent externally without consent. Never turn an inferred rule into an approved rule by extraction alone.

## Conversations, missions, action plans and execution

| Table | Fields / constraints |
|---|---|
| chat_threads | id, project_id, feature_id nullable, title, created_at, updated_at |
| chat_messages | id, project_id, thread_id, role, content, status, refs JSONB, client_request_id nullable, created_at; unique client request per thread when present |
| missions | id, project_id, feature_id nullable, title, request_text, state, active_plan_id nullable, base_sha nullable, budget JSONB, version, created_at, updated_at |
| plan_versions | id, project_id, mission_id, version_no, canonical_payload JSONB, content_hash, snapshot_manifest JSONB, expires_at, created_at; immutable, unique mission/version |
| plan_steps | id, project_id, plan_id, step_key unique within plan, kind, target_ref JSONB, expected_version, desired_payload JSONB, risk, required, dependencies JSONB; immutable |
| approvals | id, project_id, plan_id, plan_hash, approved_step_keys, actor_id, scope, granted_at, expires_at, revoked_at nullable; immutable grant, explicit revocation |
| execution_tasks | id, project_id, mission_id, plan_step_id nullable, role, state, attempt_count, workspace_ref, contract_hash, output_commit nullable, error_code, version, timestamps |
| task_dependencies | project_id, task_id, prerequisite_id, unique pair, no self edge; DAG validation in service |
| agent_runs | id, project_id, task_id, attempt_no unique per task, engine, engine_version, model, workspace_id, state, started_at, ended_at, exit_code, context_pack_id, output_manifest JSONB, usage_known, error_code |
| workspaces | id, project_id, mission_id, task_id nullable, sanitized_path_ref, branch, base_sha, head_sha, sandbox_id, state, created_at, cleaned_at |
| action_attempts | id, project_id, plan_step_id, attempt_no, idempotency_key, request_hash, expected_revision, state, remote_id nullable, observed_revision nullable, receipt JSONB, started_at, ended_at, error_code; unique step/attempt and operation key semantics; never overwrite previous attempt |
| check_results | id, project_id, mission_id, agent_run_id nullable, commit_sha, contract_hash, check_key, outcome, evidence_ref, started_at, finished_at |
| previews | id, project_id, mission_id, commit_sha, runtime_ref, local_url nullable, state, started_at, ended_at, error_code |
| usage_records | id, project_id, mission_id nullable, agent_run_id nullable, model, provider, input_tokens nullable, output_tokens nullable, cached_tokens nullable, total_tokens nullable, cost_minor_units nullable, currency nullable, source, estimate_flag, created_at |

No cost inference from absent usage. Store decimal cost precision or micro-units if supported; distinguish provider reported vs tariff-estimated. Enforce budgets with known usage and conservative caps; unknown usage can block unattended continuation.

## Jobs, events, logs

| Table | Fields / constraints |
|---|---|
| jobs | id, project_id, kind, payload JSONB, state, available_at, lease_owner nullable, lease_until nullable, heartbeat_at nullable, attempts, max_attempts, dedupe_key, last_error, created_at; index state/available_at |
| domain_events | sequence bigint identity, id UUID unique, project_id, entity_type,id, event_type, occurred_at, correlation_id, payload JSONB; immutable redacted |
| outbox | event_id unique FK, published_at nullable, attempts, next_attempt_at |
| audit_entries | id, project_id, actor_kind, actor_id, operation, target_ref, plan_id nullable, outcome, receipt_ref nullable, occurred_at, correlation_id; append-only for app |
| run_logs | id, project_id, agent_run_id, sequence, level, message_redacted, occurred_at, source; unique run/sequence; bounded retention |
| idempotency_requests | project_id, operation, key, request_hash, response_ref, created_at; unique project/operation/key |

## State machines (authoritative transitions)

Mission: `Draft -> AwaitingApproval -> Approved -> Queued -> Running -> AwaitingReview -> Succeeded`; any active state can become `Blocked`, `Failed`, `Cancelled`, `Interrupted`, `PartiallySucceeded` as appropriate. `Blocked/Interrupted/Partial` resume only via reconciliation and new approval when inputs changed. `Succeeded` means all required approved steps verified, not merely agent exit. A plan may end at `AwaitingReview` if merge not part of approved scope. `Stale` plan cannot be executed. Plan changes create new version; approvals tied to exact hash, invalidate on relevant source version changes.

Task: `Pending -> Ready -> Running -> Succeeded/Failed/Cancelled/Interrupted/Blocked`; `Skipped` only for explicitly optional/waived steps with downstream validation. Dependencies require successful prerequisites. Retry creates new attempt, never changes prior run outcome. External action: `Prepared -> Authorized -> Started -> VerifiedSucceeded / Failed / Unknown / Conflict / CancelledBeforeDispatch`; `Unknown -> VerifiedSucceeded / ConfirmedNotApplied / NeedsHumanReview`; only confirmed not applied can retry if approval still valid. Finding: `Open -> Acknowledged/Dismissed/Resolved/Stale`; resolve requires new check evidence, dismiss is not resolve and scoped to fingerprint/version. Decision: draft -> approved or rejected; approved -> superseded/invalidated with reason; effective version switch atomic. Preserve prior history.

## Retention and consistency

Default proposed: run logs 30 days, raw imported payloads 30 days, normalized historical versions 90 days, keep referenced evidence and audit/decisions until project deletion subject to access/privacy policy; configurable caps and purge job. Delete local project only after no active jobs, explicit confirmation; no remote deletes. Revoke connection hides inaccessible cache immediately, purge per configured policy, maintain only permitted minimal audit references. Backup/restore instructions in 12. All migrations test upgrade from prior schema, not just empty DB. Never store provider tokens or unredacted prompts by default in logs; chat content itself may be sensitive and must remain scoped and removable.
