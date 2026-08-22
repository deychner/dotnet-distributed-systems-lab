# System Requirements

**Purpose:** Everything that must be installed and configured on a machine to build and run this system. Updated at the end of each phase as new dependencies are introduced. This is a "can I get a clean machine running" reference, not a design doc.

**Last updated:** After Phase 0 (image version corrections + Redis/SQL Server test pulls)

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
| `rabbitmq` | `rabbitmq:4-management` | `5672` (AMQP), `15672` (management UI) | `unless-stopped` | Message broker (Phase 3+) |
| `redis` | `redis:8` | `6379` | `unless-stopped` | Distributed cache (Phase 5) — image pulled as a test, not yet wired into code |
| `sql-server` | `mcr.microsoft.com/mssql/server:2025-latest` | `1433` | *(not yet set — currently default `no`)* | Reporting service datastore (Phase 5) — image pulled as a test, not yet wired into code |

**RabbitMQ setup command:**
```
docker run -d --hostname my-rabbit --name rabbitmq -p 5672:5672 -p 15672:15672 --restart=unless-stopped rabbitmq:4-management
```

**Verify:**
```
docker ps
docker inspect rabbitmq --format='{{.HostConfig.RestartPolicy.Name}}'
```
Expected: container listed as `Up`, restart policy prints `unless-stopped`.

**Management UI:** `http://localhost:15672` — default credentials `guest`/`guest` (restricted to loopback/localhost connections by RabbitMQ's own design; not usable remotely with these credentials).

> ⚠️ Known issue: containers created with plain `docker run` default to `--restart=no` and will **not** restart automatically after a Docker Desktop or machine restart unless the restart policy is explicitly set, as above. This bit us once already on the RabbitMQ container (see Phase 0 lesson summary) — apply `--restart=unless-stopped` (or the appropriate policy) to every new container going forward, not just the ones where it's caused a problem before.

> ⚠️ **Version correction:** the RabbitMQ image was originally set up as `rabbitmq:3-management`. Checked against Docker Hub's official page and corrected to `rabbitmq:4-management`, the current supported major version as of this update. `--hostname` was also added — RabbitMQ keys its data directory off the container hostname, and Docker's own guidance is to set this explicitly so a container recreate doesn't silently orphan the data directory under a new random hostname.

**Redis setup command (test pull, not yet in active use):**
```
docker run -d --name redis -p 6379:6379 --restart=unless-stopped redis:8
```
Chosen deliberately over `redis:latest` for reproducibility — a pinned major version (`8`) still gets patch/security updates without a surprise major-version jump changing behavior underneath the project. Note: Redis's license changed away from BSD as of 7.4 (Redis Source Available License), then moved to AGPLv3 as of Redis 8 — worth knowing if licensing comes up, though it doesn't affect local dev use here.

**SQL Server setup command (test pull, not yet in active use):**
```
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<your password>" -p 1433:1433 --name sql-server --hostname sql-server -d mcr.microsoft.com/mssql/server:2025-latest
```
- `ACCEPT_EULA=Y` and `MSSQL_SA_PASSWORD` are mandatory — the container will not start without both. Password must meet SQL Server's complexity policy (8+ characters, at least 3 of: uppercase, lowercase, digit, symbol).
- **The SA password is not recoverable** if forgotten — it's set once at container creation and used internally by SQL Server. Store it in a password manager or a local, gitignored `.env` file. Never commit it, even for a local dev/lab credential.
- No restart policy applied yet, since this is a test pull only — set one (`unless-stopped`, consistent with the other containers) once this is wired into Phase 5 for real.
- `2025-latest` tracks the newest cumulative update for SQL Server 2025 (17.x) and will drift over time, similar to `redis:latest` — consider pinning to a specific CU tag (e.g., `2025-CU1-ubuntu-22.04` once available) for reproducibility when this becomes a real dependency rather than a test pull.
- No data volume mounted yet — add `-v sql-data:/var/opt/mssql` when this is wired in for real, or data is lost on `docker rm`.

---

## Local Data Stores

- **Azure Cosmos DB Emulator** (Windows) — provides a local Cosmos account and connection string; no Azure subscription required.
- **SQL Server** — test-pulled via Docker (`mcr.microsoft.com/mssql/server:2025-latest`, see Docker section above). LocalDB vs. Docker SQL Server is still an open decision to make deliberately once Phase 5 work actually starts — Docker was chosen for this test pull to confirm the image works, not as a final commitment either way.

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
- [ ] *(Phase 5, once wired in)* `docker ps` shows `redis` container `Up`, reachable on `6379`
- [ ] *(Phase 5, once wired in)* `docker ps` shows `sql-server` container `Up`, reachable on `1433`, SA password recorded in password manager / gitignored `.env`