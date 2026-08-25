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

---

## Addendum — Separate Session Covering Adjacent Ground

**Note:** this section captures a different working session than the one summarized above. It used a slightly different domain shape (`Vehicle` with `WarehouseId` + `SpotId`, hierarchical key `tenantId`→`vehicleId` discussed at one point) and a smaller seed (482 rows / 50 tenants) rather than the 5,000-row set described above. Treat the two as parallel tracks to reconcile, not a continuation — the specific route/model details don't line up 1:1 with the rest of this document.

### Config & DI plumbing (mechanical, not core lesson content, but reusable)
- `CosmosOptions` bound via `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` — fail-fast at startup instead of on first request.
- Reinforced the **control-plane vs. data-plane** distinction from earlier discussion: `CreateDatabaseIfNotExistsAsync`/`CreateContainerIfNotExistsAsync` don't belong in production runtime code (infra-as-code's job); when needed for local/emulator convenience, they belong in an `IHostedService.StartAsync` gated by `IHostEnvironment.IsDevelopment()` — not the constructor (can't be async) and not scattered into the data adapter.
- `CosmosClient` registered as a DI singleton, injected into a singleton data adapter — confirmed as the documented-correct lifetime (client manages its own connection pooling; constructing per-request is a real perf mistake).

### Serialization boundary bug — reached hint-ladder rung 6 (flag for re-derivation before capstone)
- Root cause: ASP.NET Core defaults to **System.Text.Json** for HTTP binding; the Cosmos SDK defaults to a **Newtonsoft-based** serializer internally unless explicitly configured. `[JsonProperty]` (Newtonsoft) is silently ignored by STJ; conversely, `[JsonPropertyName]` (STJ) has no effect on what Cosmos actually writes unless the client is told to use STJ too.
- Symptom chain: a `WarehouseId` bound as `0` from the request body (wrong attribute for the active serializer) → fixed by switching to `[JsonPropertyName]` → then a Cosmos "id property missing" error even though `Id` was clearly set in C#, because the *Cosmos-bound* serializer wasn't STJ and wasn't honoring the attribute at all.
- Fix: standardize on STJ across both boundaries — `[JsonPropertyName]` on the model, plus `CosmosClientOptions.UseSystemTextJsonSerializerWithOptions` so the Cosmos SDK uses the same serializer/config as ASP.NET Core, instead of maintaining two naming policies on one class.
- **This was a rung-6 (full-answer) hint** — asked for directly after several rungs of self-diagnosis under time pressure. Per the hint-ladder protocol, flagged here for a second rep: be able to explain, from scratch and without notes, *why* Cosmos's default serializer isn't automatically STJ even though ASP.NET Core's is, before moving past Phase 1 for good.
- Downstream naming-convention finding: Cosmos convention is camelCase (`tenantId`, `vehicleId`), not snake_case/PascalCase — and partition key paths are matched against the actual *serialized* JSON property names, not the C# property names or whichever attribute happens not to be in effect. A partition-key mismatch can stack multiple independent causes (wrong concept entirely, casing mismatch, wrong serializer in effect) — diagnosing it means checking all three, not stopping at the first plausible one.

### Not-found handling — resolved cleanly, no hint above rung 2
- Settled pattern: catch `CosmosException` filtered specifically on `ex.StatusCode == HttpStatusCode.NotFound` inside the data adapter, converting to `null`; return type is `Task<Vehicle?>` so the compiler forces every caller to handle the null case, rather than a bare `Task<Vehicle>` with a null-return convention nobody enforces. Controller does the actual `null` → `404` translation.
- Explicitly reasoned through *why* the status-code filter matters: an unfiltered `catch (CosmosException)` would silently convert throttling (429) or auth failures into "not found" too, masking real problems as missing data.

### Seeding tooling — environment friction, not a design lesson
- Reasoned explicitly about seed-data distribution *before* generating it: realized document-count-per-tenant doesn't affect the single-vs-cross-partition RU *contrast* (it scales both proportionally), while tenant count does (it's the dimension cross-partition fan-out actually pays for) — landed on 50 tenants × ~10 docs (uneven, 6–14 range) over 500 total, deliberately reallocated from an initial 2000-tenant/2-4-doc-each plan after working through the cost model. This derivation needed no hints above rung 2 — self-driven.
- Postman Collection Runner turned out to require a paid plan/have a run-count cap on the free tier — discovered only after building the CSV. Pivoted to a PowerShell script (`Invoke-RestMethod` looped over `Import-Csv`) as a free, no-install alternative on Windows.
- Environment gotcha, unrelated to Cosmos or system design: Windows **Smart App Control** (a Windows 11 feature distinct from WDAC/AppLocker) had silently switched on and was blocking the freshly-built, unsigned `TenantVault.exe` with a generic "application control policy has blocked this file" error — no code or config change caused it. Turning it off restored the ability to run the app. Worth remembering this exists the next time a personal Windows machine blocks a locally-built binary with no apparent cause.
- Separately, `.\seed-vehicles.ps1 ...` initially just opened the script in Notepad instead of running it — a shell/file-association issue, not a script bug. Fixed by invoking explicitly: `powershell.exe -ExecutionPolicy Bypass -File .\seed-vehicles.ps1 ...`.

### Open item — idempotent "move" / swap operation, not resolved, needs a revisit
- Explored whether `(warehouseId, spotId)` makes a good deterministic document id, reasoning from a real domain invariant (one physical spot can only hold one vehicle at a time). Correctly identified this as stronger than an arbitrary hash.
- Worked through moving/swapping vehicles between spots and self-diagnosed two real problems: (1) `id` and partition key are immutable in Cosmos, so a "move" is necessarily a delete+create, not an update; (2) a naive delete+create sequence for a two-vehicle swap has no ordering that's fully safe — either a mid-crash produces data loss (both deleted, neither recreated) or a bad ordering produces a transient double-occupancy/collision.
- Self-connected this to the Phase 4 saga/compensating-transaction pattern, but conflated "compensation" with "rollback" — correctly redirected toward the distinction (compensation = new forward-moving corrective writes using pre-captured data, not reconstructing/undoing history) but the conversation was deliberately parked here rather than resolved.
- **This is a flagged, unresolved thread** — per the instructor-mode rules, this should get a real second pass (ideally once Phase 4 concepts are fresher) rather than being left as a half-finished tangent. Specific unanswered questions to return to: does this operation actually need full saga machinery given everything lives in one Cosmos account (vs. Phase 4's multi-service/multi-database premise), or is there a lighter-weight Cosmos-native mechanism (e.g., transactional batch within a partition) that gives the same atomicity without inventing compensating events for what might just be a single-partition problem?
