# 16 — Technical validation and official references

Checked documentation on 2026-11-25; exact packages/API capabilities are deliberately not hardcoded from assumptions. These links support design choices, not proof that the operator's account works. G0 must validate installed versions, auth, permission and behavior. Record evidence in ADR and capability report. Use official docs matching pinned versions.

## External facts and references

| Component | Official reference | What it supports / what remains unknown |
|---|---|---|
| Azure DevOps MCP | [Microsoft repository](https://github.com/microsoft/azure-devops-mcp), [overview](https://learn.microsoft.com/en-us/azure/devops/mcp-server/mcp-server-overview?view=azure-devops) | Official MCP integration and domains for work items, repos, wiki etc.; actual tool catalog, local/remote support and account permissions must be probed. |
| ADO REST work item update | [Microsoft REST docs](https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/update?view=azure-devops-rest-7.1) | JSON Patch update and revision test pattern; process types/allowed fields, auth and API version verify in sandbox. |
| ADO Wiki | [Pages update docs](https://learn.microsoft.com/en-us/rest/api/azure/devops/wiki/pages/update?view=azure-devops-rest-7.1) | ETag/version on update; wiki type and write semantics must be tested. |
| Figma MCP | [Introduction](https://developers.figma.com/docs/figma-mcp-server/), [remote setup](https://developers.figma.com/docs/figma-mcp-server/remote-server-installation/), [rate limits/access](https://developers.figma.com/docs/figma-mcp-server/rate-limits-access/) | Remote server supports native canvas writes with appropriate access; custom client/OAuth/seat/tool compatibility and safe verification are not proven. |
| Figma REST | [File endpoints](https://developers.figma.com/docs/rest-api/file-endpoints/), [rate limits](https://developers.figma.com/docs/rest-api/rate-limits/) | Node/file metadata and image render/read, plan/seat rate limits. Do not assume REST supports arbitrary canvas edits. |
| MCP C# SDK | [Official repository](https://github.com/modelcontextprotocol/csharp-sdk) | Client/server SDK maintained with Microsoft; verify stdio/HTTP/OAuth support in chosen release. |
| OpenCode CLI | [CLI commands](https://opencode.ai/docs/cli/commands/), [providers](https://opencode.ai/docs/providers), [permissions](https://opencode.ai/docs/permissions/) | Noninteractive `run --format json`, provider flexibility and permission settings; version-specific configuration, IBM endpoint, usage and safe cancellation need test. |
| OpenCode versions | [Migration guide](https://opencode.ai/docs/migrate-v1) | Config schema varies; do not mix V1/V2 permission examples. |
| ASP.NET Core | [SignalR with background services](https://learn.microsoft.com/en-us/aspnet/core/signalr/background-services?view=aspnetcore-10.0) | Background worker can publish SignalR events; durable queue/outbox still our responsibility. |
| Atlassian (P2) | [Rovo MCP overview](https://developer.atlassian.com/cloud/rovo-mcp/), [tools](https://developer.atlassian.com/cloud/rovo-mcp/guides/supported-tools/), [authentication](https://developer.atlassian.com/cloud/rovo-mcp/guides/authentication-and-authorization/) | Jira/Confluence access and actions subject to OAuth/token scopes and org settings. Not in MVP critical path. |

## Local MVP G0: proof before assumptions

No provider probes are run by this documentation revision. Before implementation, consult the official documentation current at execution time and pin the verified remote endpoint, transport, SDK version, auth flow and scopes per profile. A Microsoft local MCP executable is not proof of an Azure DevOps remote service. The existence of an official remote Figma URL is not proof that this .NET client, account, seat or selected tool is authorized.

| Probe | Pass criterion | Failure decision |
|---|---|---|
| MVP-SP1 ADO remote Boards | Successful scoped invocation yields the selected real work item with usable fields, and identity reconciles with provider original. Record redacted capability evidence. | Gate blocked if only local stdio, REST, tool discovery or read without usable data works. |
| MVP-SP2 Figma remote frame | Successful scoped invocation yields context attributable to the selected real frame. Record actual fields and revision/preview limitations. | Gate blocked if no usable frame context; missing rendered preview does not block it. |
| MVP-SP3 auxiliary and fault handling | Check Wiki, Repos and preview independently; record missing permissions, feature limits and unavailable routes. Use controlled scenarios for denial/rate failure rather than consuming quotas or changing employer access. | Record limitation or request an explicit auxiliary fallback decision; no background or silent switch. |
| MVP-SP4 laptop readiness | Record exact Windows host and tooling, local HTTPS/single-operator trust boundary, vault and PostgreSQL availability, restart and endpoint support. Pin supported .NET/Node/MCP library and DB versions after proof. | Report missing prerequisites before packaging; no insecure prod-like bypass. |

Extend the capability report with provider original (redacted), tool and mapping version, normalized fields received, missing fields, route, timestamp, and observed result. A successful G0 probe does not replace the G3 acceptance run through DEV PRISM AI. Owner approval for implementation is separate from the authorization to use an account.

## Post-MVP spike cards (run on disposable assets only)

### SP-01 AI provider

Inputs: authorized endpoint, credential, model name and provider policy; never paste key into agent conversation. Verify minimal text response, structured plan sample, tool call and tool result continuation, streaming cancellation, usage fields, context limits/error handling. Check TLS/host allowlist, sanitized logs. Record provider/model/version, actual request shape and supported capabilities, no key or raw confidential content. If IBM API is OpenAI-compatible, prove with actual call; if different, implement provider adapter later or choose approved OpenAI. Cost reporting unknown until observed.

### SP-02 CLI and sandbox

Record exact binary/version, noninteractive command, provider config and permission schema. In disposable repo request one deterministic edit; capture events and independent test. Test denied path outside workspace, no host credential environment, no unapproved outbound connection, cancellation of child process tree, timeout, stderr/stdout limits, process restart handling. Verify safe prompt input method, no unauthorized push. Pass only if actual code changed and proof plus boundary tests. Alternate authorized Codex adapter if OpenCode incompatible; if both fail, coding unavailable, proceed read/action product. Do not claim agent CLI is a .NET library; it is a separately installed executable orchestrated by .NET.

### SP-03 ADO/MCP/REST

Provision dedicated project with Git repo and Wiki. Probe official MCP tools/domains and REST access. Read work item metadata/types, relation, revision. With explicit consent create disposable item, update exact fields guarded by current revision, simulate stale update, reread ID/rev. Test process state mapping and PR read/draft creation if allowed; merge is separate live gate. Wiki create/update in dedicated page, verify ETag or code-wiki workflow and stale rejection. Document OAuth/PAT policy and exact supported adapter mapping. No bypass rules or production project.

### SP-04 Figma

Dedicated file/frame, authorized scoped read; parse file/node IDs, read metadata/version, preview image, test rate-limit handling with fixture (do not intentionally exhaust real quota). Probe remote MCP handshake/auth/list tools, supported client and edit permissions; only optional disposable native edit with explicit consent. Confirm actual changed node/frame by reread and original; if no safe version guard, use manual handoff and label unsupported. Keep design viewing usable even when model or write capability absent.

### SP-05 host platform and setup

Record Windows/WSL2 or Linux, Docker version/constraints, .NET/Node/Git/Postgres versions and ports. Verify local auth/origin defense, DB separate from target app, isolated preview, network policy and secret store. If container restrictions unavailable, no real unattended coding; fixture read/actions remain. Validate bootstrap and repeatable clean start. Avoid deploying cloud infrastructure for demo.

## Capability report template

```
Capability key:
Provider/tool version:
Configured scope (redacted):
Authentication kind and least scopes:
Probe performed / exact command or request type:
Observed result + evidence path:
Available / Unavailable / Unverified:
Known restrictions / retry or manual fallback:
Owner approval required:
```

No production validation completed in this specification package. External docs may change; recheck at execution time. The chosen stack, engine and priorities are design defaults, not user-mandated exact package choices.
