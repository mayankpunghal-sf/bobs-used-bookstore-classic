# DUAL_DB_PORT_REPORT.md — Dual-Target Data Access Port (SQL Server + PostgreSQL)

**Repo:** BobsBookstoreClassic (ASP.NET MVC 5, EF6 Code-First, .NET Framework 4.8)
**Switch:** `Data:Provider` appSetting → `SqlServer` | `PostgreSql`
**Run:** porting-agent-wizard × dual-db-port · **Iterations:** 1 · **Confidence: 80/100**

---

## 1. Summary

- **Scope ported:** `Bookstore.Data` (EF6 context, repositories, configuration accessor) and `Bookstore.Web` (DI wiring, config). SQL Server path is behaviorally unchanged: same connection string name, same provider registration, same model, same SQL stream (the ILIKE interceptor is registered only on the PostgreSQL path).
- **Engines verified:** neither engine was connected in this environment (no database servers available; see §10). Build-verified on .NET Framework 4.8 via MSBuild; runtime behavior on both engines is the explicit verification gap (R11).
- **Top risks:** (a) `rowversion` concurrency token has no PG equivalent — dropped from the PG model, flagged §8; (b) PostgreSQL string equality is case-sensitive (B6) — accepted for system-generated identifiers, flagged §8; (c) EF6 LINQ translations (`GroupBy`→`FirstOrDefault`, database initializer DDL generation) are exercised only against SQL Server today.

Success criterion per contract: set `Data:Provider=PostgreSql` + fill `BookstoreDatabaseConnection_PostgreSql` (or the reverse) — zero code change, zero recompile. Both paths build from one codebase and one `ApplicationDbContext`.

## 2. File sweep table

Queue built from §4.1 (`*.cs`, `*.sql`, `*.config`, `appsettings*.json`, `*.edmx`, `*.hbm.xml`, `*.csproj`, migration folders, DDL/seed). No `appsettings*.json`, `.edmx`, `.hbm.xml`, or EF `Migrations/` folders exist in the repo.

