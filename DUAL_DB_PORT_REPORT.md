# Dual-DB Port Report — BobsBookstoreClassic (SQL Server + PostgreSQL)

**Confidence: 70** — `100 * (33 pass / 41 applicable) - 10 * (1 unresolved "no equivalent") - 5 * (0 build warnings touching changed files) = 70.5 → 70`
**Iterations: 0** (single pass; build clean on first validation).

## 1. Summary

Ported the EF6 6.5.1 / .NET Framework 4.8 codebase to run on **both SQL Server and PostgreSQL behind one config switch** (`Data:Provider` = `SqlServer` | `PostgreSql`), zero code change per environment. All data access is LINQ-to-Entities over a single `ApplicationDbContext` (no raw SQL, no stored procs, no Dapper/ADO.NET — grep-verified), so the port is concentrated in provider wiring, EF provider registration, and a provider-forked model. The SQL Server path is byte-identical to the pre-port model (no EF model-hash change → `DropCreateDatabaseIfModelChanges` will not drop existing databases). Engines verified: **neither engine connected** — verification is build-level (MSBuild exit 0); all runtime paths are listed as unverified (§10).

Top risks: (1) `RowVersion` rowversion concurrency token has no PG equivalent — disabled on PG, human decision pending (§8); (2) `Contains()` search becomes case-sensitive on PG (§8); (3) EF6+Npgsql has no built-in execution strategy for PG's `40001`/transient failures (§11).

## 2. File sweep table

| File | Classification | Changes / reason |
|---|---|---|
| `app/Bookstore.Data/ApplicationDbContext.cs` | data-access | Added `DbConnection` ctor; provider-forked `OnModelCreating` (PG: `nvarchar`→varchar for `Customer.Sub`, `Ignore(RowVersion)` ×9 entities; SQL Server branch unchanged) |
| `app/Bookstore.Data/DatabaseProviders.cs` | data-access (NEW) | `DatabaseProvider` enum, `IDatabaseProviderAccessor` (fail-fast, no default), `IDbConnectionFactory` (`SqlConnection` \| `NpgsqlConnection`, asserts conn string non-empty) |
| `app/Bookstore.Data/Bookstore.Data.csproj` | config | + `EntityFramework6.Npgsql` 6.4.3 PackageReference (latest published, NuGet-verified; Npgsql core flows transitively — resolved 4.1.3.0); + Compile entry |
| `app/Bookstore.Data/App.config` | config | + Npgsql EF6 `<provider>` registration (design-time) |
| `app/Bookstore.Web/Web.config` | config | + `Data:Provider=SqlServer`; + `AppDb_PostgreSql` placeholder conn string; + Npgsql `<provider>`; + `<system.data><DbProviderFactories>` Npgsql entry |
| `app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs` | data-access | DbContext now registered from per-request `DbConnection` via `IDbConnectionFactory` (was connection-string param) |
| `app/Bookstore.Data/BookstoreDbInitializer.cs` | data-access | Unaffected — `DropCreateDatabaseIfModelChanges` + `Seed` are provider-neutral; EF generates PG schema itself |
| `app/Bookstore.Data/BookstoreConfiguration.cs` | data-access | Unaffected — provider-agnostic settings/connection-string dictionary; reused by the accessor |
| `app/Bookstore.Data/Repositories/*.cs` (7) | data-access | Unaffected — pure LINQ over `DbSet`s; no provider-divergent constructs |
| `app/Bookstore.Data/PaginatedList.cs` | data-access | Unaffected — `OrderBy(x => x.Id)` before `Skip/Take` (A2 satisfied on both engines) |
| `app/Bookstore.Web/App_Start/ConfigurationSetup.cs` | data-access | Unaffected — SSM parameter fetch is provider-agnostic |
| `app/Bookstore.Cdk/DatabaseStack.cs` | infrastructure | Unaffected — deploys RDS SQL Server 2017; PG needs its own instance (§11) |
| `db-scripts/bobs-used-bookstore-classic-db.sql` | schema (UTF-16) | Unaffected — referenced by no code; conversion notes in §11 if ever run manually |
| `app/Bookstore.Web/Web.Debug/Release.config` | config | Unaffected — template transforms, no conn strings |
| all other `*.cs` (Domain, Web controllers/helpers, Cdk, Common) | unaffected | Zero data-access signature hits (§4.2 grep) |

## 3. Inventory of SQL Server–specific constructs

