# Local MVP G0: capability evidence

**Date:** 2026-11-25

**Status:** public references and host inventory observed; required authenticated reads not yet proven.

This is an implementation evidence record, separate from the versioned specification pack. See the [connector matrix](dev-prism-ai-specs/06_CONNECTORS_AND_SYNC.md#local-mvp-capability-matrix) and [roadmap](dev-prism-ai-specs/14_IMPLEMENTATION_ROADMAP.md#g0--prove-remote-read-capabilities-local-mvp).

| Gate | Status | Evidence | Required next proof |
|---|---|---|---|
| MVP-SP1 / ADO Boards | Unverified | The [Microsoft remote server guide](https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server?view=azure-devops) returned HTTP 200. It documents an organization-specific HTTPS endpoint, Microsoft Entra authentication, and an Entra-backed Azure DevOps organization. Custom client registration and tenant consent may be required for the supported auth path. | Sign in using the verified client auth path for an authorized synthetic project, list/map the permitted read tool, and read a selected work item with a usable title and fields. A public guide is not account or client proof. |
| MVP-SP2 / Figma frame | Unverified | The [remote setup guide](https://developers.figma.com/docs/figma-mcp-server/remote-server-installation/) and [tools reference](https://developers.figma.com/docs/figma-mcp-server/tools-and-prompts/) returned HTTP 200. They document the HTTPS server and a design-context read candidate. | Verify the supported custom client auth path on an authorized account, then read usable context bound to a selected synthetic file/node. |
| MVP-SP3 / companion reads | Unverified | No account-scoped probes performed for Wiki, Repos or Figma preview. No REST exception approved. | Verify and record each separately, or explicitly leave it unavailable. |
| MVP-SP4 / laptop | Partial | Windows x64; system .NET SDK 9.0.315, Node v23.11.1 (non-LTS) and npm 11.4.2. Docker CLI and native `psql` not found on PATH. Podman CLI 5.4.1 is installed but its configured WSL machine did not answer (`podman version`, exit 125). | Validate repo-scoped .NET 10 and Node LTS; verify a local PostgreSQL runtime, packaged local HTTPS, pairing and vault before closing the host gate. |

No provider credentials, account identifiers, MCP responses, remote mutations or live-demo claims were recorded. The roadmap tasks requiring live evidence remain unchecked. Independent local foundation work does not waive these gates.
