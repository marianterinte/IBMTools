import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Badge, Icon } from './WorkspaceUi'

export type McpProvider = 'AzureDevOpsBoards' | 'Figma'
type Profile = {
  provider: McpProvider; endpoint: string | null; transport: string; enabled: boolean
  scope: 'default' | 'global' | 'project'; ownVersion: string | null; effectiveVersion: string | null
  authorizationConfigured: boolean; status: string; checkedAt: string | null
}
type Check = { status: string; sourceStatus: string | null; authorizationUrl: string | null }
type DesktopObservation = { status: 'Connected' | 'ToolNotFound' | 'Unavailable'; checkedAt: string }
type FigmaCredentialStatus = { configured: boolean; canEdit: boolean; source: 'None' | 'Local' | 'External' | 'Unreadable' }
type Action = 'connect' | 'refresh' | 'disconnect'

const figmaCredentialPath = '/api/v1/mcp/figma-credentials'

const statusLabels: Record<string, string> = {
  NotChecked: 'Not checked', Connected: 'Connected to MCP', Disabled: 'Disabled', EndpointRequired: 'Organization required',
  SignInRequired: 'Sign in required', AccessDenied: 'Access denied', RateLimited: 'Rate limited', Unavailable: 'Server unavailable',
  ClientSetupRequired: 'OAuth client setup required on this PC', AuthorizationFailed: 'Sign-in did not complete',
  AuthorizationPending: 'Waiting for browser authorization',
}
const sourceLabels: Record<string, string> = {
  ReadSucceeded: 'Work item read verified. Nothing was imported.',
  FrameResponseReceived: 'Figma returned text for the selected frame request. Content was not imported or independently verified.',
  OrganizationMismatch: 'This work item belongs to a different Azure DevOps organization.',
  WorkItemRequired: 'Save a specific Boards work item link to check it.',
  FrameRequired: 'Save a link to a specific frame to check it.',
  ReadToolUnavailable: 'The provider did not advertise the required read tool.',
  UnsupportedReadSchema: 'The provider advertised an unsupported read schema.',
  ReadRejected: 'The provider rejected the source read.', ReadUnavailable: 'The source read did not complete.',
  UnverifiedResponse: 'The response did not verify this source.',
}
const callbackLabels: Record<string, string> = {
  connected: 'Returned from Figma authorization. The current connection status for this PC is shown below.',
  unverified: 'Figma authorization returned, but the MCP connection was not verified.',
  failed: 'Figma did not complete authorization. Check client setup or try again.',
  changed: 'The MCP profile changed during authorization. Start again.',
  expired: 'The authorization request expired or was already used. Start again.',
  busy: 'Another MCP operation is running. Try again.',
}

function base(projectId?: string) {
  return projectId ? `/api/v1/projects/${encodeURIComponent(projectId)}/mcp/profiles` : '/api/v1/mcp/profiles'
}

async function read<T>(path: string, options?: RequestInit): Promise<T> {
  const result = await fetch(path, { cache: 'no-store', ...options })
  if (result.status === 400) throw new Error('Use the official MCP endpoint for this provider.')
  if (result.status === 409) throw new Error('Settings changed or another MCP operation is running. Refresh and try again.')
  if (result.status === 404) throw new Error('This MCP profile or source could not be found.')
  if (!result.ok) throw new Error('MCP settings are temporarily unavailable.')
  return result.json()
}

