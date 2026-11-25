# 12 — Security, permissions and operations

## Threat model and boundaries

Local browser/operator, control-plane API and DB, external ADO/Figma/AI providers, imported untrusted content, untrusted coding process and target application preview are distinct trust zones. Attacker may control a work item, Wiki paragraph, Figma text, repo file, tool description/result, generated code or malicious dependency. Goal: prevent credential exfiltration, unauthorized external writes, host escape, cross-project disclosure, destructive Git changes, prompt injection and misleading evidence. Local does not mean safe. For employer use obtain policy approval for provider/model, data classification, MCP, personal keys, external endpoints and code execution; use synthetic demo data only until approved.

| Risk | Required control |
|---|---|
| Prompt injection from docs/tickets/code | data delimiters, tool allowlist, no model approval, schema validation, scoped refs, independent policy gates |
| Secret exfiltration | OS secret store, no plain DB/logs/prompt, scoped short-lived credentials, redaction, no broad environment inheritance |
| Host access from generated code | separate sandbox, nonroot, no host mounts/daemon socket/privileged mode, restricted egress and resource caps |
| Cross-project leakage | auth + project scope at API/service/DB, project-bound connections, isolation tests |
| SSRF/MCP abuse | explicit endpoint allowlist, DNS/IP policy, no unapproved redirects, no arbitrary URLs from model |
| Lost update / duplicate write | approval hash, remote version checks, action journal, reconciliation before retry |
| Malicious repo hooks/config | controlled Git runner, disable hooks, safe config, no untrusted git aliases, path validation |
| Untrusted preview | isolated runtime, loopback only, separate origin, no control-plane credentials, no production DB |
| Supply-chain risk | pinned deps/images, lockfiles, approved package registry, scan where available, no arbitrary install from ticket |
| False status | independent checks, actual receipts/versions, no simulated data in live mode |

## Local authentication

Bind API and DB to loopback or internal network only; do not accept public ingress. Use authenticated local session established through explicit first-run pairing/bootstrap secret, secure cookie or documented local token scheme with no URL tokens, rotating session, CSRF defense on writes, origin/Host validation against DNS rebinding. Dev CORS allow only actual local web origin, never wildcard with credentials. Authorize project owner on every endpoint including SignalR and media. Session secrets not persisted in browser localStorage. For remote/multiuser later redesign with OIDC/RBAC/tenant isolation. Read-only mode if no configured operator. No reliance on front-end button hiding.

## Secrets and provider connections

Secret storage adapter OS credential vault (Windows Credential Manager/appropriate platform keyring) or encrypted local store with encryption key outside DB under OS protection; dev environment secrets explicitly supplied and never committed. If secure storage unavailable, session-only credentials or block persistent setup. DB stores opaque secret reference. Separate read/write credentials where supported; least scopes, expire/rotate/revoke. Never print authorization headers, PATs, API keys, signed asset URLs, OAuth refresh tokens. Sanitize logs recursively and cap payload; detect obvious secret patterns and avoid logging source/prompt bodies by default. Child agent gets only necessary model credential through supported secure mechanism or broker; if provider requires broad host access, block until safe. Agent cannot access ADO/Git credentials or secret DB. Source data sent to configured provider only after consent and redaction; no implicit embedding or external telemetry. Note provider retains data according to their policy; no claims of zero retention without verification.

## Sandbox requirements before real coding

Git worktree is filesystem organization only. Real agent process runs inside disposable restricted container/VM or equivalent verified boundary. Dedicated sandbox user, no privilege escalation, dropped capabilities, read-only base FS where feasible, isolated writable project copy, bounded temp, CPU/memory/PIDs/storage/time limits, no host home/SSH/credential mounts, no Docker socket or host network. Permit outbound only to approved model endpoint and dependency registries when required via controlled policy/proxy; enforce DNS/redirect policy. If host cannot enforce network or process boundary, restrict to reviewed offline fixture/proof environment and explicitly mark real unattended coding blocked. Do not invoke unsafe CLI flags to bypass. Tool permissions defense-in-depth, not substitute for isolation. Sandbox never runs as admin/root. Validate generated code/build scripts as untrusted. For preview apply same boundary, dedicated local test database, fixed approved start profile, no production secrets. Docker Compose file from agent output must be validated or replaced by trusted wrapper before execution; deny privileged, host mounts/network, Docker socket, broad ports. Map only allowed loopback port.