| File(s) | Classification | Changes applied / reason |
|---|---|---|
| `app/Bookstore.Data/ApplicationDbContext.cs` | data-access | Per-provider model fork in `OnModelCreating` (see §6/§7); context ctor unchanged (receives connection-string name) |
| `app/Bookstore.Data/BookstoreConfiguration.cs` | data-access (config) | Unchanged — existing env-var-overridable settings dictionary is reused by the new provider accessor |
| `app/Bookstore.Data/BookstoreDbInitializer.cs` | data-access | Unchanged — `DropCreateDatabaseIfModelChanges` + seed works per provider (rollout notes §11) |
| `app/Bookstore.Data/PaginatedList.cs` | data-access | Unchanged — paging via `OrderBy(Id).Skip/Take` (portable; §5.3 n/a) |
| `app/Bookstore.Data/Repositories/*.cs` (7 files) | data-access | Unchanged — pure EF6 LINQ; no dialect-specific constructs found in sweep |
| `app/Bookstore.Data/Database/DatabaseProvider.cs` | data-access (new) | Provider enum seam (§5) |
| `app/Bookstore.Data/Database/DatabaseProviderAccessor.cs` | data-access (new) | Startup-resolved provider + fail-fast validation + interceptor registration (§5) |
| `app/Bookstore.Data/Database/NpgsqlLikeInterceptor.cs` | data-access (new) | `IDbCommandInterceptor` — `LIKE`→`ILIKE` on PG path (B6/B7) |
| `app/Bookstore.Data/Bookstore.Data.csproj` | config (project) | +3 `<Compile>` entries for new files; no new packages needed in Data |
| `app/Bookstore.Data/App.config` | config | + Npgsql EF6 provider + `DbProviderFactories` registration |
| `app/Bookstore.Web/Web.config` | config | + `Data:Provider` switch, + PG connection string, + Npgsql EF6 provider, + `DbProviderFactories`, (binding redirects: none required — verified by clean build) |
| `app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs` | data-access | Provider resolved once at startup; context built from connection-string **name** (§4) |
| `app/Bookstore.Web/App_Start/ConfigurationSetup.cs` | data-access (config) | Unchanged — note: SSM value lands in appSettings under `ConnectionStrings/...` and is not consumed (pre-existing, see §8 note 6) |
| `app/Bookstore.Web/App_Start/{Authentication,Bundle,Filter,Logging,Route}Setup.cs` | unaffected | No data access (grep: no EF/ADO/SQL markers) |
| `app/Bookstore.Web/Startup.cs`, `Global.asax.cs` | unaffected | OWIN/MVC wiring only |
| `app/Bookstore.Web/Controllers/*.cs` (9), `Areas/Admin/Controllers/*.cs` (7) | unaffected | Call domain services only; no data-access markers |
| `app/Bookstore.Web/Models/**, Areas/Admin/Models/**` (25 .cs) | unaffected | View models only |
| `app/Bookstore.Web/Helpers/*.cs` (10) | unaffected | MVC/OWIN helpers; no data access |
| `app/Bookstore.Domain/**` (42 .cs) | unaffected (entities/services) | Entity classes + services over repository interfaces; grep verified: no EF, no ADO.NET, no SQL. One `DateTime.Now` default (§7/§8 B18 note) |
| `app/Bookstore.Common/Constants.cs` | unaffected | App name constant |
| `app/Bookstore.Cdk/*.cs` | unaffected | CDK infrastructure; RDS engine provisioning (SQL Server) noted in §11 as infra follow-up |
| `app/Bookstore.Web/packages.config`, `Bookstore.Web.csproj` | config (packages) | + `EntityFramework6.Npgsql 6.4.3` + `Npgsql 4.1.3` entries and References |
| `BobsBookstoreClassic.sln`, other `*.csproj` (Domain, Common, Cdk) | unaffected | No changes required |
| `app/Bookstore.Web/Web.Debug.config`, `Web.Release.config` | unaffected | Template transforms only (verified: no connectionStrings entries) |
| `app/Bookstore.Web/Views/Web.config`, `Areas/Admin/Views/web.config` | unaffected | Razor view engine config |
| `app/Bookstore.Web/Web.config` views section, `*.cshtml` (~60) | unaffected | Presentation only |
| `db-scripts/bobs-used-bookstore-classic-db.sql` | schema (SQL Server only) | Unchanged — T-SQL DDL + seed for SQL Server provisioning; PostgreSQL schema is generated by the EF6 database initializer (§11). Per §7, one universal script is an anti-pattern; per-provider provisioning is documented instead |
| `config-scripts/Update-ConnectionString.ps1` | config tooling | Unchanged — rewrites the SQL Server connection string from RDS secret; a PG runbook equivalent is a rollout item (§11) |
| `Dockerfile` (root), `app/Bookstore.Web/Dockerfile`, `cdk.json`, `LogMonitorConfig.json`, `.dockerignore`, `.git*`, docs | unaffected | Container/CI/docs; note: `app/Bookstore.Web/Dockerfile` targets .NET 8 Linux and cannot build this net48 MVC5 app (pre-existing stray file, untouched) |

## 3. Inventory (construct → risk class)

| Location | Construct | Risk class |
|---|---|---|
| `Bookstore.Data/ApplicationDbContext.cs:16` | `DbContext(string)` receives connection-string name; EF6 resolves engine from the entry's `providerName` | Portable |
| `ApplicationDbContext.cs:40-65` | `OnModelCreating` per-provider model fork (nvarchar vs varchar; rowversion ignore) | Fork |
| `Bookstore.Domain/Entity.cs:16-17` | `[Timestamp] byte[] RowVersion` on every entity | **No equivalent** (§8 flag 1) |
| `Bookstore.Data/Database/NpgsqlLikeInterceptor.cs` | `LIKE`→`ILIKE` command-text rewrite (B6/B7) | Fork |
| `Bookstore.Data/Database/DatabaseProviderAccessor.cs` | Startup provider resolution, fail-fast, connection-string selection (R7) | Portable (seam) |
| `Bookstore.Web/App_Start/DependencyInjectionSetup.cs:45-49` | Provider resolved once; context per request from name | Portable |
| Repositories `Contains(...)` (BookRepository:37,42,89-93; OfferRepository:52,57) | LINQ `Contains` → `LIKE` (CI on SS, CS on PG) | Fork (interceptor) |
| CustomerRepository:28 / AddressRepository:20,29,34 / ShoppingCartRepository:27 / OrderRepository:42 | String `=` comparisons (`Sub`, `CorrelationId`) — CS on PG (B6) | Accepted divergence (§8 flag 2) |
| BookRepository:99,111 | `OrderBy(x => x.Name)` — nullable column, no explicit NULLS ordering (B9) | Accepted divergence (minor) |
| Bookstore.Domain/Orders/Order.cs:30 | `DateTime.Now.AddDays(7)` default (Kind=Local → `timestamp`) | Accepted (documented, B18) |
| OrderRepository:45-53 | `GroupBy(...).OrderByDescending(Count()).Select(FirstOrDefault().Book)` — provider translation risk on PG | Portable (unverified at runtime — §10) |
| `Web.config` MARS=true | `MultipleActiveResultSets` (SQL Server only) | N/A — no concurrent-reader code found (§5.8 grep evidence); string kept per R1 |
| No occurrences repo-wide (grep evidence) | ADO.NET types, `SqlException`, `ExecuteScalar`, transactions/`TransactionScope`, `NOLOCK`/`READ UNCOMMITTED`, TVPs, bulk paths, Guid keys, raw SQL strings, `.edmx`/NHibernate | N/A |

