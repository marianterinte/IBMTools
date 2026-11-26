# AI chat → feature implementation → Azure DevOps: design discussion

**Status:** decision agenda, not implementation authorization or release evidence.

## Is it already in the specs?

Yes. [Product requirements R10–R14](dev-prism-ai-specs/01_PRODUCT_AND_SCOPE.md#functional-requirements) cover project-scoped chat, reviewable plans, creating and verifying real Azure DevOps Boards items, isolated feature implementation, and creating a draft PR linked to the work item. The [half-star demo scenario](dev-prism-ai-specs/02_USER_FLOWS_AND_DEMO.md) combines tasks, code, checks and preview in one request. [Chat and Actions](dev-prism-ai-specs/09_CHAT_AND_ACTIONS.md#approval-model) and [Agent Orchestration](dev-prism-ai-specs/10_AGENT_ORCHESTRATION.md) specify separate approvals for external writes, publishing the actual diff, and merge. The [roadmap](dev-prism-ai-specs/14_IMPLEMENTATION_ROADMAP.md) splits this into **G5** chat and plans, **G6** approved external actions, and **G7** isolated implementation, preview and draft PR. These are P0-B, not part of the read-only Local MVP.

## Proposed user experience

For a request such as “Allow half-star ratings, implement it, and put it in Azure DevOps,” the chat should first show a proposed feature, acceptance criteria and what it understood from the selected project. It should make two distinct Azure DevOps outcomes visible: **create or update a Boards work item** and **publish a branch and open a linked draft PR in Repos**. Neither is implied by the other.

Suggested sequence: review the plan → approve the exact Boards change → implement in an isolated workspace if explicitly requested → verify the diff and run independent checks from the exact commit → review a preview → separately approve publishing a branch and creating a linked draft PR. If the Boards write succeeded but implementation or publishing failed, retain the real ticket ID and show the partial outcome. No automatic merge. A short network failure or unknown write result does not justify blindly retrying.

## Questions to settle before building

| Decision | Question | Suggested starting point |
|---|---|---|
| Model and data | Which model/provider is approved for the first demo, and which source excerpts may leave the workstation? | One approved provider (OpenAI or approved IBM endpoint), synthetic data, bounded project context and a visible record of which sources were sent. |
| Chat scope | Should the chat use the selected project/feature only, or should users explicitly select sources on each request? | Default to selected project/feature; show attached evidence and ask before broadening scope. Keep read/explain distinct from proposing a change. |
| Boards outcome | When the user says “put it in ADO,” should it create a new Feature/User Story, reuse an existing item, or also create tasks? | Start with one approved create/update action. Discover the actual work item types in the target project, preview the exact fields and verify the result by reading it back. |
| Implementation target | Which disposable repo, base branch, and independent build/test commands define the first successful implementation? | One authorized synthetic repo and one isolated worker; show the commit, diff and test result before anything is pushed. |
| Approval and PR | Does the first demo end with a local diff and preview, or with an approved draft PR linked to the Boards item? | End with a linked draft PR if authorized. Require separate approval for publishing the final diff. Keep merge out of the first demo. |
| Failure and budget | What should happen if checks fail, an external action is ambiguous, or a run exceeds its time or cost cap? | Stop dependent steps, keep completed receipts and local work, show the exact blocker, and require reconciliation or a new approval before continuing. |

## First slice to validate

Start by having a project-scoped chat return a typed, reviewable proposal for one synthetic feature: Boards intent, acceptance criteria, source references and any required build checks. No provider write or code execution in that slice. Add an independently authorized Boards write after plan review, then isolated implementation and an optional draft PR as separate acceptance gates. Read-only MCP credentials used by the current preview must not acquire write capabilities by implication.