function send<T>(path: string, method: 'POST' | 'PUT', payload: object): Promise<T> {
  return read(path, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
}

async function figmaCredentialRequest(options?: RequestInit): Promise<FigmaCredentialStatus> {
  const result = await fetch(figmaCredentialPath, { ...options, redirect: 'error', cache: 'no-store', credentials: 'same-origin' })
  if (result.status === 400) throw new Error('Enter a Client ID and a Client Secret without spaces or control characters.')
  if (result.status === 409) throw new Error('Credentials cannot be changed right now. Refresh and try again.')
  if (!result.ok) throw new Error('Protected Figma credential storage is temporarily unavailable.')
  return result.json()
}

function useProfiles(projectId?: string) {
  return useQuery({ queryKey: ['mcp-profiles', projectId ?? null],
    queryFn: ({ signal }) => read<Profile[]>(base(projectId), { signal }) })
}

export function McpProfilesPanel({ projectId }: { projectId?: string }) {
  const profiles = useProfiles(projectId)
  const [params] = useSearchParams()
  const callbackKey = params.get('mcpAuth') ?? ''
  const callback = Object.hasOwn(callbackLabels, callbackKey) ? callbackLabels[callbackKey] : undefined
  return <>
    {callback ? <p className="notice" role="status">{callback}</p> : null}
    {!projectId ? <FigmaDesktopPanel /> : null}
    {profiles.isPending ? <p role="status">Loading MCP profiles…</p> : null}
    {profiles.error ? <p className="notice error" role="alert">{profiles.error.message}</p> : null}
    {profiles.data?.map((profile) => <McpProfileForm key={`${projectId ?? 'global'}:${profile.provider}:${profile.ownVersion}:${profile.effectiveVersion}`}
      profile={profile} projectId={projectId} />)}
  </>
}

function FigmaDesktopPanel() {
  const titleId = useId()
  const check = useMutation({
    mutationFn: () => read<DesktopObservation>('/api/v1/mcp/figma-desktop/check', { method: 'POST' }),
  })
  const result = check.data
  const connected = result?.status === 'Connected'
  const label = check.isPending ? 'Checking…' : connected ? 'Connected to Figma Desktop'
    : result?.status === 'ToolNotFound' ? 'Design tool not found'
    : result?.status === 'Unavailable' ? 'Could not connect' : check.isError ? 'Check failed' : 'Not checked'
  return <section className="panel" aria-labelledby={titleId}>
    <div className="panel-heading">
      <span className="icon-tile"><Icon name="design" /></span>
      <div><h2 id={titleId}>Figma Desktop MCP</h2><p>This PC only · No OAuth setup</p></div>
      <Badge tone={connected ? 'good' : result || check.isError ? 'warning' : 'neutral'}>{label}</Badge>
    </div>
    <p>In Figma Desktop, open a design, switch to <strong>Dev Mode</strong> and enable the desktop MCP server. Leave Figma Desktop open on this PC.</p>
    <div className="button-row">
      <button type="button" className="button button-primary" disabled={check.isPending} onClick={() => check.mutate()}>
        <Icon name="refresh" /> Check Desktop connection
      </button>
      <a className="text-link" href="https://developers.figma.com/docs/figma-mcp-server/local-server-installation/" target="_blank" rel="noopener noreferrer">Figma Desktop setup <Icon name="arrow" /></a>
    </div>
    <p className="secondary">This checks the local connection only. It does not change the Remote profile or enable project source reads through Desktop.</p>
    {result ? <p className="notice" role="status">
      {connected ? 'Figma Desktop responded and listed its design tool. No file was read or imported.'
        : result.status === 'ToolNotFound' ? 'The local MCP server responded without listing the Figma design tool.'
        : 'The local MCP check did not complete. Check that Figma Desktop is open and its MCP server is enabled, then try again.'}
    </p> : null}
    {result ? <p className="secondary">Last checked on this PC: {new Date(result.checkedAt).toLocaleString()}</p> : null}
    {check.error ? <p className="notice error" role="alert">{check.error.message}</p> : null}
  </section>
}

function FigmaCredentialSetup({ working, setWorking, onUpdated }: {
  working: boolean; setWorking: (value: boolean) => void; onUpdated: () => void
}) {
  const id = useId()
  const client = useQueryClient()
  const credentials = useQuery({ queryKey: ['figma-credential-status'],
    queryFn: ({ signal }) => figmaCredentialRequest({ signal }) })
  const [feedback, setFeedback] = useState<{ text: string; error?: boolean }>()

  async function update(request: Promise<FigmaCredentialStatus>, message: string) {
    setWorking(true)
    setFeedback(undefined)
    try {
      const result = await request
      // Do not let an older in-flight GET overwrite the saved state.
      await client.cancelQueries({ queryKey: ['figma-credential-status'] })
      client.setQueryData(['figma-credential-status'], result)
      onUpdated()
      await client.invalidateQueries({ queryKey: ['mcp-profiles'] })
      setFeedback({ text: message })
    } catch (error) {
      setFeedback({ text: error instanceof Error ? error.message : 'The credential change failed.', error: true })
    } finally {
      setWorking(false)
    }
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (working || !credentials.data?.canEdit) return
    const form = event.currentTarget
    const values = new FormData(form)
    const clientId = String(values.get('clientId') ?? '').trim()
    const clientSecret = String(values.get('clientSecret') ?? '')
    if (![clientId, clientSecret].every((value) => value.length > 0 && value.length <= 512 && !/[\s\p{Cc}]/u.test(value))) {
      setFeedback({ text: 'Enter a Client ID and a Client Secret without spaces or control characters.', error: true })
      return
    }
    // Submit directly; never retain the secret in a React Query mutation cache or browser storage.
    const request = figmaCredentialRequest({ method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ clientId, clientSecret }) })
    form.reset()
    void update(request, 'Credentials saved on this PC. Select Connect to authorize with Figma.')
  }

  function remove() {
    if (working || !credentials.data?.canEdit || !window.confirm('Remove the local Figma credentials and sign out all Figma profiles on this PC?')) return
    void update(figmaCredentialRequest({ method: 'DELETE' }), 'Local Figma credentials removed.')
  }

  const state = credentials.data
  return <>
    <h3>Figma Remote OAuth client</h3>
    <p>Creating an app in Figma My Apps and registering the callback does not grant MCP access. For a new MCP client, follow <strong>Join the waitlist</strong> in the <a href="https://developers.figma.com/docs/figma-mcp-server/remote-server-installation/" target="_blank" rel="noopener noreferrer">official Figma Remote setup guide</a>.</p>
    <p>Register <code>https://localhost:5081/oauth/mcp/figma</code> for the eligible client. Saving credentials here does not contact Figma or prove approval.</p>
    {credentials.isPending ? <p role="status">Checking local credential setup…</p> : null}
    {credentials.isError ? <p role="alert">Could not load the local credential status.</p> : null}
    {state?.source === 'External' ? <p>These credentials are managed by backend configuration. They cannot be changed here.</p> : null}
    {state && !state.canEdit && state.source !== 'External' ? <p>Saving from this page requires Windows protected storage. Use backend configuration on this system.</p> : null}
    {state?.source === 'Unreadable' ? <p role="status">The saved credentials could not be read. Replace or remove them.</p> : null}
    {state?.canEdit ? <form onSubmit={save} method="post" autoComplete="off" aria-label="Figma Remote credentials">
      <p className="secondary">{state.configured ? 'Credentials are saved on this PC. Enter both values to replace them.' : 'Enter the Client ID and Client Secret provided by Figma.'} The secret is not shown again. Changes sign out all Figma profiles on this PC.</p>
      <div className="form-field">
        <label htmlFor={`${id}-client`}>Client ID</label>
        <input id={`${id}-client`} name="clientId" type="text" autoComplete="off" spellCheck={false} maxLength={512} required disabled={working} />
      </div>
      <div className="form-field">
        <label htmlFor={`${id}-secret`}>Client Secret</label>
        <input id={`${id}-secret`} name="clientSecret" type="password" autoComplete="new-password" maxLength={512} required disabled={working} />
      </div>
      <div className="button-row">
        <button type="submit" className="button button-primary" disabled={working}>Save credentials</button>
        {state.source === 'Local' || state.source === 'Unreadable' ? <button type="button" className="button button-secondary" disabled={working} onClick={remove}>Remove local credentials</button> : null}
      </div>
    </form> : null}
    {feedback ? <p className={`notice${feedback.error ? ' error' : ''}`} role={feedback.error ? 'alert' : 'status'}>{feedback.text}</p> : null}
  </>
}