## 4. Config changes

- **Switch:** `<add key="Data:Provider" value="SqlServer" />` in `Web.config` appSettings. Missing/unknown value → `InvalidOperationException` naming the key and allowed values (`SqlServer`, `PostgreSql`); **no default engine** is assumed by the accessor (the config ships with `SqlServer` so existing deployments are byte-identical in behavior).
- **Connection strings (side by side, placeholders only — no secrets):**
  - `BookstoreDatabaseConnection` — `providerName="System.Data.SqlClient"` — unchanged (kept name preserves the CDK SSM parameter contract `/{AppName}/Database/ConnectionStrings/BookstoreDatabaseConnection` and `Update-ConnectionString.ps1`).
  - `BookstoreDatabaseConnection_PostgreSql` — `providerName="Npgsql"` — `Host=localhost;Port=5432;Database=BookStoreClassic;Username=bookstore;Password=[PLACEHOLDER];Search Path=public;` (Npgsql keyword mapping per §6: `Host`, `Port`, `Database`, `Username`, `Search Path`; pooling via `Maximum/Minimum Pool Size` when needed).
- **Provider registration:** `<provider invariantName="Npgsql" type="Npgsql.NpgsqlServices, EntityFramework6.Npgsql" />` added to `<entityFramework><providers>` (SQL Server entry untouched); Npgsql `DbProviderFactories` entry added with `<remove invariant="Npgsql" />` guard. Mirrored in `Bookstore.Data/App.config`.
- **Startup validation:** `DatabaseProviderAccessor.Configure()` asserts the selected provider's connection string exists and is non-empty, failing fast with the entry name in the message.
- **Packages:** `EntityFramework6.Npgsql 6.4.3` (latest stable; net461 target) + `Npgsql 4.1.3` (the version line the EF6 provider is built and declared against). Runtime dependencies of Npgsql 4.1.3 on net461 (System.Memory, Unsafe, Text.Json, Tasks.Extensions, ValueTuple) are satisfied by existing pinned versions.

## 5. Seams introduced

| Type | Responsibility | Registration site |
|---|---|---|
| `DatabaseProvider` (enum) | The two supported engines | `Bookstore.Data/Database/DatabaseProvider.cs` |
| `DatabaseProviderAccessor` (static) | Resolves provider exactly once (Lazy) from `Data:Provider`; fail-fast on missing/invalid; maps provider → connection-string name; validates strings at startup; registers PG interceptor | Called from `DependencyInjectionSetup.ConfigureDependencyInjection` (single startup call) |
| `NpgsqlLikeInterceptor` (`IDbCommandInterceptor`) | Preserves SQL Server's case-insensitive `LIKE` semantics on PostgreSQL | `DbInterception.Add` — only when provider is PostgreSql |

No `IDbConnectionFactory`/`ISqlQueryProvider`/`IBulkInserter` seams were required: there is no manual connection creation, no raw SQL, and no bulk path in this codebase (grep-verified). The EF6 provider selection via connection-string `providerName` plays the factory/dialect role (one context, per-provider compiled model).

## 6. SQL changes

