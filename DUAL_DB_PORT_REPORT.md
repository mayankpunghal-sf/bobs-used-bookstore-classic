# DUAL_DB_PORT_REPORT.md — Bob's Used Books Classic → dual-engine (SQL Server + PostgreSQL)

## 0. Run metadata / links

| Field | Value |
|---|---|
| Run ID | 7e3dc2f9-0b0c-43ae-bd45-0ad1c861a60d |
| Repository | https://github.com/mayankpunghal-sf/bobs-used-bookstore-classic.git |
| Branch | pending — created by the git phase after this report is written |
| Commit SHA | pending — written by the git phase |
| Pull request | pending — filled in by the git phase after this report is written |
| Confidence score | 87 — 87% of applicable checklist items passed, adjusted for 1 unresolved no-equivalent flag and 0 build warnings in changed files (see §10 and `validation.json`) |
| Build status | pass — compile-only (see §10's scope note) |
| Iteration | 0 (first validation pass, no Iterate loop needed) |
| Generated at | 2026-09-09T15:40:17Z |

## 1. Summary

The repository is a classic ASP.NET MVC 5 application on .NET Framework 4.8 whose entire data layer is Entity Framework 6.5.1 over SQL Server (pure LINQ — no raw SQL, no ADO.NET, no stored procedures). This run made it dual-engine: a startup-only configuration switch (`Data:Provider`, values `SqlServer` | `PostgreSql`, failing fast on anything else) now selects which registered EF6 provider and named connection string the application uses, with the SQL Server path byte-for-byte unchanged (the switch defaults to `SqlServer`). The PostgreSQL path rides the stable `EntityFramework6.Npgsql` 6.4.3 provider (Npgsql 4.1.3), and a full PostgreSQL 16 DDL+seed translation of the SQL Server provisioning script was added alongside the original. The build compiles clean for all five projects, with Npgsql and its EF6 provider confirmed in the web output. Four constructs could not be resolved automatically and are left as flagged open items in §9 — the most consequential being that the SQL Server `rowversion` optimistic-concurrency token has no faithful PostgreSQL equivalent and is not mapped on the PostgreSQL path. No live database of either engine was connected to in this run (see §10), so the PostgreSQL execution path is build-verified and statically reviewed but not runtime-exercised.

## 2. File sweep table

Sweep classified 175 files (`sweep-classify.mjs`; full queue in the run's agent-state `sweep.json`). Per class:

| Classification | Count | Files | Changes applied |
|---|---|---|---|
| data-access | 13 | see below | provider seam + model branch + DI wiring (detail in §5) |
| config | 7 | `App.config`, `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Areas/Admin/Views/web.config`, `Views/Web.config`, `packages.config` | `App.config`, `Web.config`, `packages.config` changed; view/web.config transforms unaffected (no DB content) |
| schema | 1 | `db-scripts/bobs-used-bookstore-classic-db.sql` | unchanged; new PG translation added beside it |
| unaffected | 154 | all remaining `.cs`/`.csproj`/`.sln`/CDK files | zero data-access signature hits (recorded per-file in sweep output) |

Data-access files and their disposition:

| File | Disposition |
|---|---|
| `app/Bookstore.Data/DatabaseProvider.cs` | **new** — provider seam (§5) |
| `app/Bookstore.Data/ApplicationDbContext.cs` | changed — provider-conditional `OnModelCreating` |
| `app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs` | changed — connection string resolved via the switch |
| `app/Bookstore.Data/Bookstore.Data.csproj` | changed — provider packages + compile item |
| `app/Bookstore.Web/Bookstore.Web.csproj` | changed — provider references (bin copy) |
| `app/Bookstore.Data/App.config`, `app/Bookstore.Web/Web.config`, `app/Bookstore.Web/packages.config` | changed — provider/switch/connection-string registration (§4) |
| 7 × `app/Bookstore.Data/Repositories/*.cs`, `PaginatedList.cs`, `BookstoreDbInitializer.cs` | verified unchanged — provider-neutral EF6 LINQ only (no ADO.NET/raw SQL/transactions) |
| `app/Bookstore.Cdk/DatabaseStack.cs`, `ConfigurationSetup.cs`, `BookstoreConfiguration.cs` | intentionally unchanged — engine-agnostic IaC/config plumbing (see §9 R9-INFRA and §11) |
| `app/Bookstore.Web/Helpers/LocalAuthenticationMiddleware.cs` | sweep false positive reclassified — OWIN middleware, no direct data access |

## 3. Inventory

| File / line | Construct | Risk class | Resolution |
|---|---|---|---|
| `Entity.cs:16-17` | `[Timestamp] byte[] RowVersion` (SQL Server rowversion concurrency token) | No equivalent | flagged (§9 R9-2) — token not mapped on PG path |
| `ApplicationDbContext.cs:40` | `HasColumnType("nvarchar")` on `Customer.Sub` | Portable | forked per engine: kept verbatim on SQL Server, default provider string mapping + `MaxLength(450)` on PG |
| `DependencyInjectionSetup.cs:45` | hardcoded `BookstoreDatabaseConnection` selection | Portable | replaced with switch-resolved name (startup-only) |
| `Web.config:164-173`, `App.config` | single EF6 provider registration (`System.Data.SqlClient`) | Portable | both providers registered; selection by connection string |
| `db-scripts/bobs-used-bookstore-classic-db.sql` | `int IDENTITY(1,1)` / `SET IDENTITY_INSERT` | Portable | `GENERATED BY DEFAULT AS IDENTITY` + explicit seed ids + sequence re-sync |
| same | `nvarchar(max)/nvarchar(450)/datetime/decimal(18,2)/bit/timestamp` columns | Portable | `text/varchar(450)/timestamp/numeric(18,2)/boolean/(omitted — xmin)` (§7) |
| same | clustered PK/index options, `PAD_INDEX`/`ON [PRIMARY]`/`TEXTIMAGE_ON` | Portable | dropped — PG heap storage, no equivalent options |
| same | `ALTER DATABASE ... SET` server options, full-text broker enable | No equivalent (N/A) | omitted — PG N/A, documented in script header |
| same | `__MigrationHistory` insert with gzipped SQL Server SSDL model blob | No equivalent | omitted — not translatable; EF6 maintains its own history table per engine |
| LINQ `Contains(...)` in repositories (e.g. `BookRepository.cs:37-93`) | translates to `LIKE` | Fork (engine behavior) | flagged (§9 R9-3) — case-sensitivity divergence documented, no code change |
| `DateTime.UtcNow` discipline (`Entity.cs:12-14` etc.) | naive `datetime` storage | Portable | `timestamp` (no tz) on PG; UTC discipline preserved (§9 R9-4) |

No `NOLOCK`, `TransactionScope`, `ExecuteScalar`, `SqlException` catches, MARS usage (connection string `MultipleActiveResultSets` exists on SQL Server only — no PG equivalent needed; nothing in code depends on multi-result-set behavior), three-part names, temp tables, or stored procedures exist in the codebase (grep + `scan-three-part-names.mjs` negative).

## 4. Config changes

- **Switch:** `<add key="Data:Provider" value="SqlServer" />` in `Web.config` appSettings. Values: `SqlServer` | `PostgreSql` (case-insensitive). Any missing/invalid value throws at startup naming the key and allowed values — no default engine, no sniffing. Default `SqlServer` preserves today's behavior on every existing deployment.
- **Connection strings (placeholders only; no credentials anywhere in source):**
  - `BookstoreDatabaseConnection` — unchanged (localdb/SSPI locally; AWS deployments inject the RDS SQL Server string from SSM Parameter Store via `ConfigurationSetup.cs`, unchanged).
  - `BookstoreDatabaseConnection_PostgreSql` — `Host=localhost;Port=5432;Database=bookstoreclassic;Username=PLACEHOLDER;Password=PLACEHOLDER` (`providerName="Npgsql"`). Replace placeholders per environment.
- **Startup validation:** `DatabaseProviderAccessor` asserts the selected provider's connection string exists and is non-empty before first use.
- **Provider registration:** `entityFramework/providers` now registers `System.Data.SqlClient` (unchanged) and `Npgsql` (`Npgsql.NpgsqlServices, EntityFramework6.Npgsql`); `system.data/DbProviderFactories` registers the Npgsql factory in `Web.config` (and the EF provider entry in `Bookstore.Data/App.config`).
- Both engines are never translated into each other at runtime — two named connection strings sit side by side; the switch only picks one at startup.

## 5. Seams introduced

| Type | Responsibility | Registration / use site |
|---|---|---|
| `Bookstore.Data.DatabaseProvider` (enum) | typed engine identity {`SqlServer`, `PostgreSql`} | `DatabaseProviderAccessor` |
| `Bookstore.Data.DatabaseProviderAccessor` (static) | resolves `Data:Provider` **once** into a typed value; fail-fast on missing/invalid; exposes `ConnectionStringName`; asserts the selected connection string exists/non-empty | consumed by `ApplicationDbContext.OnModelCreating` and `DependencyInjectionSetup.cs:45` |

Scope note: the codebase has no raw SQL, no bulk-insert paths, and no dialect-conditional SQL fragments, so `ISqlQueryProvider`/`ISqlDialect`/`IBulkInserter` adapters were deliberately **not** scaffolded — they would be dead code with zero call sites (R10; approved plan Tier 1 scoped the seam to the provider resolution actually exercised). EF6 itself already provides the provider-abstraction layer this application needs; if raw SQL is introduced later, `ISqlQueryProvider` becomes the required seam.

## 6. SQL changes

Application code contains no SQL statements (LINQ only), so no statement-level conversions were required. The one SQL artifact — the SQL Server provisioning script — was translated per-engine into **`db-scripts/bobs-used-bookstore-classic-db-pg.sql`** (PG 16): 9 tables, quoted PascalCase identifiers, 32 seed rows, 17 indexes, 15 FKs, inside one transaction; identity sequences re-synced via `setval` after seeding. The original script is untouched and remains authoritative for SQL Server. The SQL Server path additionally keeps `MultipleActiveResultSets=true` in its connection string (its localdb default); the PG string has no MARS key (§5.8 does not apply — no code depends on multiple active result sets).

## 7. Type mapping applied

| SQL Server | PostgreSQL | Note |
|---|---|---|
| `nvarchar(max)` | `text` | lossless |
| `nvarchar(450)` (`Customer.Sub`) | `varchar(450)` | length preserved; unique index carried over (`IX_Sub`) |
| `datetime` | `timestamp` (without time zone) | deviation flagged (§9 R9-4): values are UTC by application discipline; `timestamptz` rejected for provider-behavior parity |
| `decimal(18,2)` | `numeric(18,2)` | precision/scale exact; no bare `DECIMAL` anywhere (script scan: clean) |
| `bit` | `boolean` | EF maps `bool` natively; B10 satisfied |
| `int IDENTITY(1,1)` | `int GENERATED BY DEFAULT AS IDENTITY` | PG10+ feature, safe at resolved target 16 |
| `rowversion` | *(no column)* | deviation flagged (§9 R9-2): PG exposes the auto-updating system column `xmin`, but the EF6 Npgsql provider has no EF-Core-style native `xmin` mapping, and mapping a property to a column named `xmin` collides with PG's reserved system-column rules in both DDL generation and INSERT — so the token is not mapped on the PG path |

## 8. Checklist result summary

Full per-item detail lives in the run's `validation.json` (agent-state dir). Counts by group:

| Group | pass | flagged | fail | not_applicable |
|---|---|---|---|---|
| core-wiring | 16 | 0 | 0 | 1 (R6 — no raw SQL exists) |
| dotnet-breaks | 1 (§5.9) | 1 (§5.11) | 0 | 16 (§5.1-5.8, 5.10, 5.12-5.15 — signals absent) |
| configuration | 4 | 0 | 0 | 1 (no PgBouncer) |
| schema-ddl | 5 | 0 | 0 | 2 (no synonyms/partitioning) |
| naming | 2 | 0 | 0 | 2 (no PL/pgSQL written; prefix check ran clean/empty) |
| syntax-behavior | 6 | 0 | 0 | 27 (constructs absent from DDL/LINQ) |
| types | 1 | 0 | 0 | 2 (no GUID columns / exotic types) |
| per-technology | 1 (EF6 §11) | 0 | 0 | 1 (no stored procedures) |
| sign-off | 2 | 0 | 0 | 0 |
| **total** | **38** | **1** | **0** | **52** |

Confidence: `100 * (38/39) - 10*1 - 5*0 = 87` (38 passed / 39 applicable; 1 unresolved no-equivalent flag; 0 build warnings in changed files).

## 9. R9 flags — open items for human review

### R9-1 — EF6 + PostgreSQL provider is a niche pairing
- **Construct:** Entity Framework 6 on .NET Framework 4.8 targeting PostgreSQL.
- **Location(s):** `app/Bookstore.Data/Bookstore.Data.csproj` (new `EntityFramework6.Npgsql` 6.4.3 reference), `app/Bookstore.Web/Bookstore.Web.csproj`, `packages.config`, `Web.config` provider entries.
- **Why unresolved automatically:** the EF6+Npgsql provider was re-released only recently (`EntityFramework6.Npgsql` 6.4.x, built against EF 6.4/Npgsql 4.1) and has a small usage footprint compared to the EF Core provider; its PG16-era behavior (identifier quoting on code-first DDL, identity handling through the initializer) cannot be proven without a live database, which this run does not connect to.
- **Options considered:** (a) keep EF6 + `EntityFramework6.Npgsql` (chosen — zero application-code change, SQL Server path untouched); (b) migrate to EF Core + `Npgsql.EntityFrameworkCore.PostgreSQL` (modern, actively maintained, but a framework rewrite of every repository — R2/R3 out of scope); (c) replace EF6 with a hand-written dual-dialect Dapper/ADO.NET layer (major rewrite, new SQL surface to maintain).
- **Recommended action:** stay on (a) for the incremental migration, and require the QA runtime pass (§10) before any PG environment goes live; revisit (b) when the SQL Server retirement is scheduled.
- **Risk if left unaddressed:** if the provider's code-first DDL mis-handles quoted PascalCase identifiers or identity columns on PG 16, the `DropCreateDatabaseIfModelChanges` initializer path fails at first PG startup — surfacing as a 500 on first request, not a build error.

### R9-2 — Optimistic-concurrency token has no PostgreSQL mapping
- **Construct:** `[Timestamp] byte[] RowVersion` on the `Entity` base class (SQL Server rowversion).
- **Location(s):** `app/Bookstore.Domain/Entity.cs:16-17` (inherited by all 10 mapped entities); `app/Bookstore.Data/ApplicationDbContext.cs` (PG branch now ignores it); `db-scripts/bobs-used-bookstore-classic-db-pg.sql` (no `RowVersion` column).
- **Why unresolved automatically:** PostgreSQL's equivalent is the system column `xmin`, but the EF6 Npgsql provider does not implement the EF-Core-style automatic `xmin` mapping, and mapping the property to a user column named `xmin` is rejected by PostgreSQL (reserved system-column name) in both DDL generation and INSERT paths. The remaining options require either a schema-adjacent change or acceptance of weaker concurrency semantics — a product decision.
- **Options considered:** (a) map `RowVersion` to a real PG `bytea` column updated by a trigger (preserves EF-level `DbUpdateConcurrencyException` behavior; adds trigger DDL and write amplification); (b) leave the token unmapped on PG (chosen for this run — no false sense of protection; concurrent-update detection is lost on PG only); (c) map to `xmin` via column-name override (rejected — collides with reserved system-column rules, see above).
- **Recommended action:** accept (b) short-term (write conflicts are rare in this workload — single-admin inventory edits), and adopt (a) if/when concurrent-update anomalies are observed or multi-writer admin traffic grows.
- **Risk if left unaddressed:** on PostgreSQL, two concurrent updates to the same book/order both succeed last-write-wins, silently overwriting the first writer's change (e.g. two admins adjusting stock at once) — no error is raised where SQL Server would have thrown a concurrency exception.

### R9-3 — Search becomes case-sensitive on PostgreSQL
- **Construct:** LINQ `string.Contains(...)` filters (translate to `LIKE`).
- **Location(s):** `app/Bookstore.Data/Repositories/BookRepository.cs:37,42,89-93`; `OfferRepository.cs`, `CustomerRepository.cs`, `ReferenceDataRepository.cs` (same pattern); `SearchController`.
- **Why unresolved automatically:** SQL Server's default collation is case-insensitive, so `Contains("hobbit")` matches "The Hobbit" today; PG `LIKE` is case-sensitive. The fixes are either a schema change (`citext`/collation — forbidden by `allow_schema_changes=false`), a query rewrite (`ToLower()` both sides — changes query semantics/performance, R2), or provider-level `ILIKE` translation (not supported by the EF6 provider) — none automatable.
- **Options considered:** (a) accept case-sensitive search on PG and document (chosen); (b) `citext` columns for searched fields (schema change, needs `allow_schema_changes=true` + data migration); (c) query rewrite to case-fold both sides (R2 trade-off: index usage lost on those predicates).
- **Recommended action:** (a) now; if users report missing results after cutover, prefer (b) for `Book.Name`/`Author`/`ISBN` only.
- **Risk if left unaddressed:** search/filter pages silently return fewer or empty results on PG for mixed-case input ("harry potter" finds nothing) — a correctness regression users will notice immediately, though no data is lost.

### R9-4 — `datetime` columns map to timezone-naive `timestamp`
- **Construct:** `DateTime` columns (`CreatedOn`, `UpdatedOn`, `DeliveryDate`, `DateOfBirth`), all populated with `DateTime.UtcNow` in application code.
- **Location(s):** `app/Bookstore.Domain/Entity.cs:12-14`, `app/Bookstore.Domain/Orders/Order.cs:30`; PG DDL: all `"CreatedOn"/"UpdatedOn"/"DeliveryDate"/"DateOfBirth"` columns.
- **Why unresolved automatically:** SQL Server `datetime` is timezone-naive, so `timestamp` preserves engine parity; `timestamptz` is arguably more correct for UTC values but changes read-back behavior under the older provider and diverges from the SQL Server path this run must not alter (R1).
- **Options considered:** (a) `timestamp` without time zone + keep app-side UTC discipline (chosen); (b) `timestamptz` (more correct semantics; riskier with the older provider, and PG-DRIFT from the SQL Server schema).
- **Recommended action:** (a), plus a QA assertion that every PG session reads/writes UTC-consistent values; revisit (b) only in a dedicated schema-hygiene change.
- **Risk if left unaddressed:** if any future writer uses `DateTime.Now` (server-local) instead of `UtcNow`, naive `timestamp` storage will silently mix local and UTC instants in the same column — order delivery dates and month-statistics queries (which filter on `DateTime.UtcNow`) will be wrong by the server's UTC offset.

### R9-INFRA — PostgreSQL infrastructure is not provisioned by this repo
- **Construct:** `app/Bookstore.Cdk/DatabaseStack.cs` deploys RDS **SQL Server Express 2017** only (`DatabaseStack.cs:51-54`, port 1433) and publishes its connection string to SSM.
- **Why unresolved automatically:** standing up a PG RDS instance/stack is an infrastructure decision (instance class, storage, backups, secrets rotation) outside a code port; the SSM parameter value itself is engine-agnostic, so no code change is required to point the app at PG.
- **Options considered:** (a) provision PG externally and paste its connection string into the same SSM parameter (chosen — zero code change); (b) add a CDK `PostgreSql` variant stack (clean IaC, but new standing infra the client may not want yet).
- **Recommended action:** (a) for the pilot; adopt (b) when PG becomes the primary engine.
- **Risk if left unaddressed:** none in code — but a PG environment will not exist until someone provisions one; teams may mistake a working build for a working PG deployment.

## 10. Verification

- **Build (compile-only):** `MSBuild.exe BobsBookstoreClassic.sln -t:restore,build -p:RestorePackagesConfig=true` → **succeeded**, all 5 projects. Warnings in changed files: 0 (all build warnings are pre-existing: Magick.NET NuGet advisories, CDK obsolete-API notices, CDK TFM advisories). `Npgsql.dll` and `EntityFramework6.Npgsql.dll` confirmed in `app/Bookstore.Web/bin/`.
- **Static checks:** reserved words 0 collisions; identifier length 0 over-63-byte names / 0 truncation collisions; bare `DECIMAL` 0; three-part SQL names 0 (matches were C# property chains); PL/pgSQL param-prefix check 0 (no PL/pgSQL); batched grep: 0 provider conditionals outside the model-creation seam, 0 `SqlConnection`/`Sql*` concrete types, 0 `NOLOCK`, 0 `TransactionScope`.
- **Scope note — please read:** this run performed **no live dual-engine testing**. No SQL Server or PostgreSQL instance was connected to, no tests were executed against either engine, and the `DropCreateDatabaseIfModelChanges` initializer was never run. That verification is QA's responsibility. Build/compile verification and static checklist cross-checks only.
- **Unverified paths (R11 — every one listed):** (1) PG code-first initializer DDL on PG 16 (identifier quoting, identity handling — see R9-1); (2) the PG provisioning script execution end-to-end; (3) EF6 LINQ query translation parity for `Contains`/`Skip`/`Take`/`GroupBy` under the Npgsql EF6 provider; (4) concurrency-token absence behavior on PG (R9-2); (5) SQL Server regression beyond compile-level (switch defaults to `SqlServer`, model branch is byte-for-byte the original code path, but no live run occurred).

## 11. Rollout prerequisites

1. **PostgreSQL target version:** **16 by default — unverified.** No docker-compose, CI service config, IaC, or runbook in the repo names a PG version (the CDK stack deploys SQL Server only), so `pg_version` fell back to the skill default 16 (`resolve-pg-version.mjs`, source "default (unverified)"). Every version-gated feature used (`GENERATED ... AS IDENTITY`, PG10+) is safe at 16; confirm the actual engine version when provisioning (R9-INFRA).
2. **Provision a PostgreSQL 16 instance** (RDS or otherwise) and publish its connection string into the `BookstoreDatabaseConnection_PostgreSql` connection string / SSM parameter — placeholders must be replaced (no real credentials belong in source).
3. **Auth model:** PG connection uses username/password (no Windows auth). TLS: add `SSL Mode=Require` (and `Trust Server Certificate` only for self-signed dev certs) to the PG connection string in production.
4. **Run the PG provisioning script** (`db-scripts/bobs-used-bookstore-classic-db-pg.sql`) **or** let the EF initializer create the schema — pick one per environment and do not mix; the script already re-syncs identity sequences after seeded ids (`setval` — required runbook step after any explicit-id seed).
5. **Switch:** set `Data:Provider=PostgreSql` per environment to activate the PG path; `SqlServer` (default) preserves current behavior exactly.
6. **search_path:** default (`"$user", public`) is in effect; the app never issues session-level `SET search_path`. Optionally pin `Search Path=public` in the connection string for explicitness.
7. **Pooling:** Npgsql's built-in pooling is in play; no PgBouncer is deployed. If one is added later in transaction-pooling mode, configure `max_prepared_statements` (PgBouncer 1.21+) or disable prepared statements client-side first.
8. **No retry-on-40001 paths / no `max_prepared_transactions` requirement** — no `REPEATABLE READ`/`SERIALIZABLE` usage and no distributed transactions exist in the codebase.
9. **Long-connection hygiene (new operational surface vs SQL Server):** PG autovacuum/bloat exposure from idle-in-transaction sessions — the app uses per-request `DbContext` lifetimes (Autofac `InstancePerRequest`) so this should not occur, but monitor `pg_stat_activity` for `idle in transaction` after cutover.
10. **QA runtime pass required** covering both engines before any PG environment is declared live (§10's unverified list is the test charter).

## 12. Iteration history

N/A — iteration 0; first validation pass passed with no Iterate loop.
