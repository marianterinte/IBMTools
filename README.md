# DEV PRISM AI

A local, feature-oriented workspace for Azure DevOps and Figma. Only the app and PostgreSQL run on the workstation. Required provider reads use real remote MCP. See the [Local MVP scope](Documentation/dev-prism-ai-specs/00_START_HERE.md), [G0 evidence](Documentation/local-mvp-g0-evidence.md) and [remote read runbook](Documentation/local-mvp-remote-read-runbook.md).

## Current increment

Open [https://localhost:5081](https://localhost:5081) after starting the packaged app. When PostgreSQL is ready, the project workspace opens directly, with no local login, pairing or session. Only use this on a trusted, single-operator workstation: loopback HTTPS, Host and browser Origin restrictions do not isolate local users or processes. Provider authorization is separate.

Local projects, features, versioned decision memory and a scoped sync history are persisted. Sync history is a foundation only: no provider connector or import worker is enabled. Local decision evidence is currently an operator statement. The [local OpenAPI document](https://localhost:5081/api/openapi/v1.json) describes the endpoints currently exposed.

**Windows verification:** Release build, web lint/type/build and packaged HTTPS smoke passed. All four EF migrations applied on native PostgreSQL 17; rerunning the update was a no-op. Readiness returned `{"database":"ready"}` and a temporary HTTPS API project was independently located by SQL in `dev_prism_ai` on `127.0.0.1:5432` as `dev_prism_ai_app`, then removed. The packaged frontend and project API responded without a session. Foreign browser origins and writes without Origin returned `403`. The previous Podman database was not changed.

**Workspace UI verification:** On the packaged HTTPS app, a Chrome smoke walkthrough created a project and feature, searched by feature key, drafted and explicitly approved a local decision, and reopened that decision in the context rail after reload. It also checked provider and sync history empty/availability states and the responsive layout from 320 to 1440 pixels, with no browser errors or third-party requests. These are local verification results, not provider or integrated release evidence.

## Build and run on Windows

Use .NET SDK 10.0.404 and Node.js 24.22.3. Install the pinned versions or prepend the corresponding toolchain directories to PATH. From the repository root in PowerShell:

```powershell
dotnet dev-certs https --trust
Push-Location web
npm.cmd ci
npm.cmd run lint
npm.cmd run build
Pop-Location
dotnet restore DevPrism.slnx --locked-mode
dotnet build DevPrism.slnx -c Release --no-restore
dotnet publish src/DevPrism.Api/DevPrism.Api.csproj -c Release -o .local/publish --no-restore
```

Configure and migrate one of the databases below in the **same PowerShell process**, then run:

```powershell
Push-Location .local/publish
try { dotnet DevPrism.Api.dll } finally { Pop-Location; Remove-Item Env:ConnectionStrings__DevPrism -ErrorAction SilentlyContinue }
```

The packaged app listens on loopback HTTPS at `https://localhost:5081`. `/api/v1/health` reports DB readiness only: `ready` (200), or `notConfigured`, `migrationRequired` or `unreachable` (503). For frontend development, Vite at `http://127.0.0.1:5173` proxies only readiness, not workspace data. On Windows, set `NODE_USE_SYSTEM_CA=1` so Node can validate the trusted ASP.NET development certificate. Use the packaged HTTPS origin for project CRUD. Unknown API/media URLs return errors rather than the SPA fallback.

## Local PostgreSQL setup

Choose native PostgreSQL 17 **or** the Compose database. Do not share an unrelated database or use a PostgreSQL admin login for the application. Keep secrets in a private, OS-protected store where possible and inject them into process configuration; never commit them or print resolved config. Back up before applying a destructive migration. The runtime does not create, migrate or reset the DB automatically.

### Native PostgreSQL 17

Provision a dedicated database (e.g. `dev_prism_ai`) and login/owner (e.g. `dev_prism_ai_app`) with a unique password. Use a non-superuser without cluster-wide `CREATEDB`, `CREATEROLE` or replication privileges, and restrict the new database to intended roles. The app connects to `127.0.0.1:5432` by default in this example; verify the server's listener, firewall and access rules separately, since a loopback connection string does not constrain where the server listens. Set `ConnectionStrings__DevPrism` in the shell using the private connection string from your credential store, in the form `Host=127.0.0.1;Port=5432;Database=dev_prism_ai;Username=dev_prism_ai_app;Password=<secret>`. Use connection-string quoting or a builder for values containing delimiters. No Docker or Podman is needed for this option.

### Compose-managed PostgreSQL 17

Use Docker Compose or a verified Compose provider backed by Podman. The database uses an independent persistent volume and publishes loopback port 55432. Generate a local password once and preserve it for the same volume:

```powershell
New-Item -ItemType Directory -Force .local | Out-Null
if (-not (Test-Path .local/db-password)) {
  $bytes = New-Object byte[] 32
  $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
  try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
  [IO.File]::WriteAllText((Join-Path (Get-Location).Path '.local/db-password'), [BitConverter]::ToString($bytes).Replace('-', ''))
}
$env:DEVPRISM_DB_PASSWORD = (Get-Content .local/db-password -Raw).Trim()
$env:ConnectionStrings__DevPrism = 'Host=127.0.0.1;Port=55432;Database=devprism;Username=devprism;Password=' + $env:DEVPRISM_DB_PASSWORD
docker compose -p devprism-local -f infra/compose.yaml up -d --wait
Remove-Item Env:DEVPRISM_DB_PASSWORD
```

When using Podman, start the existing machine and use its Compose provider or a verified standalone binary targeting the Docker-compatible named pipe. Keep the ignored password file private. The Compose setup was verified with PostgreSQL 17.9 under Podman 5.4.1 and Compose 5.8.0; the initial migration was idempotent and a synthetic project survived a database restart.

### Apply migrations for either option

From the repository root in the configured shell:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/DevPrism.Infrastructure --startup-project src/DevPrism.Api
```
