# Phase 0 — Environment Setup: Lesson Summary

**Status:** Complete
**Focus:** Local dev environment for Cosmos DB, Docker/WSL2, and messaging broker selection.

---

## What Was Done

- Installed **Azure Cosmos DB Emulator** (prior experience from work machine — no issues).
- Installed **Docker Desktop** with the **WSL2 backend**.
  - Installed WSL2 itself (`wsl --install`).
  - Configured resource limits via `%UserProfile%\.wslconfig`:
    ```ini
    [wsl2]
    memory=6GB
    processors=4
    swap=4GB
    ```
  - Sized for a 16GB RAM / 8-core (16-thread) machine, leaving headroom for Visual Studio and the Cosmos DB Emulator running natively on Windows.
  - Confirmed WSL2 reports *logical* processors (16 threads) rather than physical cores (8) — worth remembering when reading WSL/Docker CPU settings.
- Established the idle-resource-management habit: `wsl --shutdown` to reclaim RAM between sessions, Docker Desktop set to not launch on Windows startup.
- **Chose a messaging broker: RabbitMQ**, over Azure Service Bus Emulator and Kafka.
- Pulled and ran RabbitMQ via Docker:
  ```
  docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
  ```
- Diagnosed that the container did not survive a Docker Desktop restart, traced it to the default `--restart=no` policy, and corrected it:
  ```
  docker update --restart=unless-stopped rabbitmq
  ```
- Confirmed the fix via `docker inspect rabbitmq --format='{{.HostConfig.RestartPolicy.Name}}'` and a real Docker Desktop restart test.
- Logged into the RabbitMQ management UI at `localhost:15672` (`guest`/`guest`) and confirmed it's reachable.
- Deliberately deferred to later phases (not oversights):
  - NuGet package installation — will happen per-project as each project is created, starting in Phase 1.
  - Phase 5 tooling (SQL Server, Redis, BenchmarkDotNet, OpenTelemetry) — explicitly out of scope until Phase 5.

---

## Key Decision: RabbitMQ vs. Azure Service Bus vs. Kafka

**Initial instinct:** Azure Service Bus, for Azure-shop resume relevance — then reconsidered in favor of "widest applicability."

**Kafka was seriously considered and ruled out.** Initial assumption was that Kafka is "a message broker similar to Azure Service Bus." That assumption was wrong in a way worth remembering:

| | RabbitMQ / Azure Service Bus | Kafka |
|---|---|---|
| Model | Queue — messages removed on ack | Append-only log — messages retained, consumers track their own offset |
| Consumer pattern | Competing consumers (one message → one consumer) | Consumer groups (each group gets the full stream; partitions divided within a group) |
| Ordering | Not guaranteed by default; must be engineered (e.g., version checks) or use session/FIFO features | Guaranteed per-partition; partition by key (e.g., document ID) for order |
| Dead-lettering | Native, first-class (DLQ after retry exhaustion) | Not native — must be built manually (e.g., a separate topic) |
| Typical pairing | MassTransit's primary, most idiomatic transport | MassTransit support exists but is less idiomatic; often paired with CDC (Debezium) patterns instead |

**Final decision: RabbitMQ**, with MassTransit as the transport abstraction layer.

**Reasoning (interview-ready form):** Queue-based broker semantics — competing consumers and native dead-letter queues — match both what Blackbaud uses in production and what Phase 3's milestones are actually written to teach (idempotent consumers, ordering via version checks, observable dead-lettering). Kafka's log/offset model would have meant solving a different set of problems (partition-based ordering, no native DLQ) than the ones this plan is targeting, even though it's a legitimate and valuable model to know in its own right.

---

## Notable Moments / Weak Spots to Revisit

- **Kafka misconception:** initially described Kafka as "similar to Azure Service Bus." The queue-vs-log distinction (and its downstream effects on ordering, consumer semantics, and dead-lettering) is flagged for a second rep before the capstone — Phase 3 and Phase 4 both depend on being able to state broker guarantees precisely.
- **`on-failure` restart policy:** first description of it ("restarts when the whole thing goes down") was imprecise. Correct definition: restarts only on a non-zero (error) exit code; does nothing on a clean exit or manual stop. Correct policy (`unless-stopped`) was still chosen correctly despite the imprecise reasoning.
- No hint above **rung 1** (clarifying question) was needed anywhere in this phase. The RabbitMQ restart-policy bug was self-diagnosed down to the exact flag by inspecting Docker Desktop's generated run command.

---

## Addendum: Image Version Discipline (post-completion follow-up)

After Phase 0 was marked complete, a follow-up question ("is `redis:7` actually current?") surfaced a real gap worth logging:

- **RabbitMQ was running on a stale major version.** The container had been set up with `rabbitmq:3-management`. Checked against Docker Hub's official page directly (not a search snippet) and found the current supported line is `4.3.5`/`4`/`latest`. Corrected to `rabbitmq:4-management`. Also added `--hostname` to the run command, which had been missing — RabbitMQ keys its data directory to the container hostname, and Docker's own docs recommend setting it explicitly to avoid orphaning data on container recreation.
- **Redis guidance given earlier (`redis:7`) was also out of date.** Current major version is `8` (license moved from RSAL back to open-source AGPLv3 as of Redis 8, worth knowing as a side fact). Corrected recommendation to `redis:8` — pinned major version, not `latest`, for the same reproducibility reasoning applied to RabbitMQ.
- **Independently, before being asked, a SQL Server image was pulled using `mcr.microsoft.com/mssql/server:2025-latest`** — checked against Microsoft's own docs and confirmed as a real, current, officially supported tag (SQL Server 2025 / 17.x). This was the right instinct applied without prompting: check the actual current version rather than trust a remembered or copy-pasted one.

**Takeaway logged for the weak-spot list:** version tags given in the moment (by either party) should be treated as provisional, not authoritative, until checked against the source (Docker Hub / vendor docs) — training data and casual recall both go stale on version numbers specifically, even when the surrounding concept is otherwise correct. This is a good general habit to keep applying across every phase, not just infrastructure setup: check current versions of NuGet packages, SDKs, and tooling before locking them in, rather than assuming a remembered version number is current.

**Redis and SQL Server containers pulled as tests only** — not yet wired into any project, not yet part of "Phase 0 complete" scope. Formal setup (restart policy, data volumes, wiring into code) deferred to Phase 5 as originally planned. Captured in `system-requirements.md` as test pulls so a fresh-machine setup has a record of what's been validated versus what's still pending.

---

## Carried Forward Into Phase 1

- RabbitMQ running locally, restart-safe, management UI confirmed.
- MassTransit confirmed as the transport abstraction to use once a project exists.
- Cosmos DB Emulator ready and already familiar.
- No NuGet packages installed yet — first package installs happen at the start of Phase 1 alongside TenantVault project creation.