function McpProfileForm({ profile, projectId }: { profile: Profile; projectId?: string }) {
  const client = useQueryClient()
  const id = useId()
  const [endpoint, setEndpoint] = useState(profile.endpoint ?? '')
  const [enabled, setEnabled] = useState(profile.enabled)
  const [message, setMessage] = useState('')
  const [showFigmaSetup, setShowFigmaSetup] = useState(false)
  const [credentialBusy, setCredentialBusy] = useState(false)
  const figma = profile.provider === 'Figma'
  const needsFigmaSetup = figma && !profile.authorizationConfigured && profile.status !== 'Connected'
  const title = figma ? 'Figma MCP' : 'Azure DevOps MCP'
  const profilePath = `${base(projectId)}/${profile.provider}`
  const invalidate = () => client.invalidateQueries({ queryKey: ['mcp-profiles'] })
  const pendingAuthorization = message === statusLabels.AuthorizationPending

  const change = useMutation({
    mutationFn: (reset: boolean) => reset
      ? send<Profile>(`${profilePath}/reset`, 'POST', { expectedVersion: profile.ownVersion })
      : send<Profile>(profilePath, 'PUT', { endpoint: endpoint.trim() || null, enabled, expectedVersion: profile.ownVersion }),
    onSettled: invalidate,
  })
  const action = useMutation({
    mutationFn: (operation: Action) => send<Check>(`${profilePath}/${operation}`, 'POST', reference(profile)),
    onSuccess: (result, operation) => {
      const label = needsFigmaSetup && operation === 'refresh' && result.status === 'SignInRequired'
        ? 'Figma requires MCP authorization. Set up this app’s OAuth client before signing in.'
        : statusLabels[result.status]
      setMessage([label, result.sourceStatus && sourceLabels[result.sourceStatus]].filter(Boolean).join(' · '))
      if (!result.authorizationUrl) return
      const url = new URL(result.authorizationUrl)
      if (url.origin === 'https://www.figma.com' && url.pathname === '/oauth/mcp') window.location.assign(url.href)
    },
    onSettled: invalidate,
  })
  const busy = change.isPending || action.isPending || credentialBusy
  const unsaved = endpoint.trim() !== (profile.endpoint ?? '') || enabled !== profile.enabled

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!busy) { setMessage(''); change.mutate(false) }
  }

  return <section className="panel" aria-labelledby={`${id}-title`}>
    <div className="panel-heading">
      <span className="icon-tile"><Icon name={figma ? 'design' : 'boards'} /></span>
      <div><h2 id={`${id}-title`}>{title}</h2><p>{figma ? 'Remote · ' : ''}Streamable HTTP · {profile.scope === 'project' ? 'Project override' : profile.scope === 'global' ? 'Global profile' : 'Built-in default'}</p></div>
      <Badge tone={needsFigmaSetup && profile.enabled ? 'warning' : profile.status === 'Connected' ? 'good' : 'neutral'}>
        {needsFigmaSetup && profile.enabled ? 'OAuth setup required' : statusLabels[profile.status] ?? 'Unknown status'}
      </Badge>
    </div>
    <form onSubmit={save}>
      <div className="form-field">
        <label htmlFor={id}>MCP server URL</label>
        <input id={id} type="url" maxLength={255} value={endpoint} disabled={busy} spellCheck={false} autoComplete="off"
          placeholder={figma ? 'https://mcp.figma.com/mcp' : 'https://mcp.dev.azure.com/organization'}
          onChange={(event) => setEndpoint(event.target.value)} aria-describedby={`${id}-hint`} />
        <small id={`${id}-hint`} className="secondary">{figma ? 'This profile uses the official Figma Remote MCP endpoint.' : 'Use the official MCP URL for your Azure DevOps organization, not the Boards web link.'}</small>
      </div>
      <label className="mcp-enabled"><input type="checkbox" checked={enabled} disabled={busy} onChange={(event) => setEnabled(event.target.checked)} /> Enable MCP profile</label>
      <div className="button-row">
        <button className="button button-primary" type="submit" disabled={busy}>{projectId ? 'Save project override' : 'Save global settings'}</button>
        {profile.ownVersion ? <button className="button button-secondary" type="button" disabled={busy}
          onClick={() => { setMessage(''); change.mutate(true) }}>{projectId ? 'Use global profile' : 'Reset to defaults'}</button> : null}
      </div>
    </form>
    <div className="button-row" aria-label="MCP connection actions">
      {!needsFigmaSetup ? <button type="button" className="button button-secondary" disabled={busy || unsaved || pendingAuthorization || !profile.enabled || !profile.endpoint || !profile.authorizationConfigured}
        onClick={() => { setMessage(''); action.mutate('connect') }}>{action.isPending && action.variables === 'connect' ? 'Waiting for sign-in…' : 'Connect'}</button> : null}
      {figma && !projectId ? <button type="button" className="button button-secondary" disabled={busy} aria-expanded={showFigmaSetup} aria-controls={`${id}-oauth-setup`}
        onClick={() => setShowFigmaSetup(!showFigmaSetup)}>{needsFigmaSetup ? 'Set up Figma sign-in' : 'Manage Figma credentials'}</button> : null}
      {needsFigmaSetup && projectId ? <Link className="button button-secondary" to="/settings/mcp">Set up Figma sign-in in MCP settings</Link> : null}
      <button type="button" className="button button-secondary" disabled={busy || unsaved || !profile.enabled || !profile.endpoint}
        onClick={() => { setMessage(''); action.mutate('refresh') }}><Icon name="refresh" /> Refresh status</button>
      {!needsFigmaSetup ? <button type="button" className="button button-secondary" disabled={busy || unsaved || !profile.endpoint}
        onClick={() => { setMessage(''); action.mutate('disconnect') }}>Disconnect locally</button> : null}
    </div>
    {unsaved ? <p className="secondary">Save the profile before testing these settings.</p> : null}
    {needsFigmaSetup ? <p className="notice">Signing in to figma.com does not authorize DEV PRISM AI. Set up this app’s Remote OAuth client, then select Connect to authorize in the browser.</p> : null}
    {figma && !projectId ? <div id={`${id}-oauth-setup`} className="notice mcp-setup" hidden={!showFigmaSetup}>
      {showFigmaSetup ? <FigmaCredentialSetup working={busy} setWorking={setCredentialBusy} onUpdated={() => setMessage('')} /> : null}
    </div> : null}
    {!figma && !profile.authorizationConfigured ? <p className="notice">Configure an approved Microsoft Entra tenant and public client on this PC before signing in.</p> : null}
    {projectId && profile.scope !== 'project' ? <p className="secondary">Sign-in and local disconnect also apply to other projects that inherit this profile.</p> : null}
    {profile.checkedAt ? <p className="secondary">Last checked on this PC: {new Date(profile.checkedAt).toLocaleString()}</p> : null}
    {message ? <p className="notice" role="status">{message}</p> : null}
    {change.error || action.error ? <p className="notice error" role="alert">{(change.error ?? action.error)?.message}</p> : null}
  </section>
}

