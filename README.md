# DEV PRISM AI

A local, feature-oriented workspace for Azure DevOps and Figma. The application and PostgreSQL run on the operator's laptop; the required provider reads use real remote MCP. See [Local MVP scope](Documentation/dev-prism-ai-specs/00_START_HERE.md) and [provider readiness](Documentation/local-mvp-g0-evidence.md).

## Current increment

The scaffold runs over loopback HTTPS and serves a packaged React UI. Its readiness check reports `503` until local setup and persistent dependencies are implemented. It does not authenticate operators, store project content or contact remote providers yet.

**G1.1 evidence (verified locally, Windows x64):** Release build, web lint/type/build and packaged publish succeeded with .NET SDK 10.0.404 and Node 24.22.3. The packaged HTTPS SPA returned 200, readiness returned 503 with scaffold status, and unknown API/media paths returned 404 instead of the SPA. This does not close the G1 gate.

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
Set-Location .local/publish
dotnet DevPrism.Api.dll
```

Open **https://localhost:5081**. For frontend development, start the API separately, then run `npm.cmd run dev` from the web directory. The dev UI listens on `127.0.0.1:5173` and proxies the API with TLS validation; on Windows set `NODE_USE_SYSTEM_CA=1` for Node to use the trusted development certificate in the Windows certificate store. The packaged HTTPS origin is the release reference.

No provider credentials are needed to build or run this scaffold. Do not put tokens in the repository or in diagnostic output. A successful build and local startup do not satisfy the remote MCP gates.