No hand-written SQL exists (Appendix A sweep found zero matches in `*.cs`; the only `.sql` file is the SQL Server DDL dump). All statements remain EF6 LINQ-generated and fully parameterized (`@name` placeholders — portable per §5.3; no reused-name or prefix-collision patterns found; no parameter-count-risky `IN` expansions). Dialect forks applied at the mechanism level instead of per statement:

| Mechanism | Driving entry | Where |
|---|---|---|
| Provider registration (config, not code) | §2 seam table | `Web.config`, `App.config` |
| `LIKE` → `ILIKE` | A42/B6/B7 | `NpgsqlLikeInterceptor` (PG path only) |
| `nvarchar` column type → `character varying` | Appendix C | `OnModelCreating` PG branch |
| `rowversion` dropped from PG model | §5.11 | `OnModelCreating` PG branch |

## 7. Type mapping applied (physical, per engine)

| SQL Server (current) | PostgreSQL (EF6 Npgsql-generated) | .NET | Note |
|---|---|---|---|
| `int IDENTITY(1,1)` | `integer` + identity | `int` | Provider-managed; sequence re-sync n/a for EF-created schema |
| `nvarchar(max)` | `text` | `string` | Appendix C standard |
| `nvarchar(450)` (Customer.Sub) | `character varying(450)` (explicit fork) | `string` | Deviation: forked; SS side unchanged |
| `datetime` | `timestamp` | `DateTime` | B18/B19: PG µs precision ≥ SS 3.33 ms; `DateTime.Now` default documented |
| `decimal(18,2)` | `numeric(18,2)` | `decimal` | Exact |
| `bit` | `boolean` | `bool` | B10 — EF binds bool natively |
| `rowversion` (`RowVersion`) | **dropped** (column not created) | `byte[]` | **Lossy mapping — flagged (§8.1)**; SS side keeps `timestamp` token unchanged |
| `varbinary(max)` (`__MigrationHistory.Model`) | `bytea` | — | EF-managed |

No `uniqueidentifier`, `xml`, `hierarchyid`, `geography`, `sql_variant`, or TVP columns exist in the model (DDL dump verified).

## 8. R9 flags (client decisions outstanding)

1. **`rowversion` concurrency token (all tables).** PostgreSQL has no rowversion. Options: (a) *current choice* — drop the token from the PG model (no optimistic concurrency on PG; last-write-wins); (b) add an `int`/`bigint` version column maintained by both engines (requires `allow_schema_changes=true` and an SS-side column — rejected under schema freeze); (c) map to PG `xmin` (EF6 has no built-in support; custom interceptor/rewrite, higher maintenance). Risk: lost-update window on PG only.
2. **Case-sensitive string equality on PG (B6).** `Sub`/`CorrelationId` comparisons are CI on SS, CS on PG. Values are system-generated (Cognito subs / GUIDs) with no case variance; accepted. If user-typed strings ever become lookup keys, apply `citext`, `lower()` normalization, or an ICU CI collation. The search path (`Contains`) *is* handled via the ILIKE interceptor.
3. **`ORDER BY` on nullable `Name` (B9).** SQL Server sorts NULLs first, PG last (EF6 emits no explicit NULLS ordering). Only affects rows with NULL `Name`; fix would require a per-provider query translation seam — flagged, not changed (R2).
4. **Guid ordering (§5.10), bulk/COPY (B27), MARS (§5.8), TVPs (B28), isolation retry (§5.6):** not applicable — grep-verified absence; no flags required.
5. **Named connection strings deviate from §0 defaults** (`AppDb_SqlServer`/`AppDb_PostgreSql`): kept `BookstoreDatabaseConnection` (+`_PostgreSql` suffix) because the name is an external contract (CDK SSM parameter, deploy script). Renaming would break deployed infrastructure — R1 outranked the input default.
6. **Pre-existing, not touched (R2/R10):** `ConfigurationSetup.cs` stores the SSM database parameter into appSettings under `ConnectionStrings/BookstoreDatabaseConnection` where nothing reads it (AWS deploys rely on `Update-ConnectionString.ps1` rewriting Web.config instead); `BookRepository.cs:111` `query.OrderBy(x => x.Name)` result unassigned (dead code); stray Linux .NET 8 `app/Bookstore.Web/Dockerfile` that cannot build this project; NU1902/NU1903 vulnerability warnings and MSB3243 Magick.NET reference/PackageReference version conflict (pre-existing build warnings); CDK project targets EOL net6.0.