| File:line | Construct | Risk class | Resolution |
|---|---|---|---|
| `ApplicationDbContext.cs:40` (pre-port) | `HasColumnType("nvarchar")` | Fork | PG branch: `HasMaxLength(450)` only → `varchar(450)`; SQL Server branch unchanged |
| `Entity.cs:16-17` | `[Timestamp] byte[] RowVersion` (all 9 tables, SQL Server `timestamp`=rowversion) | **No equivalent** | R9 flag §8; PG branch ignores it (§8) |
| `Web.config:12` | `MultipleActiveResultSets=true` | No equivalent | No Npgsql equivalent; EF6 issues no multi-active-result queries — inert here; left in the SQL Server string only |
| `Web.config:12` | `Integrated Security=SSPI` (LocalDB) | Infra | PG string uses SCRAM user/password; rollout note §11 |
| `db-scripts/*.sql` | T-SQL DDL/`GO`/`IDENTITY`/`N''`/`timestamp` | No equivalent (script) | Not ported (unused by code); EF generates PG schema; notes §11 |
| Repositories `Contains(...)` filters | EF → `LIKE` | Fork (behavior) | Case-insensitive on SQL Server collation, case-sensitive on PG — §8 flag, deliberately not rewritten (R1/R2) |
| `OrderRepository.cs:45-53` | `GroupBy` + `FirstOrDefault().Book` projection | Verify | EF6 translation must be confirmed on PG at runtime (§10) |

## 4. Config changes

- Switch: `appSettings/Data:Provider` = `SqlServer` (default → current behavior) | `PostgreSql`. Invalid/missing → `InvalidOperationException` naming the key and allowed values (§2 fail-fast, no default engine).
- `BookstoreDatabaseConnection` (SQL Server): **unchanged** (`Server=(localdb)\MSSQLLocalDB;...`).
- `AppDb_PostgreSql` (placeholder, R5 — no secrets): `Host=localhost;Port=5432;Database=BookStoreClassic;Username=bookstore;Password=[REPLACE_ME];Search Path=public`.
- Startup validation: `DatabaseConnectionFactory.CreateConnection` asserts the selected provider's connection string exists and is non-empty before first use.
- `Data:Provider` is env-var overridable via the existing `BookstoreConfiguration` mechanism (web.config key present → env override applies).

## 5. Seams introduced

| Type | Responsibility | Registration |
|---|---|---|
| `DatabaseProvider` (enum) | typed engine value | — |
| `IDatabaseProviderAccessor` / `DatabaseProviderAccessor` | resolves `Data:Provider` once at startup (Lazy singleton), fail-fast | `builder.RegisterInstance` in `DependencyInjectionSetup` |
| `IDbConnectionFactory` / `DatabaseConnectionFactory` | returns `DbConnection` for the resolved provider | `InstancePerRequest` in `DependencyInjectionSetup` |

## 6. SQL changes

None required at the statement level — the codebase contains **zero hand-written SQL** (no `ExecuteSqlCommand`, `Database.SqlQuery`, Dapper, `SqlFunctions`, stored procs — grep-verified). All SQL is EF-generated and parameterized (R6 vacuously satisfied). No dialect forks in query code; the single fork is in model configuration (§3).

## 7. Type mapping applied

Handled by EF6 per provider; physical mapping is generated per engine (schema is initializer-created, no migration files exist). Explicit mappings: `nvarchar(450)`→`varchar(450)` (PG branch only). No lossy mappings introduced. Verification flag: `decimal` (`Book.Price`, `Offer.BookPrice` = `decimal(18,2)` on SQL Server) — confirm Npgsql 4.1.3's EF6 default numeric precision on PG (§10); no code asserts a specific store precision.

## 8. R9 flags — decisions needed (primary client-facing section)