function reference(profile: Profile) {
  return { effectiveVersion: profile.effectiveVersion, scope: profile.scope }
}

export function McpReadCheck({ projectId, provider, selectionId }: {
  projectId: string; provider: McpProvider; selectionId: string
}) {
  const client = useQueryClient()
  const { data } = useProfiles(projectId)
  const profile = data?.find((entry) => entry.provider === provider)
  const check = useMutation({
    mutationFn: (active: Profile) => send<Check>(`${base(projectId)}/${provider}/sources/${encodeURIComponent(selectionId)}/check`, 'POST', reference(active)),
    onSettled: () => client.invalidateQueries({ queryKey: ['mcp-profiles'] }),
  })
  const result = check.data
  const current = check.variables?.effectiveVersion === profile?.effectiveVersion && check.variables?.scope === profile?.scope
  return <>
    <button type="button" className="button button-secondary" disabled={!profile || profile.status !== 'Connected' || check.isPending}
      onClick={() => profile && check.mutate(profile)}>{check.isPending ? 'Checking…' : 'Check source'}</button>
    {current && result ? <p className="notice mcp-source-result" role="status">{result.sourceStatus ? sourceLabels[result.sourceStatus] : statusLabels[result.status]}</p> : null}
    {check.error ? <p className="notice error mcp-source-result" role="alert">{check.error.message}</p> : null}
  </>
}