## 9. Reserved words and identifier style

`identifier_style=preserve`: EF6 quotes every identifier on both engines (`[Order]` / `"Order"`), so no unquoted identifier ever reaches PG. Tables named `Order` (reserved on PG) and columns `State`, `Year`, `Text` are safe because quoting is unconditional; zero manual SQL exists to regress this (grep-verified). No PL/pgSQL functions were ported, so §8.2 `par_`/`var_` prefixes are n/a. Identifier length: longest name is `__MigrationHistory` (18 bytes) — far under the 63-byte PG limit (B29).

## 10. Verification

- **Commands:** `MSBuild.exe BobsBookstoreClassic.sln /t:Restore /p:RestorePackagesConfig=true /p:NuGetAudit=false` (exit 0) and `MSBuild.exe BobsBookstoreClassic.sln /p:Configuration=Debug /v:minimal /p:NuGetAudit=false` (exit 0). Toolchain: VS 18 Community MSBuild, .NET Framework 4.8 targeting pack, dotnet SDK 10 (CDK project).
- **Results:** build succeeds; `bin` contains `EntityFramework6.Npgsql.dll` (195,072 B) and `Npgsql.dll` (770,560 B); both edited config files parse as well-formed XML; grep confirms no ADO.NET instantiation, no provider conditionals outside the two sanctioned startup/model-build sites.
- **Unverified paths (R11 statement):** no SQL Server or PostgreSQL server was reachable from this environment. Not exercised at runtime: PG schema creation by `DropCreateDatabaseIfModelChanges`, seed inserts with explicit ids under PG identity, ILIKE interceptor behavior, `GroupBy`→`FirstOrDefault` translation on PG, end-to-end page flows on either engine. There are no test projects in the solution (no gap to report beyond the above). Confidence is capped accordingly.

## 11. Rollout prerequisites (PostgreSQL environment)

1. **PG version:** ≥ 12 recommended (target 16 per inputs; Npgsql 4.1.3 supports 9.0+, PG 16 compatibility expected but unverified here).
2. **Database creation:** the initializer (`DropCreateDatabaseIfModelChanges`) creates the database and schema — the login needs `CREATEDB`. Pre-provisioning via a hand-written PG DDL set is the alternative if the login cannot have `CREATEDB`.
3. **Seed/identity check on first run:** the initializer seeds reference data with explicit ids (1-24) that FKs reference; verify `ReferenceData` ids match after first PG start (same identity semantics as SQL Server, but confirm once).
4. **Auth:** SQL Server Windows auth (`Integrated Security=SSPI`) has no Npgsql equivalent — use SCRAM username/password (or Kerberos/GSSAPI) in `BookstoreDatabaseConnection_PostgreSql`.
5. **`Search Path=public`** is set in the connection string (pooled sessions reset session-level `search_path`).
6. **Pooling:** map `Max/Min Pool Size` → `Maximum/Minimum Pool Size` if tuned on the SQL Server string.
7. **Retry policy:** no RR/SERIALIZABLE usage exists (default READ COMMITTED both engines); no `40001` retry loop required. `EnableRetryOnFailure`-style strategies are not configured on the current EF6 stack.
8. **Infra follow-up (out of code scope):** CDK `DatabaseStack` provisions SQL Server Express RDS; a parallel PG engine option (and a `Update-ConnectionString.ps1` counterpart for PG) is needed for AWS deployments of the PG path.
9. **MARS:** the SQL Server string keeps `MultipleActiveResultSets=true` (R1). The PG string omits it (no equivalent; none needed — no concurrent-reader code exists).

---

## Confidence score

**80 / 100** = `100 × (28 applicable checklist items passed / 28 applicable) − 10 × (2 unresolved "no equivalent" flags: rowversion token, PG CI-equality policy) − 5 × (0 build warnings touching changed files)`, floored at 0. Checklist items marked N/A (raw SQL, ADO.NET types, ExecuteScalar, transactions, NOLOCK, MARS, bulk, TVPs, PL/pgSQL, migrations-assemblies) were excluded from the applicable denominator after grep/read evidence; "tests pass" is replaced by the documented R11 gap (no test projects; engines not connected). Iterations used: **1** of 3.
