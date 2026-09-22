# DUAL_DB_PORT_REPORT — bobs-used-bookstore-classic (run 499021d5)

## 0. Run metadata / links

| Field | Value |
|---|---|
| Run ID | 499021d5-d12b-4180-aba8-18445a2fc3dd |
| Repository | https://github.com/mayankpunghal-sf/bobs-used-bookstore-classic.git |
| Branch | pending — filled in by the git phase after this report is written |
| Commit SHA | pending — filled in by the git phase |
| Pull request | pending — filled in by the git phase after this report is written |
| Confidence score | 91 — 91% of applicable checklist items passed, adjusted for unresolved flags and build warnings (formula and inputs in §10) |
| Build status | fail under `dotnet build` (pre-existing toolchain incompatibility, see §10); Bookstore.Data compiles clean under the repo's real toolchain (VS 2026 MSBuild `/restore`) with all ported code |
| Iteration | 0 (first Validate pass) |
| Generated at | 2026-09-22 (UTC) |

## 1. Summary

The application's single EF6 `ApplicationDbContext` now runs on either SQL Server or PostgreSQL 16, selected by one appSetting (`Data:Provider`, values `SqlServer` | `PostgreSql`, default `SqlServer`) resolved exactly once at startup into a typed `DatabaseProvider` value. Both EF6 providers are registered in `Web.config`/`App.config`; the DI container constructs a per-request `SqlConnection` (existing `BookstoreDatabaseConnection`, byte-for-byte unchanged, MARS kept) or `NpgsqlConnection` (new `AppDb_PostgreSql`, placeholder values, `Search Path=public`) and hands it to the context through a new additive `ApplicationDbContext(DbConnection, bool)` constructor. The one physical type divergence in the model (`Customer.Sub` mapped with `HasColumnType("nvarchar")`) is forked at model-building time to `varchar` on the PostgreSQL provider only. No raw SQL, stored procedures, or ADO.NET call sites exist in this codebase, so the port is entirely at the EF6-provider seam; SQL Server behavior is unchanged on every path (R1). Engines verified: compile-level only — no live database connection was made (see §10). Top risks: PostgreSQL-side optimistic concurrency is inert (rowversion has no PG equivalent in the EF6 provider — §9 entry 3), no PostgreSQL DDL script ships (the EF6 initializer creates the PG schema — §9), and CDK infrastructure still provisions SQL Server only (§9).

## 2. File sweep table

