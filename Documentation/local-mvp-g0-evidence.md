# Local MVP G0: capability evidence

**Date:** 2026-11-25

**Status:** public references and host inventory observed; required authenticated reads not yet proven.

This is an implementation evidence record, separate from the versioned specification pack. See the [connector matrix](dev-prism-ai-specs/06_CONNECTORS_AND_SYNC.md#local-mvp-capability-matrix) and [roadmap](dev-prism-ai-specs/14_IMPLEMENTATION_ROADMAP.md#g0--prove-remote-read-capabilities-local-mvp).

| Gate | Status | Evidence | Required next proof |
|---|---|---|---|
| MVP-SP1 / ADO Boards | Unverified | The [Microsoft remote server guide](https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server?view=azure-devops) returned HTTP 200. It documents an organization-specific HTTPS endpoint, Microsoft Entra authentication, and an Entra-backed Azure DevOps organization. Custom client registration and tenant consent may be required for the supported auth path. | Sign in using the verified client auth path for an authorized synthetic project, list/map the permitted read tool, and read a selected work item with a usable title and fields. A public guide is not account or client proof. |
| MVP-SP2 / Figma frame | Unverified | The [remote setup guide](https://developers.figma.com/docs/figma-mcp-server/remote-server-installation/) and [tools reference](https://developers.figma.com/docs/figma-mcp-server/tools-and-prompts/) returned HTTP 200. They document the HTTPS server and a design-context read candidate. | Verify the supported custom client auth path on an authorized account, then read usable context bound to a selected synthetic file/node. |
| MVP-SP3 / companion reads | Unverified | No account-scoped probes performed for Wiki, Repos or Figma preview. No REST exception approved. | Verify and record each separately, or explicitly leave it unavailable. |
| MVP-SP4 / laptop | Partial | Verified repo-scoped .NET 10.0.404 and Node 24.22.3, packaged loopback HTTPS/pairing, and migrations with PostgreSQL 17.9 on Podman 5.4.1 + Compose 5.8.0. System SDK remains unchanged. | Verify the provider credential store and selected remote MCP client SDK/auth flow before closing the host gate. |

**ADO probe preparation:** The official resource metadata was retrieved successfully and advertises `https://mcp.dev.azure.com/.default`. Microsoft documents `wit.read` and `wit_work_item` with the read-only `get` action. The isolated .NET probe uses MCP SDK 2.4.0 and MSAL 4.92.0; locked restore, Release build and the help path succeeded. The operator confirmed authorized synthetic source assets are available. An approved public client registration, interactive auth, actual schema and usable work item read have not been observed. Follow the [ADO runbook](local-mvp-remote-read-runbook.md#azure-devops-run-the-net-probe).

No provider credentials, account identifiers, MCP responses, remote mutations or live-demo claims were recorded. The roadmap tasks requiring live evidence remain unchecked. Independent local foundation work does not waive these gates.
