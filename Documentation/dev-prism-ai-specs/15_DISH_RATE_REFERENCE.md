# 15 — DishRate: independent reference project and demo fixtures

## Why this project exists

DishRate is a small target application, not part of DEV PRISM AI's core backend. It gives a reproducible non-confidential scenario for ADO/Figma/Wiki/Git linking, real code execution, rule evolution and cross-source drift. It may later be deployed publicly, but MVP preview is local. Keep a separate Git repository and separate PostgreSQL database/credentials. DEV PRISM AI must work on other registered repos without DishRate-specific logic; typed rating policy checker is a demonstrative policy plugin, not hardcoded core behavior.

## Local MVP source preparation

For the first presentation, DishRate means dedicated synthetic artifacts in real authorized online services, not a required runnable target app. Prepare a Boards item with visible editable acceptance criteria and a Figma frame with explicit rating annotations. Have one normal revision and one planned change in the same work item; retain the authorized original links. Optional: an ADO Wiki rule page, a repo commit reference and a rendered Figma preview if those capabilities are proven. No employer assets, remote writes from PRISM, CLI, seed deployment or fixture-only source can satisfy the Local MVP gate. Use the walkthrough in [02_USER_FLOWS_AND_DEMO.md](02_USER_FLOWS_AND_DEMO.md#local-mvp-presentation-810-minute-rehearsal-target). The implementation reference below belongs to later phases.

## Target app scope (post-MVP)

React/TypeScript/Vite + ASP.NET Core Web API + EF Core/PostgreSQL. Minimal page for browsing restaurants, list dishes, detail, submit/edit own rating, leaderboard, owner review export later. Use synthetic seed data and preseeded demo identities through a clearly labeled local-only authentication fixture. No public deployment with fake auth; production auth, file upload moderation, privacy and abuse prevention require separate work. Optional photo is seed asset, no upload MVP. Local preview via approved Docker Compose profile; `GET /health` verifies readiness. Pin toolchain and package locks in seed. Use accessible star/radio control, keyboard support, clear validation, loading/error states.

## Domain model

| Entity | Fields / constraints |
|---|---|
| Restaurant | UUID id, name, city, ownerUserId; synthetic names |
| Dish | UUID id, name, description, imagePath optional |
| MenuItem | UUID id, restaurantId, dishId, active; unique restaurant/dish |
| User | UUID id, displayName, email (synthetic, never returned in owner export), role |
| Rating | UUID id, menuItemId, userId, score, createdAtUtc, updatedAtUtc; unique(menuItemId,userId), FK, range/step DB check |

Rating belongs to a menu item (dish at a restaurant), not a global dish. User can update own rating; no duplicate row. API derives user ID from authenticated fixture/session, not arbitrary request body. Owner can access only own restaurant, no identity fields. Public average visible after at least three ratings per menu item; owner export statistics threshold is separately approved rule if applicable, never assumed from public rule. No user-identifying data in fixture screenshots. Decimal rating/aggregate semantics documented. UTC timestamps. All user-input strings sanitized/encoded. No real restaurant/person data needed.

## Initial accepted decisions

| Key | Scope | Approved rule |
|---|---|---|
| ADR-001 | whole app | React + .NET + PostgreSQL, separate FE/BE, OpenAPI contract frozen before parallel work |
| BR-004 | rating | one rating per authenticated user per menu item; upsert own only |
| BR-005 | public details/leaderboard | average/ranking only when at least 3 ratings |
| BR-006 v1 | rating API/UI/DB | integer score 1–5 inclusive, step 1 |
| BR-007 | timestamps | UTC in backend/storage |
| BR-012 | owner export/dashboard | owners cannot access reviewer email/full name or user IDs; own restaurant only |
| ADR-008 | future export | same privacy rules apply to new partner/export features |

Decisions proposed from initial prompt must be explicitly approved; create Wiki page with versioned stable IDs and link actual ADO work items. Do not prepopulate approved decisions in live run without user review. Fixture mode may seed approved examples clearly labeled. BR-012 and ADR-008 can be approved early even when export not yet implemented, to demonstrate future constraint.

## Initial contract and tests

Create `contracts/rating.openapi.yaml` (or chosen equivalent) in target repo, freeze before branching. Minimal endpoints:

- `GET /api/restaurants?city=DemoCity`: list id/name/city.
- `GET /api/restaurants/{id}/menu`: menu item id, dish title, optional image, `ratingCount`, `averageRating` nullable when count < 3.
- `GET /api/menu-items/{id}`: details with same aggregate and own rating if authenticated (own only).
- `PUT /api/menu-items/{id}/my-rating`: `{score: 4}` initial integer 1–5, returns `{score, updatedAtUtc}`; 400 invalid range/step, 401 unauthenticated, 404 unknown menu item. Unique constraint handles concurrent upsert safely.
- `GET /api/leaderboard?city=DemoCity&limit=10`: eligible menu items only, descending average, rating count then stable ID tie-break; limit max 50.
- Later `GET /api/owner/restaurants/{id}/reviews.csv`: authorize owner, CSV headers only `dishName,score,createdAtUtc` or explicitly approved nonidentifying fields; protect CSV formula injection in text, no reviewer identity/ID. Aggregate threshold only if explicitly applicable.

Seed: three fictional restaurants, four menu items, five fake users using reserved example.test emails. Example ratings per menu item: A [5,4,5] count 3, B [5] count 1, C [3,4,4,5] count 4, D none. Public average A = 14/3, C = 4.0, B/D hidden. Display rounding defined (one decimal, agreed midpoint rule) but sort by full aggregate, not display string. Tests: range 0/6 fails; fractional 4.5 fails v1; valid update keeps row count; anonymous denied; another user's rating not exposed; aggregates hidden below 3; ranking excludes low count; owner restricted to own restaurant and no identifier; API schema type v1 integer. Separate FE control and BE validation tests, integration DB tests, end-to-end browser smoke.

## Half-star change mission

Design update: rating component Figma frame changed to explicit annotation `Allowed values: 1.0, 1.5, ..., 5.0` or user-confirmed structured interpretation. ADO acceptance criteria update `accept 0.5 increments`. Existing Wiki BR-006 v1 and API integer; backend C# `int Score`, PostgreSQL integer, FE whole-star selection. Expected finding links all. Approve BR-006 v2 target, policy `{min:1,max:5,step:0.5,representation:decimal}`. OpenAPI `type:number`, `minimum:1`, `maximum:5`, `multipleOf:0.5`. C# decimal, PostgreSQL `numeric(2,1)` with range and `mod(score*2,1)=0` or equivalent check. Migration preserves existing integer values and records reversible limitations (downgrade would lose fractions; do not silently round). FE half-step keyboard accessible selection. Validation rejects 1.25, 0.5, 5.5; accepts 1, 1.5, 4.5, 5. Tests for migration with old rows, API/UI/DB consistency, average/ranking. Freeze new contract before parallel branches; actual integration tests from approved profile. Wiki updates after verified merge and explicit approval, then policy version promoted. If post-merge docs fail, show partial and pending reconciliation. Figma already updated; do not edit again unless separate requested action.

## Suggested target repo

```
DishRate/
  contracts/rating.openapi.yaml
  src/Api/
  src/Domain/
  src/Infrastructure/
  web/src/ratings/
  web/src/restaurants/
  tests/Acceptance/
  tests/Backend/
  infra/compose.yaml
  AGENTS.md
```

Paths are suggestions; register actual ownership after scaffold. Initial seed may contain empty shell, fixtures and contract; no need to create every app from empty folder live. For from-scratch demo, backlog creation is real; scaffold stage may be prepared to avoid restore/network delays. Show actual changes in feature mission. Separate seed setup from product code. Use known scripts with safe runner profiles, e.g. restore/build/test .NET, `npm ci`, `npm run build`, `npm test`, approved compose start. Validate actual commands in target README.

## Source fixture map (illustrative IDs only)

Feature `dish-rating` links ADO work item `#142` (fixture), Figma frame `142:18` (fixture), Wiki `/Business Rules/Ratings`, Git `src/Domain/Rating.cs`, `web/src/ratings/RatingControl.tsx`, `contracts/rating.openapi.yaml`, `tests/Acceptance/RatingTests.cs`, BR-004/005/006/007 and PR. Feature `owner-export` links future ADO `#214`, ADR-008 and BR-012, owner design/table, export contract. Actual provider IDs discovered after creating/reading. Seed fixture snapshot v1 and v2 for rating frame and work item; deterministic diff uses explicit structured annotation. Live mode must never return fixture IDs as real provider IDs. Use clearly labeled sample data and local fixture switch; never silently fallback from live to fixtures.

## Demo runbook commands/prompts

1. Provision dedicated ADO org/project/repo/Wiki, Figma file and authorized AI provider; create local DishRate seed clone and synthetic DB. Register DEV PRISM AI project and run probes/sync. Open original links to prove data source.
2. Prompt initial backlog as in F03. Review rule assumptions, approve exact writes. Show ADO work item IDs + Wiki revision and reread. Link rating frame and code refs through explicit links/graph review.
3. Prompt integer rating implementation; approve plan and contract. Show FE/BE actual concurrent runs if G8 ready, otherwise one live run. Show integration diff/checks and preview. Approve draft PR, optional merge subject to policy. If merge not allowed, use dedicated integration branch as next demo baseline with explicit label and no ADO Done state.
4. Change Figma annotation/ADO criteria, trigger sync. Show Activity, Feature Explorer, Watchtower conflict, impact and proof. Ask half-star feature, review multi-system plan and target decision. Implement and verify; show 4.5 in actual preview, test evidence, ADO/Wiki receipts after authorized completion.
5. Restart DEV PRISM AI and new thread. Request owner export with email, show BR-012 conflict and safe alternative. Optionally implement privacy-safe export and run known contract/CSV test.
6. Show token/elapsed measurements if known, otherwise unknown. Open graph and original artifact revisions. Explain limitations. Stop preview and agent processes, revoke disposable credentials when done. Reset demo by fresh dedicated repo/project or safe local restore; no bulk remote delete.
