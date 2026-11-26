# MCP settings in DEV PRISM AI

The app is a Streamable HTTP MCP client for two approved remote providers. Open [MCP settings](https://localhost:5081/settings/mcp) for the global profiles and the Figma Desktop connection check, or **Connections** inside a project to override a profile and manage resource links. Changes to profiles do not import data or write to either provider.

| Provider | MCP server URL | Resource link saved in the project |
|---|---|---|
| Figma | `https://mcp.figma.com/mcp` (predefined) | A design file or frame URL. **Check source** requires a frame link with its node ID. |
| Azure DevOps | `https://mcp.dev.azure.com/<organization>` (replace the organization slug and save) | A Boards project or work item link. **Check source** requires a work item in the organization configured in the effective profile. |

Projects inherit the global settings unless you save a project override. **Use global profile** removes the override without deleting the source links. A project can select another approved Azure DevOps organization or disable a provider. Figma currently has one approved endpoint. The app rejects arbitrary server URLs and does not forward provider credentials to redirects. The database stores profile settings and resource links, **not OAuth tokens**. Inherited profiles share the same local sign-in; an override has its own local authorization. Sign in separately on another PC or after restarting the app. Only the theme preference is stored in the browser.

## Check Figma Desktop on this PC

1. On the same PC running DEV PRISM AI, open a design in Figma Desktop and enable the MCP server in Dev Mode. See the [Figma Desktop setup guide](https://developers.figma.com/docs/figma-mcp-server/local-server-installation/).
2. In **MCP settings → Figma Desktop MCP**, select **Check Desktop connection**. The backend initializes MCP at the fixed address `http://127.0.0.1:3845/mcp` and looks for the Figma design tool in the tool list. It sends no OAuth credentials and does not invoke any tools. No callback or OAuth client registration is needed for this check.
3. **Connected to Figma Desktop** is the result of the last manual check, not a persistent session or confirmation of access to any frame. The check does not store a profile, change global or project overrides, read any design, or satisfy the remote Figma release gate. Project **Check source** still uses the remote profile.

## Configure remote authorization on each PC

### Figma Remote

1. For a new custom MCP client, select **Join the waitlist** in the [official Figma Remote setup guide](https://developers.figma.com/docs/figma-mcp-server/remote-server-installation/). Identify DEV PRISM AI as your custom client and follow Figma's onboarding instructions. Do not include the Client Secret in the request. Creating a REST OAuth app in My Apps, saving a callback or publishing that app does not confirm remote MCP eligibility. No public approval indicator in My Apps is documented for this flow.
2. Use the eligible client's **Client ID** and **Client Secret**, with the exact redirect URI `https://localhost:5081/oauth/mcp/figma`. DEV PRISM AI uses authorization code with PKCE (S256) and requests `mcp:connect`.
3. On Windows, open **MCP settings → Figma MCP (Remote) → Set up Figma sign-in**. Enter both values and select **Save credentials**. The backend stores the pair using Windows DPAPI (current user) under `%LOCALAPPDATA%\DevPrismAi`. Neither value is returned by the credential API or saved to the shared database or browser storage. The values are active without restarting. **Manage Figma credentials** can replace or remove them, clearing local Figma sign-ins and any pending Figma authorization. Removing the local pair does not revoke the grant at Figma.
4. Select **Connect**, approve access in the browser, and look for **Connected to MCP**. Saving credentials only enables the attempt; it does not contact Figma or prove eligibility. A failed authorization alone does not diagnose whether it was an unapproved client, a mismatched callback, or another provider error.

Alternatively, supply `Mcp__Figma__ClientId` and `Mcp__Figma__ClientSecret` to the backend through an approved local secret store before startup. If either is supplied externally, the in-app editor is read-only. Use this method on platforms where the Windows store is not available. Never place the secret in chat, source control, command history, Neon, or regular application settings.

### Azure DevOps

Supply `Mcp__AzureDevOps__TenantId` and `Mcp__AzureDevOps__ClientId` to the backend at startup. Use a [Microsoft Entra public client](https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server?view=azure-devops) approved for the target tenant, with `http://localhost` registered for the MSAL system browser callback and delegated `wit.read` consent on the Azure DevOps MCP resource. No client secret or write permission is needed for Boards verification.

## Connect and verify

1. Save the intended profile and select **Connect**. Finish provider sign-in and any required consent in the browser. Figma redirects back to the originating settings page; Entra uses its own temporary system browser callback. Do not share any callback URL or authorization code.
2. **Connected to MCP** means authenticated MCP negotiation and tool discovery succeeded at the last check, not that any specific file or work item is readable. **Refresh status** rechecks through MCP without importing or renewing the OAuth grant; if authorization expired, select **Connect** again. **Disconnect locally** clears this process's authorization and observation, not the OAuth grant at the provider.
3. In the project's **Connections** view, save a link to an authorized disposable work item or frame and select **Check source**. For Boards, the ID, project, title, and an additional meaningful field must match. For Figma, the read is bound to the selected file and node; a text response is reported without claiming independent identity/content validation or importing it. Cached resource history, ingestion, and provider writes are not part of this increment.

## Evidence and limits

- On the development PostgreSQL database, migration `20261126081707_McpProfiles` added the profile table and indexes without rewriting previous data. The packaged app returned HTTP 200 for readiness, global settings and inherited project profiles. It rejected a missing Origin, an unapproved endpoint, a stale profile version, an unknown project, and an invalid OAuth callback state.
- The actual Figma endpoint required sign-in on an unauthenticated refresh. The browser walkthrough tested authorization return and source status with fixtures, not a real account. No authenticated Figma or Azure DevOps read was demonstrated in this increment. Both G0 gates remain open; see the [remote read runbook](local-mvp-remote-read-runbook.md) and [provider evidence](local-mvp-g0-evidence.md).
