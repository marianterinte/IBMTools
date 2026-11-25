# DEV PRISM AI — instructions for implementation agents

Read the specification pack before implementing. If this file is in the repository root, the other Markdown files normally live in `docs/spec/`; if they are colocated, resolve relative to this file. Mandatory first reading: `00_START_HERE.md`, `01_PRODUCT_AND_SCOPE.md`, `04_ARCHITECTURE.md`, `12_SECURITY_AND_OPERATIONS.md`, `14_IMPLEMENTATION_ROADMAP.md`, `18_TRACEABILITY_AND_DECISIONS.md`.

Execute the roadmap sequentially. Work on only the active group unless explicitly directed. No speculative refactoring or dependencies outside the active scope. Read the group's mandatory specs before implementation and after context compaction. For HARD/COMPLEX work, first write an explicit breakdown and proof plan. Keep tasks in the roadmap marked accurately with evidence. Stop affected work on contradictions, unsafe credentials, missing sandbox, or unverified provider capability; continue independent safe work where possible. Never substitute a mock for a promised live integration without labeling it.

Code standards:

- C#: XML documentation (`/// <summary>`, `<param>`, `<returns>`, exceptions where meaningful) for classes, interfaces and functions/methods including private helpers with nontrivial behavior. TypeScript: JSDoc/TSDoc for functions, exported types, classes and interfaces. Document domain invariants and side effects; avoid restating syntax.
- File headers for authored source files link to relevant spec/ADR IDs and briefly explain the file's responsibility. Respect tooling-required first lines and generated file rules. Do not insert comments into JSON or generated files that cannot accept them; document their relationship in the owning module README.
- Explain nontrivial compound conditions and loops in business terms (why, what they protect or affect), not mechanical descriptions. Prefer meaningful names, typed results, cancellation, bounded retries and explicit error states. Never log secrets.
- Keep provider-specific logic in adapters, approvals and authorization in backend services, and state transitions centralized. The React client cannot be a security boundary. Never enable unsafe full-access modes to make tests pass.
- Add unit, integration and negative tests with each change. Use reproducible commands. Record exact test result and whether live or fixture. No removal/weakening of tests to get a green build. No invented performance data.
- Use only synthetic dedicated demo assets. No changes to personal or employer repositories or services unless explicitly configured and authorized. No automatic merge to main, publication or deployment without human approval.
- Keep `README` and setup instructions runnable. Update the decision log when deviating from proposed defaults. If the owner uses a different provider, engine or platform, validate it through the adapter contract instead of leaking assumptions into the core.
