# Phase 1 — Multi-Tenant Security in Cosmos DB: Milestone 1 Lesson Summary

**Status:** Milestone 1 (Partition Strategy) complete. Milestones 2–5 of Phase 1 not yet started.
**Focus:** Partition key design in Cosmos DB and the RU-cost tradeoffs of different query shapes against it.

---

## What Was Done

- Scaffolded **TenantVault** as an ASP.NET Core Web API (controllers, not minimal APIs — deliberate choice, matches work experience) targeting **.NET 10** (a deliberate deviation from the plan's original .NET 8 spec, justified as current LTS; verified `Microsoft.Azure.Cosmos` SDK compatibility before committing).
- No authentication, no Docker container support enabled in the project — both deliberately deferred/declined:
  - Auth is explicitly Milestone 2's job.
  - Container support was caught as a "sounds production-appropriate" reflex click with no actual justification for this milestone (TenantVault isn't being dockerized or deployed yet).
- Domain: a car-storage/vehicle-inventory model (tenant → warehouse → vehicle) instead of the plan's original notes/documents example. Confirmed the domain gap (no customer/owner entity tracked) doesn't block Milestone 1's actual goal and was consciously left out rather than "fixed" mid-milestone.
- **Partition key: hierarchical, `tenantId` → `warehouseId`** (two levels, within Cosmos's 3-level hierarchical key limit). Chosen over a single-level `tenantId` key specifically to avoid the 20GB-per-logical-partition ceiling if any one tenant's data grew large — a synthetic/composite-style mitigation, implemented here as a true hierarchical key rather than a manually concatenated composite string.
- Document `id` set to `vehicle_id` alone (not a composite of tenant+vehicle) — correctly reasoned that `id` only needs to be unique *within* a partition, not database-wide, so no "belt and suspenders" composite was needed.
- Single container, `Vehicle` documents only — deliberately did not add a `Warehouse` docType, since it wasn't needed to demonstrate partition strategy or RU cost and would have been scope creep.
- Endpoint set, settled after several iterations (see below):
  1. `POST vehicle` — create
  2. `GET vehicle/{tenantId}/{warehouseId}/{vehicleId}` — **point read** (`ReadItemAsync`, not a query)
  3. `GET vehicle/{tenantId}/{warehouseId}` — **full-key query** (`SELECT * FROM c`, scoped entirely by `PartitionKey`, no `WHERE` needed since Cosmos was verified to filter correctly on the full key alone)
  4. `GET vehicle?tenantId=X&year=Y` — **partial-key query** (`tenantId` required, `year` optional filter)
  5. `GET admin/vehicle?year=Y` — **true cross-partition query**, no partition key supplied at all; explicitly routed under an `admin` prefix and flagged as needing role-based authorization once auth lands, rather than being built as an open endpoint
- Seeded synthetic data at increasing volume to get a clean RU signal: started at 10 records/tenant (too thin — RU differences were noise), moved to 25,000 rows (100/tenant-warehouse pair, too slow to load overnight), settled on **5,000 rows (50 tenants × 5 warehouses × 20/pair)** as the practical volume that still produced a real signal.
- Logged `RequestCharge` (and, once the anomaly below was investigated, full `response.Diagnostics`) via Serilog for every query type.

---

## Key Decision: Hierarchical Partition Key, and What "Redundant" Actually Means

**Final partition key:** `tenantId` → `warehouseId`, hierarchical (not a manually concatenated synthetic key).

**Reasoning (interview-ready form):** A flat `tenantId`-only partition key risks the 20GB-per-logical-partition ceiling for any single large tenant. A hierarchical key adds a second dimension (`warehouseId`) that increases cardinality without giving up the ability to route efficiently by tenant alone via a **partial key query** — Cosmos-specific terminology for a query that supplies a prefix of a hierarchical key. A partial key query is more efficient than a true cross-partition query (it routes to a bounded subset of partitions rather than fanning out to all of them) but is not as cheap as a full-key query, and — critically — **is not automatically index-filtered the way a full key is.**

---

## Notable Moments / Weak Spots to Revisit

- **The central finding of this milestone: `PartitionKey` alone is not sufficient for a partial-key query.** Initial (incorrect) theory was that setting `PartitionKey` to just `tenantId` in `QueryRequestOptions` made a `WHERE tenantId = @tenantId` clause redundant, the same way it is for a full key. This was "verified" against a 10-record-per-tenant dataset and looked correct — the RU numbers were close enough that the missing `WHERE` clause's cost wasn't visible against the noise. Once the dataset grew to 5,000 rows, the same missing-`WHERE` query jumped to **93.81 RU** versus **5.83 RU** with the clause present, on identical result sets (100 documents, correctly tenant-scoped either way — never a data leak, purely a cost bug).
  - **Root cause, confirmed via `response.Diagnostics`, not guessed:** Query Metrics showed `Retrieved Document Count: 5,001` and `Index Utilization: 2.00%` without the `WHERE` clause, versus `Retrieved Document Count: 100` and `Index Utilization: 100.00%` with it — both against the same single `PartitionKeyRangeId`. First hypothesis (that the partial key was fanning out across *multiple physical partitions*) was checked against the diagnostics and **ruled out** — same partition range in both cases. The actual mechanism: `PartitionKey` in `QueryRequestOptions` only *routes* the request to the right partition range; it does not act as a query predicate. A full key collapses "routing" and "filtering" into the same no-op, which is what made the earlier (wrong) theory look correct in the full-key case. A partial key still requires an explicit `WHERE` predicate for the query engine to actually filter (and use the index) once inside that range — without it, Cosmos reads and discards every document in the range.
  - **Corrected, defend-it-ready rule:** *`PartitionKey` alone is sufficient only for a full (complete) hierarchical key. A partial/prefix key still requires a matching `WHERE` predicate to get indexed filtering — otherwise Cosmos reads every document in the routed partition range and filters nothing.*
  - **Process lesson, arguably more valuable than the Cosmos lesson itself:** a plausible theory, tested against too small a dataset, produced a false confirmation. The bug wasn't caught by re-reasoning about Cosmos internals — it was caught by re-running the same test at higher volume and refusing to accept a suspicious number ("this doesn't seem right") without checking record counts and diagnostics first, rather than jumping straight to changing code.
- **Tenant ID mix-up mid-investigation:** the first "wrong number" (93.6 RU on a query returning only 1 document) was actually caused by querying with an incorrect/nonexistent `tenantId`, unrelated to the real bug above. This was correctly caught by checking record counts first rather than assuming the RU number itself was the anomaly — but it also briefly caused two variables (wrong tenant ID *and* the missing `WHERE` clause) to get changed at once, which would have muddied the real finding if not separated back out one variable at a time.
- **Routing design detour:** an early attempt to give `GetVehiclesByTenantAsync` and `GetVehiclesByYearAsync` separate single-segment routes (`vehicle/{tenantId}` vs `vehicle/{year}`) created a real ambiguity, papered over with a `:int` route constraint — fragile, since a numeric-string tenant ID would silently misroute. Correctly resolved by moving both filters to query-string parameters on a single `GET vehicle` endpoint instead of relying on path-segment type inference. This produced its own follow-up bug (two actions with an identical route template and verb, ASP.NET Core can't disambiguate by query string since routing happens before query-string binding) — resolved by merging into one action with optional parameters, rather than adding yet another distinct path.
- **Scope-creep instincts caught before they were acted on**, each independently talked back out of:
  - Adding a `Warehouse` docType "for completeness" — correctly identified as not serving the milestone's actual goal.
  - Condensing *all* GET endpoints (including the point read and full-key query) into one mega-endpoint for consistency after merging two of them — correctly identified as actively harmful here, since it would obscure the RU-cost story that is the entire point of the milestone.
  - Optimizing the PowerShell seeding script for speed before establishing whether the slow version was actually a blocker — right question to ask, answered by just running it and finding out (it was, in fact, too slow at 25k rows; parallelized version had its own tooling bugs and was abandoned in favor of a smaller, still-adequate 5,000-row dataset).
- **Security posture named precisely, not softened:** confirmed explicitly that tenant isolation is *currently spoofable* (any caller can supply any `tenantId` in the route/query, unauthenticated) and that this is a deliberate, scoped gap closed by Milestone 2 — not glossed over as "fine for now."

---

## Carried Forward Into Milestone 2

- TenantVault API scaffolded, no auth yet — Milestone 2's entire job is closing the tenant-spoofing gap identified above via JWT-derived tenant resolution middleware.
- Cosmos hierarchical partition key (`tenantId` → `warehouseId`) in place and validated at real volume.
- Confirmed, logged RU numbers for all four query shapes (point read, full-key, partial-key, cross-partition) — final corrected partial-key number pending one more clean rerun to confirm the cross-partition (admin) number wasn't affected by the earlier tenant-ID mix-up.
- `admin/vehicle` endpoint exists but has zero enforcement — explicitly flagged in-code as needing role-based authorization, not just authentication, once Milestone 2 lands.
- Weak-spot flagged for a second rep: **test volume affects correctness conclusions, not just magnitude** — a theory "confirmed" against a small dataset should be treated as provisional until re-checked at realistic scale.
