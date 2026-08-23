# Lessons Learned — Configuring & Using Cosmos DB (.NET)

Phase 1 milestone: multi-tenant partition strategy, hierarchical keys.

## 1. Options Binding for Cosmos Config

- Define a `CosmosOptions` POCO (endpoint, key, database name, container name, partition key path(s), throughput, autoscale flag) and bind it with `builder.Services.AddOptions<CosmosOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`.
- `ValidateOnStart()` matters: it fails fast at app startup on bad/missing config instead of failing on the first request that touches Cosmos.
- Account keys should not live in committed `appsettings.json`. The emulator key is a fixed, public value, so it's fine for local dev; anything beyond that should use Managed Identity / Entra ID or Key Vault.

## 2. Hierarchical Partition Keys

- Config shape changes from a single `PartitionKeyPath` string to an **ordered list** (`PartitionKeyPaths`), e.g. `["/tenantId", "/warehouseId"]`. Order is meaningful — it must match how you build keys in code.
- Container creation requires `PartitionKind.MultiHash` and `PartitionKeyDefinitionVersion.V2`.
- Building a key at write/read time uses `PartitionKeyBuilder().Add(tenantId).Add(vehicleId).Build()` — the `.Add()` order must match the container's declared path order exactly.

## 3. Throughput & Container Provisioning Belong in Infrastructure, Not App Code

- Provisioning throughput and creating databases/containers from application startup code is common in tutorials but is generally an **infrastructure-as-code concern** in production (Bicep/ARM/Terraform), not something the running app does.
- Reasons: (1) throughput is a direct cost lever — app deploys shouldn't be able to silently change spend; (2) it requires control-plane permissions, which is a larger blast radius than the data-plane (read/write items) permissions the app actually needs.
- Practical pattern: gate `CreateDatabaseIfNotExistsAsync` / `CreateContainerIfNotExistsAsync` behind an environment check (e.g., `IHostEnvironment.IsDevelopment()`) so it only runs against the emulator, using an `IHostedService.StartAsync` (async-safe, runs once before the app serves requests) rather than trying to call async setup from a constructor.

## 4. CosmosClient as a Singleton

- `CosmosClient` is explicitly designed to be created once and reused — it manages its own connection pooling. Register it as a DI singleton, constructed from `CosmosOptions`, and inject it into a data adapter (also typically a singleton) via its constructor.

## 5. Serialization Has Two Independent Boundaries

The biggest source of bugs this session came from forgetting there are **two separate serialization boundaries** that don't automatically share configuration:

1. **ASP.NET Core request/response binding** — uses `System.Text.Json` (STJ) by default since .NET Core 3.0.
2. **Cosmos SDK's internal item serialization** — defaults to a Newtonsoft-based serializer unless explicitly configured otherwise.

Key mistakes and fixes:

- `[JsonProperty(...)]` is a **Newtonsoft** attribute; STJ silently ignores it. If ASP.NET Core is using STJ (the default) and your model only has `[JsonProperty]`, incoming fields bind to their default value (e.g., `0` for an `int`) instead of erroring — a silent failure, not a loud one.
- The fix, if standardizing on STJ: use `[JsonPropertyName("...")]` instead, and (if needed) `[JsonRequired]` from `System.Text.Json.Serialization` — note this only catches a **missing** property, not a present-but-invalid one like `0` or `null`; that needs separate validation (data annotations, FluentValidation, manual checks).
- Even after fixing the HTTP boundary, the Cosmos SDK can still be serializing with its own default (Newtonsoft-based) serializer internally — meaning `[JsonPropertyName]` has no effect on what actually gets written to Cosmos unless you explicitly tell the SDK to use STJ:

  ```csharp
  var clientOptions = new CosmosClientOptions
  {
      UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
      {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      }
  };
  ```

- Mixing Newtonsoft attributes on the model with an unconfigured Cosmos client (defaulting to Newtonsoft) while ASP.NET Core uses STJ creates a situation where HTTP binding works but the object sent to Cosmos doesn't look the way you expect (e.g., `id` missing entirely from Cosmos's point of view even though it's set in C#).
- **Standardizing on one serializer (STJ) across both boundaries** — model attributes, ASP.NET Core config, and `CosmosClientOptions` — avoids having to keep two naming policies in sync on the same class.

## 6. Naming Convention Consistency

- Cosmos DB convention (matching JSON/JS ecosystem norms) is **camelCase** property names (`tenantId`, `warehouseId`), not snake_case or PascalCase.
- Partition key paths (`PartitionKeyPaths` in config) are matched against the **actual serialized JSON property names** on the wire — not the C# property names, and not whatever attribute you happen to have on the class if it isn't the one actually in effect for that serialization boundary.
- A partition key mismatch error can have multiple independent causes stacked together: (1) the field conceptually not existing on the object at all, (2) inconsistent casing between the container's declared path and the serialized property, or (3) the wrong serializer being in effect so the intended attribute is never honored in the first place. Diagnosing requires checking all three, not just the first thing you notice.
- Once one serializer (STJ) governs both the HTTP boundary and the Cosmos boundary, consistency becomes automatic: a client (e.g., Postman) only needs to match the `[JsonPropertyName]` values used for HTTP binding, since the same serialized shape then flows through to Cosmos unchanged.
