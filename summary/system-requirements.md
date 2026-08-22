# System Requirements

**Purpose:** Everything that must be installed and configured on a machine to build and run this system. Updated at the end of each phase as new dependencies are introduced. This is a "can I get a clean machine running" reference, not a design doc.

**Last updated:** After Phase 0

---

## Operating System / Base Platform

- **Windows** with **WSL2** enabled.
  - Install via `wsl --install` (elevated PowerShell) if not already present.
  - Keep updated with `wsl --update`.
- **Visual Studio 2022+** (with ASP.NET / .NET workload).
- **.NET 8 SDK.**

---

## WSL2 Configuration

Config file: `%UserProfile%\.wslconfig` (create if it doesn't exist — global across all WSL2 distros).

```ini
[wsl2]
memory=6GB
processors=4
swap=4GB
```

> Values above are sized for a 16GB RAM / 8-core (16-thread) machine. Adjust proportionally for other hardware — see "Sizing notes" below.

**Sizing notes:**
- Memory: roughly a third to half of total system RAM, leaving headroom for Visual Studio + Cosmos DB Emulator (both run natively on Windows, outside WSL2).
- Processors: WSL2 reports *logical* processors (hyperthreads), not physical cores — check `wsl --status` or Task Manager to confirm what your CPU actually has before assuming the default is correct.
- Swap: default is 25% of assigned memory; increase if memory-constrained overall.

**Applying changes:** edit `.wslconfig`, then run `wsl --shutdown` and restart WSL — changes don't apply live.

**Idle resource management:**
- Run `wsl --shutdown` to fully terminate the WSL2 VM (and Docker Desktop's backend with it) when not actively working.
- Docker Desktop → Settings → General → disable "Start Docker Desktop when you sign in to your computer" if you don't want the VM warm all the time.

---

## Docker

- **Docker Desktop for Windows**, configured to use the **WSL2 based engine** (Settings → General → "Use the WSL 2 based engine").

### Containers required

| Container | Image | Ports | Restart Policy | Purpose |
|---|---|---|---|---|
| `rabbitmq` | `rabbitmq:3-management` | `5672` (AMQP), `15672` (management UI) | `unless-stopped` | Message broker (Phase 3+) |

**Setup command:**
```
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 --restart=unless-stopped rabbitmq:3-management
```

**Verify:**
```
docker ps
docker inspect rabbitmq --format='{{.HostConfig.RestartPolicy.Name}}'
```
Expected: container listed as `Up`, restart policy prints `unless-stopped`.

**Management UI:** `http://localhost:15672` — default credentials `guest`/`guest` (restricted to loopback/localhost connections by RabbitMQ's own design; not usable remotely with these credentials).

> ⚠️ Known issue: containers created with plain `docker run` default to `--restart=no` and will **not** restart automatically after a Docker Desktop or machine restart unless the restart policy is explicitly set, as above.

*(Containers for SQL Server and Redis will be added here once Phase 5 setup happens.)*

---

## Local Data Stores

- **Azure Cosmos DB Emulator** (Windows) — provides a local Cosmos account and connection string; no Azure subscription required.

*(SQL Server / LocalDB — needed starting Phase 5, not yet installed.)*

---

## Messaging

- **Broker:** RabbitMQ (see Docker section above for container setup).
- **Abstraction layer:** MassTransit — will be added as a NuGet package once a project exists that needs it (starting Phase 1/Phase 3 work). `MassTransit.RabbitMQ` is the specific transport package for this broker choice.

---

## NuGet Packages

None installed yet — no projects created as of end of Phase 0. Packages are added per-project as they're needed; this section will be filled in starting with Phase 1's TenantVault project.

Anticipated for early phases (per the learning plan): `Microsoft.Azure.Cosmos`, `MassTransit`, `MassTransit.RabbitMQ`, `Polly`, `Microsoft.Extensions.Http.Resilience`, `Serilog`.

---

## Verification Checklist (fresh machine)

Use this to confirm a machine is ready to pick up where this project currently stands:

- [ ] `wsl --status` shows WSL2 as default version
- [ ] `%UserProfile%\.wslconfig` present and matches the config above (adjusted for hardware)
- [ ] Docker Desktop running, WSL2 engine confirmed in settings
- [ ] `docker ps` shows `rabbitmq` container `Up`
- [ ] `http://localhost:15672` reachable, login succeeds with `guest`/`guest`
- [ ] Cosmos DB Emulator installed and able to start
- [ ] Visual Studio 2022+ with .NET 8 SDK installed
