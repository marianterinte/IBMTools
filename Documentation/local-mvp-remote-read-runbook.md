# Authorize and prove the two remote MCP reads

This is the handoff for G0. It does not authorize any account, install a provider client, record live success, or supply a REST/fixture substitute. Compare results against the [capability report](local-mvp-g0-evidence.md) and [connector acceptance matrix](dev-prism-ai-specs/06_CONNECTORS_AND_SYNC.md#local-mvp-capability-matrix). Only use dedicated online demo assets with synthetic content. No employer or customer data.

## Prerequisites the operator can confirm without sharing secrets

| Provider | Prepare in the provider UI | Auth constraint to prove |
|---|---|---|
| Azure DevOps | An authorized project in an Azure DevOps organization backed by Microsoft Entra ID. Prepare one disposable Boards work item with a meaningful description, acceptance criteria or state. Verify the operator can see it in the original UI. | Follow the [official remote guide](https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server?view=azure-devops). Confirm the approved client registration/consent path and that the organization permits the expected read. Do not use a PAT with an unsupported remote flow. |
| Figma | An authorized synthetic design file and a selected frame with readable text or an explicit rating annotation. Verify the operator can see it in Figma. | Follow the [official remote setup](https://developers.figma.com/docs/figma-mcp-server/remote-server-installation/). Confirm the custom client is eligible and has an approved auth flow for the test account; seat, rate limits and permissions must be observed. |

Do not paste credentials, authorization codes, callback URLs, tokens, signed assets or private payloads into chat, config files checked into Git, or screenshots. The operator completes provider sign-in in the approved browser/client flow. A ready tenant or file is not proof that PRISM's client can authenticate. If the flow requires consent from an administrator, wait for that approval. Never change tenant settings to bypass the requirement.

## Read-only verification sequence

1. Record the provider profile, supported HTTP transport and .NET MCP client version. Authenticate only to the intended official endpoint. Negotiate the session and record which read capability is available, without dumping the complete response or advertising a write capability.
2. Invoke only that read for the preselected project/item or file/frame. Compare the result to the original. Record the minimum fields received, whether identity was returned or explicitly bound to the selection, and whether a native version was supplied. A discovery response or echoed URL does not pass.
3. Record `Verified live`, `Unverified`, or `Blocked` separately for Boards and frame context. Report Wiki, Repos and rendered preview independently if tested. No automatic alternate route is permitted. A supported third-party client's successful read is useful preliminary evidence, not proof of the intended .NET client or PRISM feature.
4. Once both mappings are proven, implement the typed adapters against the observed schemas. Then rehearse the actual application flow on the laptop; the lab probe alone does not pass M02/M03. See [manual acceptance](dev-prism-ai-specs/13_TESTING_AND_ACCEPTANCE.md#local-mvp-acceptance-run-m01m10).

## Evidence to retain

Record the date, provider, transport, SDK/client version, approved scope summary, read capability, sanitized example fields, native revision present/absent, observed outcome, and next action. If blocked, state whether the issue is account access, client registration, tenant or provider permission, unsupported operation, missing payload fields, or rate limit. Never change the status to successful based on published documentation alone.