1. **`RowVersion` optimistic-concurrency token** (`Entity.RowVersion`, `[Timestamp] byte[]` → SQL Server `rowversion`) — PostgreSQL has no equivalent store-generated version type. Current port: PG model **ignores** it → PG has **no lost-update detection**. Options: (a) map to PG's `xmin` system column as the concurrency token (no schema change; verify EF6-Npgsql support at 4.1.3); (b) add an explicit `int`/`bigint` version column maintained by both engines (schema change on SQL Server — conflicts with `allow_schema_changes=false`); (c) use `UpdatedOn` `timestamptz` as a weaker last-modified token (weaker, schema change). **Do not ship PG to concurrent multi-user write traffic before deciding.**
2. **Case-insensitive search** — `Contains(...)` filters compile to `LIKE`: CI on SQL Server (collation), **CS on PG** → user-facing search behavior changes. Options: (a) accept CS search on PG; (b) `lower(x).Contains(lower(term))` on both engines (R1 cost: prevents index use on SQL Server); (c) dialect-routed `ILIKE` only on PG (fork in repository filter code).
3. **Text sort order** — `ORDER BY Name` uses SQL Server collation vs PG ICU/locale ordering; display ordering differs for mixed case/accents. Options: pin ICU collation, add explicit sort key, or accept.
4. **EF6 retry/transient-failure strategy** — SQL Server side historically uses `SqlAzureExecutionStrategy`; EF6+Npgsql has no built-in equivalent, so PG gets no automatic retry (incl. `40001`). Options: custom `IDbExecutionStrategy` per provider, or application-level retry.
5. **`decimal` store precision on PG** — verify Npgsql 4.1.3 EF6 default (`numeric` precision) against SQL Server's `decimal(18,2)`; add explicit `HasPrecision(18,2)` if divergence is observed (would alter the model — validate no unwanted initializer DB drop).

## 9. Reserved-word collisions

None — all identifiers are EF-generated/quoted (PascalCase, quoted under `preserve` style); no hand-written SQL with unquoted identifiers. (`Order`/`User`-style names are always quoted by EF6.)

## 10. Verification

- `MSBuild.exe app/Bookstore.Web/Bookstore.Web.csproj -t:Restore -p:RestorePackagesConfig=true -p:SolutionDir=<repo>\` → restored (EntityFramework6.Npgsql 6.4.3 + Npgsql 4.1.3.0 resolved into both `Bookstore.Data` and `Bookstore.Web` outputs).
- `MSBuild.exe ... -p:Configuration=Debug` → **exit 0**, twice (incl. after final edits). Only pre-existing warnings: Magick.NET version-conflict/advisory warnings (untouched area).
- **Unverified (no engine available this run)**: PG connection-string parsing (`Search Path` keyword at Npgsql 4.1.3 — verify; use `SearchPath=` if rejected), Npgsql provider-services resolution at runtime, `DbProviderFactories` partial-assembly-name entry, PG model creation + `Seed` via initializer, all repository queries on PG (esp. `OrderRepository.ListBestSellingBooksAsync` GroupBy+`FirstOrDefault` translation), `ExecuteScalar`-free mapping (n/a), concurrency behavior after §8 item 1 decision, SQL Server runtime after DI change (connection-based; behavior-preserving by design but not executed).
- No test project exists in the repository; nothing was exercised on either engine (R11 gap, explicit).

## 11. Rollout prerequisites

- **PG version: UNVERIFIED DEFAULT 16** — no docker-compose/IaC/README/CI states a PG target anywhere in the repo (CDK deploys RDS SQL Server 2017). No version-gated construct (MERGE, JSON funcs, computed columns, INCLUDE indexes) is used, so the default has no effect on written code — but pin the real target before deploying.
- PostgreSQL instance reachable from the app (PG mode): current CDK provides SQL Server only; `DatabaseStack` extension or an external PG (e.g. RDS PG/Aurora) required.
- Create the PG database (e.g. `BookStoreClassic`); schema + seed are created by `DropCreateDatabaseIfModelChanges` on first run (dev posture, pre-existing).
- Auth model: SCRAM user/password for PG (Windows auth has no PG analog); set real credentials in `AppDb_PostgreSql`/SSM — no secrets in source (R5).
- `Search Path=public` is set in the PG connection string (matches default `pg_schema`; adjust for a non-public schema).
- MARS: PG needs no equivalent; no pooling configuration required beyond Npgsql defaults. If PgBouncer in transaction mode is introduced later: configure `max_prepared_statements` (PgBouncer 1.21+) — EF6+Npgsql uses server-side prepared statements.
- No sequence re-sync needed (identities are per-engine, EF-managed).
- If deploying via ECS/Windows containers, `Npgsql.dll` + `EntityFramework6.Npgsql.dll` must flow to the publish output (verified present in local `bin`).
- SQL script `db-scripts/bobs-used-bookstore-classic-db.sql` (if ever needed on PG): `IDENTITY(1,1)`→`GENERATED BY DEFAULT AS IDENTITY`, `SET IDENTITY_INSERT`→`OVERRIDING SYSTEM VALUE`, strip `N''`, split `GO`, `[RowVersion] timestamp` has no direct equivalent (§8 item 1), `[dbo]`→schema choice, `READ_COMMITTED_SNAPSHOT`/`ALLOW_SNAPSHOT_ISOLATION` settings are PG-default MVCC behavior.