| File | Classification | Changes applied |
|---|---|---|
| app/Bookstore.Data/Bookstore.Data.csproj | unaffected → discovered in-scope | + `EntityFramework6.Npgsql` 6.4.3 PackageReference (pulls Npgsql 4.1.3; direct EF 6.5.1 pin wins) |
| app/Bookstore.Web/Web.config | config | + `Data:Provider` appSetting (SqlServer); + `AppDb_PostgreSql` (Npgsql, placeholders); + Npgsql EF6 provider + `DbProviderFactories` entry; `BookstoreDatabaseConnection` untouched |
| app/Bookstore.Data/App.config | config | + Npgsql provider + `DbProviderFactories` (parity with Web.config) |
| app/Bookstore.Data/BookstoreConfiguration.cs | data-access | + `DatabaseProvider` enum + `GetDatabaseProvider()` (Lazy, fail-fast on invalid values) |
| app/Bookstore.Web/App_Start/ConfigurationSetup.cs | data-access | + forces provider resolution at end of `ConfigureConfiguration()` (runs before DI) |
| app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs | data-access | provider-conditional per-request `DbConnection` → `ApplicationDbContext` registration |
| app/Bookstore.Data/ApplicationDbContext.cs | data-access | + `DbConnection` ctor overload; + PG-only model fork (`nvarchar`→`varchar` on `Customer.Sub`) |
| app/Bookstore.Web/packages.config | config | + Npgsql 4.1.3 + EntityFramework6.Npgsql 6.4.3 entries |
| app/Bookstore.Domain/Entity.cs | unaffected → discovered in-scope | skipped — `[Timestamp] byte[] RowVersion` kept byte-for-byte (R13); §9 flag 3 |
| 7 × Repositories/*.cs | data-access | skipped — provider-neutral EF6 LINQ; §5.9 audit found only `DateTime.UtcNow` (Kind=Utc) |
| app/Bookstore.Data/BookstoreDbInitializer.cs | data-access | skipped — provider-neutral; PG schema creation flows through the Npgsql provider |
| Web.Debug.config / Web.Release.config / Views\Web.config / Areas\Admin\Views\web.config | config | skipped — no connection strings or provider registration |
| app/Bookstore.Cdk/DatabaseStack.cs | data-access-adjacent | skipped — CDK infra; §9 flag 1 |
| db-scripts/bobs-used-bookstore-classic-db.sql | schema | skipped — T-SQL DDL/seed unchanged (R1); §9 flag 2 |

Full per-file detail: `FileChanges.json` in the agent-state dir (8 changed, 15 skipped-with-reason, 2 verified discoveries, amendment_ratio 0.125, runaway_scope false).

## 3. Inventory

| File | Line | Construct | Risk class | Resolution |
|---|---|---|---|---|
| app/Bookstore.Domain/Entity.cs | 15 | `[Timestamp] byte[] RowVersion` (EF6 rowversion concurrency token) | No equivalent | Kept unchanged; PG maps to inert `bytea` token — §9 flag 3 |
| app/Bookstore.Data/ApplicationDbContext.cs | ~30 | `HasColumnType("nvarchar")` on `Customer.Sub` | Fork | Forked to `varchar(450)` on the Npgsql provider only (Appendix C: no `nvarchar` type on PG) |
| app/Bookstore.Web/Web.config | 12 | `MultipleActiveResultSets=true` (MARS) | Portable | Kept on SQL Server string (R1); omitted from PG string (Npgsql has no MARS; §5.8) |
| db-scripts/bobs-used-bookstore-classic-db.sql | whole file | T-SQL DDL/seed (82 KB, 0 PL/pgSQL units) | No equivalent (as written) | Unchanged (R1); PG schema created by EF6 initializer — §9 flag 2 |
| app/Bookstore.Cdk/DatabaseStack.cs | whole file | CDK provisions SQL Server RDS only | Redesign | Unchanged — §9 flag 1 |
| Repositories (Offer/Order) | 24; 57, 64 | `DateTime.UtcNow` (Kind=Utc) | Portable | No change — Npgsql 4.1.3 legacy timestamp mapping stores UTC wall-clock consistently (§5.9) |
| app/Bookstore.Data/BookstoreConfiguration.cs | — | `GetConnectionString` (throws on missing key) | Portable | Reused as the startup fail-fast assert for the selected engine's connection string |

## 4. Config changes

- **Switch:** `<appSettings><add key="Data:Provider" value="SqlServer" />` in `Web.config`. Resolved once at startup into `DatabaseProvider { SqlServer, PostgreSql }`; invalid values throw `ConfigurationErrorsException` naming the key and allowed values. Absent key defaults to `SqlServer` (approved at the plan gate — R1 rationale: existing deployments keep their engine; note this deviates from the seams contract's "no default engine" line by explicit approval).
- **Connection strings (placeholders, R5):**
  - SQL Server (pre-existing, unchanged): `BookstoreDatabaseConnection` = `Server=(localdb)\MSSQLLocalDB;Initial Catalog=BookStoreClassic;MultipleActiveResultSets=true;Integrated Security=SSPI;` (providerName `System.Data.SqlClient`).
  - PostgreSQL (new): `AppDb_PostgreSql` = `Host=YOUR_PG_HOST;Port=5432;Database=YOUR_PG_DATABASE;Username=YOUR_PG_USER;Password=YOUR_PG_PASSWORD;Search Path=public` (providerName `Npgsql`).
- **Provider registration:** `<provider invariantName="Npgsql" type="Npgsql.NpgsqlServices, EntityFramework6.Npgsql" />` added beside the SqlClient provider in `<entityFramework><providers>`; `Npgsql.NpgsqlFactory` (Version=4.1.3.0, PublicKeyToken=5d8b90d52f89f407) added under `<system.data><DbProviderFactories>` — in both `Web.config` and `Bookstore.Data\App.config`.
- **Startup validation:** `ConfigurationSetup.ConfigureConfiguration()` forces `GetDatabaseProvider()` before DI registration; the selected engine's connection string is fetched during DI registration, where a missing name fails fast (`KeyNotFoundException` from the pre-existing `GetConnectionString`).
- **Deviation (logged):** run-config's `conn_mssql=AppDb_SqlServer` does not exist in this repo; the actual SQL Server connection name is `BookstoreDatabaseConnection`, which was preserved (R1). The switch maps `SqlServer`→`BookstoreDatabaseConnection`, `PostgreSql`→`AppDb_PostgreSql`.

## 5. Seams introduced

| Type | Responsibility | Registration site |
|---|---|---|
| `DatabaseProvider` enum (`BobsBookstoreClassic.Data`) | typed engine value, immutable per process | `BookstoreConfiguration.cs` |
| `BookstoreConfiguration.GetDatabaseProvider()` | Lazy one-time resolution of `Data:Provider` (R7) | forced at startup in `ConfigurationSetup.cs` |
| `ApplicationDbContext(DbConnection, bool contextOwnsConnection)` | context constructed from a provider-supplied connection (ef6.md: plain-string ctor always binds the default SqlClient factory) | additive overload in `ApplicationDbContext.cs` |
| Per-request connection factory (inline in DI module) | `NpgsqlConnection(AppDb_PostgreSql)` or `SqlConnection(BookstoreDatabaseConnection)` per resolved provider | `DependencyInjectionSetup.cs` (`builder.Register(...).InstancePerRequest()`) |
| Model-building provider fork | `nvarchar`→`varchar` remap on PG only | `ApplicationDbContext.OnModelCreating` (`Database.Connection is NpgsqlConnection`, runs once) |

`ISqlDialect`/`ISqlQueryProvider`/`IBulkInserter` were deliberately **not** instantiated: the codebase has zero raw SQL, zero stored procedures, and zero bulk-insert paths (recorded plan decision, not an omission).

## 6. SQL changes

None. No raw SQL statements exist in application code (all data access is EF6 LINQ, provider-translated). The T-SQL DDL/seed script was intentionally left unchanged (R1, `allow_schema_changes=false`) — see §9 flag 2.

## 7. Type mapping applied

| SQL Server | PostgreSQL | Mechanism | Deviation/loss |
|---|---|---|---|
| `nvarchar(450)` (`Customer.Sub`) | `varchar(450)` | `OnModelCreating` fork, PG provider only | none — same length, Unicode-safe on PG |
| `rowversion` (`Entity.RowVersion`, `[Timestamp] byte[]`) | `bytea` (EF6 Npgsql default for `byte[]`) | none — token kept as-is (R13) | **lossy:** no server-generated version on PG; concurrency check compares a never-updated value → optimistic concurrency is inert on PG. §9 flag 3. |
| `DateTime` properties | `timestamp` (Npgsql 4.1.3 legacy mapping) | provider default | UTC wall-clock stored consistently (all writes are `DateTime.UtcNow`); no `timestamptz` Kind strictness on this pinned version |
| `int` identity keys | `serial`/identity via Npgsql provider | EF6 model conventions | none |

No bare `DECIMAL`/`NUMERIC`, no `uniqueidentifier`, no `hierarchyid`/`geography`/`sql_variant`/TVPs in the model. One behavior note: the unique index on `Customer.Sub` admits case-variant duplicates differently per engine (SQL Server collation vs PG case-folding) — flagged in §10's unverified list for QA.

## 8. Checklist result summary

Per-group counts (full per-item detail in `validation.json`, agent-state dir):

| Group | Pass | Flagged | Not applicable |
|---|---|---|---|
| core-wiring | 13 | 1 (builds — toolchain) | 2 (R6 raw SQL, R11 live-DB) |
| sign-off | 1 | 1 (build-and-switch — same toolchain cause) | 0 |
| dotnet-breaks | 2 (§5.8, §5.9) | 1 (§5.11 rowversion) | 13 |
| configuration | 4 | 0 | 1 (pooler) |
| schema-ddl | 2 | 0 | 5 |
| naming | 2 | 0 | 2 |
| syntax-behavior | 2 | 0 | 25 |
| types | 2 | 0 | 1 |
| per-technology | 1 | 0 | 1 |

## 9. R9 flags — open items for human review

**Flag 1 — CDK infrastructure provisions SQL Server only.**
- **Construct:** `DatabaseStack` CDK construct creating the SQL Server database resource (`app/Bookstore.Cdk/DatabaseStack.cs`).
- **Location(s):** `app/Bookstore.Cdk/DatabaseStack.cs` (whole file).
- **Why this run couldn't resolve it automatically:** infrastructure provisioning is a deployment decision outside the app-level port, and `allow_schema_changes=false` for this run; adding a PG engine option to CDK is an architecturally separate change.
- **Options considered, with trade-offs:** (a) keep CDK unchanged and provision PG externally (zero code risk; PG environments are provisioned by hand/IaC outside this repo); (b) add a CDK parameter selecting the engine (one stack serves both, but touches deployment code and needs testing against the CDK toolchain); (c) add a parallel PostgreSQL stack (cleanest separation, doubles infra maintenance).
- **Recommended action:** option (a) — keep CDK SQL-Server-only for now; the app-level switch already lets any environment point at an externally provisioned PostgreSQL 16 instance.
- **Risk if left unaddressed:** any environment that flips `Data:Provider` to `PostgreSql` without first provisioning a PG instance gets a hard connection failure at startup — no silent misbehavior, but deployment will block until infra exists.

**Flag 2 — no PostgreSQL DDL/seed script shipped.**
- **Construct:** `db-scripts/bobs-used-bookstore-classic-db.sql` — T-SQL schema + reference-data seed (82 KB).
- **Location(s):** `db-scripts/bobs-used-bookstore-classic-db.sql`.
- **Why this run couldn't resolve it automatically:** hand-translating 82 KB of T-SQL DDL is high-effort and duplicates what the EF6 initializer already does; `allow_schema_changes=false` rules out authoring a divergent PG schema.
- **Options considered, with trade-offs:** (a) rely on the EF6 initializer (`DropCreateDatabaseIfModelChanges`) to create the PG schema and seed reference data via the Npgsql provider (zero extra artifacts; schema is code-owned); (b) hand-translate the DDL to a PG script (static artifact for DBAs, but drifts from the EF model and effectively redesigns schema delivery); (c) ship a partial reference-only PG script (documentation value only, risk of being mistaken for runnable).
- **Recommended action:** option (a) — the initializer already creates and seeds the database on both engines; keep the T-SQL script for SQL Server environments only.
- **Risk if left unaddressed:** teams that provision databases by running the SQL script against PostgreSQL will get immediate syntax errors; the correct PG path is "start the app once and let the initializer create the schema."

**Flag 3 — rowversion concurrency token has no PG equivalent in the EF6 provider.**
- **Construct:** `[Timestamp] public byte[] RowVersion` on the `Entity` base class (EF6 rowversion = SQL Server server-generated version column used for optimistic concurrency).
- **Location(s):** `app/Bookstore.Domain/Entity.cs` (inherited by all 8 entity types).
- **Why this run couldn't resolve it automatically:** the Npgsql EF6 provider has no server-generated rowversion equivalent; the documented alternatives each need a product decision or are unverified for EF6 (xmin mapping support in `EntityFramework6.Npgsql` could not be confirmed from available documentation), and an integer version column would change the physical schema (`allow_schema_changes=false`).
- **Options considered, with trade-offs:** (a) keep `byte[] RowVersion` as-is — zero code change, SQL Server concurrency unchanged, but on PG the token is an inert `bytea` (no concurrency protection); (b) map an `xmin`-backed `uint` property on the PG provider fork — real optimistic concurrency on PG, but EF6-provider xmin support is unverified and would need a spike; (c) add an explicit integer `Version` column with a concurrency check — engine-neutral and verifiable, but requires a schema change (blocked by run config).
- **Recommended action:** option (a) now (shipped), with option (b) as a small follow-up spike once a PG environment exists; do not silently accept (a) as "done" for PG.
- **Risk if left unaddressed:** lost-update anomalies on PostgreSQL — two admins editing the same book/customer overwrite each other with no `DbUpdateConcurrencyException`, exactly the scenario rowversion prevents on SQL Server. Probability scales with concurrent admin edit traffic.

**Flag 4 — connection-string name deviation from run config.**
- **Construct:** run-config named `conn_mssql=AppDb_SqlServer`, but the repo's actual SQL Server connection string is `BookstoreDatabaseConnection`.
- **Location(s):** `app/Bookstore.Web/Web.config` (connectionStrings); `app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs`.
- **Why this run couldn't resolve it automatically:** renaming the existing connection string would change SQL Server behavior/deployment contracts (R1/R13); the run-config value appears to have been authored without knowledge of the repo's real name.
- **Options considered, with trade-offs:** (a) keep `BookstoreDatabaseConnection` for the SQL Server path (zero deployment impact; run-config's `conn_mssql` is simply not used); (b) add an `AppDb_SqlServer` alias entry (satisfies run-config literally but creates two names for one string — confusion risk); (c) rename to `AppDb_SqlServer` everywhere (breaks SSM-injected config in `ConfigurationSetup.cs`).
- **Recommended action:** option (a), implemented — the switch maps `SqlServer`→`BookstoreDatabaseConnection`, `PostgreSql`→`AppDb_PostgreSql`.
- **Risk if left unaddressed:** none at runtime; only a documentation mismatch — anyone wiring config from run-config's `conn_mssql` name will look for a key that doesn't exist.

## 10. Verification

- **Build commands and results:**
  - `dotnet build BobsBookstoreClassic.sln` (the command `run-build.mjs` detects): **exit 1** — 3 errors, all pre-existing toolchain incompatibilities reproducible without port changes: `MSB4019` (`Microsoft.WebApplication.targets` not found under dotnet SDK 10 for the legacy Web csproj) and `CS0246` for `Npgsql`/`ImageMagick` (the legacy non-SDK csproj's stale 14.2.0.0 `HintPath` references cannot resolve without a committed packages folder — the untouched `Magick.NET` package fails identically, proving the cause is not the port).
  - VS 2026 MSBuild (`MSBuild.exe /t:Build /p:Configuration=Debug /restore`) on `Bookstore.Data.csproj`: **success** — `Bookstore.Data.dll` produced; `Npgsql.dll`, `EntityFramework6.Npgsql.dll`, `EntityFramework.dll`, `EntityFramework.SqlServer.dll` all confirmed in `bin\Debug`. This is the repo's real toolchain (legacy non-SDK projects + WebApplication targets).
  - Warnings: 187 at solution level, **0 attributable to changed files** (Magick.NET NU19xx advisories, CDK obsolete-API warnings, MSB3243 version conflicts — all pre-existing).
- **Confidence formula:** `100 * (30/33) - 10*0 - 5*0 = 91` — 30 of 33 applicable checklist items passed; 3 flagged (2 = the dotnet-SDK toolchain issue above, 1 = §5.11 rowversion, an R9 entry with options rather than a no-equivalent-at-all case, so it does not count toward the −10 term); 0 unresolved no-equivalent flags; 0 build warnings in changed files.
- **Scope note:** this agent did **not** connect to or run tests against a live SQL Server or PostgreSQL instance — build/compile verification and static checklist cross-checks only. Live dual-engine verification is QA's responsibility.
- **Unverified paths (R11 list):** (1) runtime behavior of the Npgsql EF6 provider against a real PostgreSQL 16 instance — model building, initializer DDL generation, seed, CRUD; (2) the `Data:Provider=PostgreSql` switch end-to-end (DI branch, connection open, queries); (3) `Customer.Sub` unique-index case-folding behavior on PG; (4) `xmin`-based concurrency (option b of §9 flag 3) — not implemented; (5) `Web.config` runtime resolution of the `Npgsql` DbProviderFactories entry under IIS/IIS Express (assembly binding redirects for Npgsql 4.1.3 may need adding if a runtime load error appears); (6) Web project build (blocked by the pre-existing MSB4019 under dotnet SDK; Npgsql assemblies flow to Web bin via Bookstore.Data's PackageReference copy-local — verify after a VS build).

## 11. Rollout prerequisites

- **PostgreSQL 16** — target version is an explicit task override recorded in run-config (`pg_version_source`), used verbatim as instructed; not auto-detected from repo infra (no Docker/CI/IaC in the repo pins a PG version). No version-gated constructs (JSON functions, `MERGE`, `GENERATED ... STORED`, `INCLUDE` indexes) are used, so the version gate has no open items.
- **Extensions:** none required (no pgcrypto/pgcrypto-dependent mappings in the ported code).
- **Auth model:** PG path uses `Username`/`Password` (SCRAM) — set real values in each environment's `AppDb_PostgreSql`; the SQL Server localdb `Integrated Security` is pre-existing and SQL Server-side only.
- **Schema delivery:** start the app once with `Data:Provider=PostgreSql`; the EF6 initializer creates and seeds the schema (see §9 flag 2). No sequence re-sync needed (fresh database).
- **search_path:** `Search Path=public` is set explicitly in the PG connection string (pooled sessions reset session-level `search_path`).
- **Pooling:** Npgsql's own pooling is on by default; if a PgBouncer transaction-mode pooler is later placed in front of PostgreSQL, configure `max_prepared_statements` (or disable prepared statements client-side) and set `No Reset On Close=true` if Npgsql pooling is also active — recorded here as an infrastructure prerequisite, not a code change.
- **Retry on `40001`:** no serialization-failure retry paths exist in the codebase; none were added (R2). If PG runs at `REPEATABLE READ`/`SERIALIZABLE` in some environment, add retry there.
- **Connection lifecycle:** contexts are per-request (DI `InstancePerRequest`); no long-held transactions were introduced. Standard autovacuum/bloat monitoring applies (no SQL Server equivalent — new operational surface for this team).
- **Prior-run artifact:** none cited — no prior-run catalog entry informed a decision in this run.

---

*Confidence: 91 (formula above). Iteration: 0. Generated by porting-agent-wizard run 499021d5; full per-item checklist detail in the agent-state dir's `validation.json`.*