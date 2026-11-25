# Authorize and prove the two remote MCP reads

This is the handoff for G0. It does not authorize any account, install a provider client, record live success, or supply a REST/fixture substitute. Compare results against the [capability report](local-mvp-g0-evidence.md) and [connector acceptance matrix](dev-prism-ai-specs/06_CONNECTORS_AND_SYNC.md#local-mvp-capability-matrix). Only use dedicated online demo assets with synthetic content. No employer or customer data.

## Prerequisites the operator can confirm without sharing secrets

| Provider | Prepare in the provider UI | Auth constraint to prove |
|---|---|---|
| Azure DevOps | An authorized project in an Azure DevOps organization backed by Microsoft Entra ID. Prepare one disposable Boards work item with a meaningful description, acceptance criteria or state. Verify the operator can see it in the original UI. | Follow the [official remote guide](https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server?view=azure-devops). Confirm the approved client registration/consent path and that the organization permits the expected read. Do not use a PAT with an unsupported remote flow. |
| Figma | An authorized synthetic design file and a selected frame with readable text or an explicit rating annotation. Verify the operator can see it in Figma. | Follow the [official remote setup](https://developers.figma.com/docs/figma-mcp-server/remote-server-installation/). Confirm the custom client is eligible and has an approved auth flow for the test account; seat, rate limits and permissions must be observed. |

Do not paste credentials, authorization codes, callback URLs, tokens, signed assets or private payloads into chat, config files checked into Git, or screenshots. The operator completes provider sign-in in the approved browser/client flow. A ready tenant or file is not proof that PRISM's client can authenticate. If the flow requires consent from an administrator, wait for that approval. Never change tenant settings to bypass the requirement.

## Azure DevOps: run the .NET probe

1. Use a dedicated, approved Microsoft Entra app registration in the demo tenant. Under **Authentication → Mobile and desktop applications**, register the redirect URI `http://localhost` for the [MSAL system-browser flow](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/using-web-browsers). Use a public client, not a client secret; do not enable unrelated flows. Verify that the Azure DevOps MCP enterprise application is available in the tenant (application ID `2a72489c-aab2-4b65-b93a-a91edccf33b8`). Under API permissions, grant only its delegated `wit.read` permission and obtain any required consent. Do not grant legacy `Ado.Mcp.Tools`, write or manage access. Existing work item visibility in the provider UI is also required.
2. In an **interactive PowerShell terminal**, from the repository root, run:

   ```powershell
   .\.tools\dotnet-10.0.404\dotnet.exe run --project tools/DevPrism.AdoMcpProbe -c Release
   ```

   If you use a system-wide .NET 10.0.404 SDK instead, use `dotnet` in place of the repo-local executable. Enter the organization slug, exact project name, selected work item numeric ID, directory (tenant) ID and dedicated application (client) ID **in the local terminal**. Type `READ` to confirm the scope, then sign in with the authorized account in the system browser. Do not paste an access token or authorization code anywhere. The temporary loopback callback belongs to MSAL; the actual work-item read uses the official remote MCP server.
3. The probe uses the public MCP resource's documented `.default` OAuth scope with that dedicated registration and rejects missing or broader than expected grants. It negotiates Streamable HTTP, asks for the documented read-only `wit_work_item` tool, and only invokes `get` after checking the advertised input. It does not write a config, token, or payload file; logs are limited to a schema hash and sanitized outcome. It passes only if the result proves the exact item ID, source project, a nonempty title and another meaningful field. A missing native revision is reported separately. The probe is not part of the CLI, server or UI required to run PRISM.
4. Report only the sanitized result line or the `Not verified` reason. If the client, consent or schema is rejected, keep the gate blocked; no PAT, REST substitution, or broader grant. If the probe passes, still run M02 through the application before marking the MVP release complete.

## Read-only verification sequence

1. Record the provider profile, supported HTTP transport and .NET MCP client version. Authenticate only to the intended official endpoint. Negotiate the session and record which read capability is available, without dumping the complete response or advertising a write capability.
2. Invoke only that read for the preselected project/item or file/frame. Compare the result to the original. Record the minimum fields received, whether identity was returned or explicitly bound to the selection, and whether a native version was supplied. A discovery response or echoed URL does not pass.
3. Record `Verified live`, `Unverified`, or `Blocked` separately for Boards and frame context. Report Wiki, Repos and rendered preview independently if tested. No automatic alternate route is permitted. A supported third-party client's successful read is useful preliminary evidence, not proof of the intended .NET client or PRISM feature.
4. Once both mappings are proven, implement the typed adapters against the observed schemas. Then rehearse the actual application flow on the laptop; the lab probe alone does not pass M02/M03. See [manual acceptance](dev-prism-ai-specs/13_TESTING_AND_ACCEPTANCE.md#local-mvp-acceptance-run-m01m10).

## Evidence to retain

Record the date, provider, transport, SDK/client version, approved scope summary, read capability, sanitized example fields, native revision present/absent, observed outcome, and next action. If blocked, state whether the issue is account access, client registration, tenant or provider permission, unsupported operation, missing payload fields, or rate limit. Never change the status to successful based on published documentation alone.
