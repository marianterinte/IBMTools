# DEV PRISM AI

A local, feature-oriented workspace for Azure DevOps and Figma. The application and PostgreSQL run on the operator's laptop; the required provider reads use real remote MCP. See [Local MVP scope](Documentation/dev-prism-ai-specs/00_START_HERE.md) and [provider readiness](Documentation/local-mvp-g0-evidence.md).

## Current increment

The application runs over loopback HTTPS and serves the packaged React UI and local pairing. The paired operator can use the project API when PostgreSQL is configured and migrated. Readiness still reports `503` because the complete import stack is not available. No provider is contacted.

**G1.1 evidence (verified locally, Windows x64):** Release build, web lint/type/build and packaged publish succeeded with .NET SDK 10.0.404 and Node 24.22.3. The packaged HTTPS SPA returned 200, readiness returned 503 with scaffold status, and unknown API/media paths returned 404 instead of the SPA. This does not close the G1 gate.

**Local pairing / project API evidence:** The HTTPS smoke walkthrough verified unauthenticated denial, antiforgery and origin refusal, pairing with a Secure/HttpOnly cookie, local project creation and duplicate conflict, cross-owner non-disclosure, no-store responses and revocation of the previous session after sign-out. Provider credential setup remains unverified and is not exposed.

## Build and run on Windows

Prerequisites: .NET SDK 10.0.404 and Node.js 24.22.3 (see the toolchain pins). Either install supported SDKs or prepend your local toolchain directories to PATH. Run from the repository root in PowerShell:

```powershell
dotnet dev-certs https --trust
Set-Location web
npm.cmd ci
npm.cmd run lint
npm.cmd run build
Set-Location ..
dotnet build DevPrism.slnx -c Release
dotnet publish src/DevPrism.Api/DevPrism.Api.csproj -c Release -o .local/publish
$bytes = New-Object byte[] 32
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
$env:DevPrism__PairingCode = [BitConverter]::ToString($bytes).Replace('-', '')
Set-Clipboard -Value $env:DevPrism__PairingCode
Set-Location .local/publish
try { dotnet DevPrism.Api.dll } finally { Remove-Item Env:DevPrism__PairingCode -ErrorAction SilentlyContinue }
```

Open **https://localhost:5081**, paste the one-time code in the Local operator form, and clear your clipboard. The grant is valid for 15 minutes and can be used once. The session expires after 30 minutes, when signed out, or when the app stops. To pair again after one of those events, restart with a newly generated code. Do not reuse, log or share the clipboard value.

For frontend development, start the API separately, then run `npm.cmd run dev` from the web directory. The dev UI listens on `127.0.0.1:5173` and proxies readiness only with TLS validation; on Windows set `NODE_USE_SYSTEM_CA=1` for Node to use the trusted development certificate in the Windows certificate store. Pairing and project operations use the packaged HTTPS origin.

No provider credentials are needed to build or run this scaffold. Do not put tokens in the repository or in diagnostic output. A successful build and local startup do not satisfy the remote MCP gates.

## Local database setup

Use Docker Compose or a verified Compose provider backed by Podman. The database is loopback-only on port 55432 and uses an independent persistent volume. From the repo root in PowerShell, generate a local password once and preserve it when restarting the same database:

```powershell
New-Item -ItemType Directory -Force .local | Out-Null
if (-not (Test-Path .local/db-password)) {
    $bytes = New-Object byte[] 24
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    [IO.File]::WriteAllText((Join-Path (Get-Location) '.local/db-password'), [BitConverter]::ToString($bytes).Replace('-', ''))
}
$env:DEVPRISM_DB_PASSWORD = (Get-Content .local/db-password -Raw).Trim()
$env:ConnectionStrings__DevPrism = 'Host=127.0.0.1;Port=55432;Database=devprism;Username=devprism;Password=' + $env:DEVPRISM_DB_PASSWORD
docker compose -p devprism-local -f infra/compose.yaml up -d --wait
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/DevPrism.Infrastructure --startup-project src/DevPrism.Api
```

For Podman on Windows, start the existing machine; run the Compose command with a verified standalone Compose binary targeting the Podman Docker-compatible named pipe, or use an installed Compose provider through Podman. Keep the generated password file local and private. Avoid printing the environment or resolved Compose configuration; both may contain credentials. Migrations are explicit; application startup never recreates the database.

**G1.2 evidence (verified locally):** PostgreSQL 17.9 was healthy under Podman 5.4.1 and Compose 5.8.0. The initial EF migration applied, a second update had no work, the DB rejected an orphan connection, and a synthetic project persisted across a container restart. The published port was loopback only. This is schema evidence, not proof of endpoint authorization or provider access.