Path policy: canonicalize registered repo root and generated workspaces, reject `..`, absolute target from model, symlink/junction escapes, alternate data streams on Windows, hidden sensitive paths and `.git` mutation except broker. Validate path claims by normalized path segment, case sensitivity appropriate to filesystem, not raw prefix. Read large/binary files metadata only. Broker imports changes using safe patch/commit inspection; deny unexpected paths and protected tests. Git refs generated and validated; no force push, remote delete, reset user checkout, credential helpers from untrusted config. Command profiles preapproved by operator (build/test/preview). Use argument arrays, timeouts, cancellation and output limits. No direct arbitrary shell endpoint. Engine may run code within sandbox under approved policy only.

## External authorization and audit

Read scope consent at connection; writes require exact approved plan. Approval expires, revocable before dispatch; action service rechecks scope, provider permissions and remote revision. Record actor, timestamps, hashes, target, diff summary and outcome. Branch protection honored, no policy bypass. No global transaction; provide partial state and approved compensation. Agent output can only propose policy amendments. Deterministic guard evidence tied to commit SHA, no stale results. Audit export redacted, project deletion local only. Sensitive cache hidden immediately on known access revoke; purge per policy. ADO/Figma content may include personal data; minimize scope, do not copy whole org. Explicit browser open original URL safe scheme and no credential query.

## Local setup and configuration

Prereqs: supported .NET SDK, Node LTS, Git, PostgreSQL via Compose, Docker/VM sandbox, validated CLI, authorized provider credentials, demo ADO/Figma. Bootstrap script checks versions, ports and paths; fail with actionable instructions. Example `.env.example` placeholders only:

```dotenv
ASPNETCORE_URLS=http://127.0.0.1:5080
DEVPRISM_DB_HOST=127.0.0.1
DEVPRISM_DB_PORT=55432
DEVPRISM_DB_NAME=devprism
DEVPRISM_WORKSPACE_ROOT=<registered directory>
DEVPRISM_SECRET_BACKEND=<os-vault-or-session>
```

Prefer secure local origin for production; HTTP loopback dev only after risk review. Generate DB password at setup, do not ship common default. App applies migrations using explicit command; backup before destructive migrations. Suggested commands after implementation: `docker compose -f infra/compose.yaml up -d db`, `dotnet restore`, `dotnet ef database update` with documented project/startup flags, `dotnet run --project src/DevPrism.Api`, `npm ci && npm run dev` from web. Actual commands validated in G1 README, not promises that scaffold already exists. Health readiness includes DB and scheduler; connector health separate; no sensitive config on public health response.

## Resilience and observability

Retry reads bounded exponential jitter; honor Retry-After. External write uncertainty reconcile first. Worker heartbeat/lease expiry; on restart mark stale running as Interrupted, verify/terminate owned process, reconcile actions. Never retry non-idempotent write simply because timeout. UI event replay after disconnect. Logs structured correlation IDs and bounded retention, no hidden reasoning, redacted. Budget cap, max concurrency, pause/cancel. Recovery drill: backup DB + permitted artifacts encrypted at rest; restore into separate local instance with credentials reentered, verify schema and no auto-resume writes until operator reviews. Monitor disk usage; prune logs/previews/workspaces only when safe and not referenced. Emergency stop prevents new jobs and terminates owned coding processes; in-flight external call may finish, reconcile. If suspicious repo/secret leak, cancel, revoke credentials, isolate workspace, preserve minimal evidence, review before resuming.
