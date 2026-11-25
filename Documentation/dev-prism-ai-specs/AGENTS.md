# DEV PRISM AI — instructions for implementation agents

Read the specification pack before implementing. If this file is in the repository root, the other Markdown files normally live in `docs/spec/`; if they are colocated, resolve relative to this file. Mandatory first reading: `00_START_HERE.md`, `01_PRODUCT_AND_SCOPE.md`, `04_ARCHITECTURE.md`, `12_SECURITY_AND_OPERATIONS.md`, `14_IMPLEMENTATION_ROADMAP.md`, `18_TRACEABILITY_AND_DECISIONS.md`.

Do not start implementation merely because docs exist. When authorized, execute the active roadmap group against the canonical Local MVP scope. Work on later stages only when separately scheduled. No speculative dependencies. Stop affected work on contradictions, unsafe credentials or unverified required provider contracts; continue independent authorized work with truthful status. A local MCP fixture or REST response never qualifies as required remote MCP proof.

## Task contract

**Ready:** specify one observable outcome, scope and exclusions, affected contracts, dependencies, a normal case and an important failure/degraded case. Identify whether verification must be live, local or can use a fixture. For HARD/COMPLEX items include privacy/authorization, recovery and the evidence to record.

**Done:** give the actual result with relevant scope, source route, version/run/commit reference and redacted evidence; note the relevant failure check and limitations. Do not mark an unchecked condition complete. Use `Verified live`, `Verified locally`, `Verified fixture` or `Blocked`. Existing build/type/lint/migration checks may be reused; no new unit, integration or E2E suite is a standard FE/BE deliverable. Manual release verification remains required.

**Consistency by task type:** UI states distinguish empty, unavailable, hidden and stale. Backend HTTP changes declare validation, auth scope, status and OpenAPI surface. Connector tasks declare capability, identity, provenance, visibility, failure and repetition behavior. Persistence tasks state which fields are immutable and which can be changed. No widening of scope, fake status, silent fallback or external write without approval.

Code standards:

- C#: XML documentation (`/// <summary>`, `<param>`, `<returns>`, exceptions where meaningful) for classes, interfaces and functions/methods including private helpers with nontrivial behavior. TypeScript: JSDoc/TSDoc for functions, exported types, classes and interfaces. Document domain invariants and side effects; avoid restating syntax.
- File headers for authored source files link to relevant spec/ADR IDs and briefly explain the file's responsibility. Respect tooling-required first lines and generated file rules. Do not insert comments into JSON or generated files that cannot accept them; document their relationship in the owning module README.
- Explain nontrivial compound conditions and loops in business terms (why, what they protect or affect), not mechanical descriptions. Prefer meaningful names, typed results, cancellation, bounded retries and explicit error states. Never log secrets.
- Keep provider-specific logic in adapters, approvals and authorization in backend services, and state transitions centralized. The React client cannot be a security boundary. Never enable unsafe full-access modes to make tests pass.
- Record verifiable outcomes using the task contract. Preserve existing checks; do not weaken them to get a green build. Do not invent tests, provider results or performance data.
- Use only synthetic dedicated demo assets. No changes to personal or employer repositories or services unless explicitly configured and authorized. No automatic merge to main, publication or deployment without human approval.
- Keep `README` and setup instructions runnable. Update the decision log when deviating from proposed defaults. If the owner uses a different provider, engine or platform, validate it through the adapter contract instead of leaking assumptions into the core.
