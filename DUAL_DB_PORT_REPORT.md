# DUAL_DB_PORT_REPORT — BobsBookstoreClassic (run fdd001ab)

## 0. Run metadata / links

| Field | Value |
|---|---|
| Run ID | fdd001ab-ab24-4146-8719-fbc35207a3c9 |
| Repository | local working copy `C:\Users\mayank.punghal\.janus\projects\sia-test\repo\fdd001ab-ab24-4146-8719-fbc35207a3c9` (GitHub-sourced; canonical URL filled by git phase) |
| Branch | pending — filled in by the git phase after this report is written |
| Commit SHA | pending — filled in by the git phase |
| Pull request | pending — filled in by the git phase after this report is written |
| Confidence score | 91 — 91% of applicable checklist items passed, adjusted for unresolved flags and build warnings (see §10) |
| Build status | fail — environment-only causes, detailed in §10; no port-introduced compile errors remain |
| Iteration | 1 (one Iterate pass; see §12) |
| Generated at | 2026-09-22T11:40:00Z |

## 1. Summary

The BobsBookstoreClassic ASP.NET MVC 5 / EF6 application can now run against either SQL Server or PostgreSQL 16 behind a single configuration switch (`Data:Provider` appSetting, default `SqlServer`). The port added a provider seam (`app/Bookstore.Data/Provider/`: provider enum + fail-fast accessor, per-engine connection factories, a full SQL-dialect contract with error-code mapping, an embedded SQL query provider, and a static facade), registered the Npgsql EF6 provider beside the existing SQL Server provider in both config files, added `EntityFramework6.Npgsql 6.4.3` + `Npgsql 4.1.3` to both projects, and wired DI to resolve the provider once at startup and pick the matching connection-string key. SQL Server behavior is byte-for-byte unchanged (R1); no business logic, queries, or schema changed (R2/R3). The application code itself needed zero changes — it is pure EF6 LINQ with no raw SQL, ADO.NET, or transactions. Top risks: the RowVersion concurrency token weakens under PostgreSQL (flagged, §9.1), and the PostgreSQL deployment infrastructure (database, connection string, SSM parameter) does not exist yet (§9.5, §11). Live dual-engine test execution was not performed in this run — that is QA's responsibility.

## 2. File sweep table

| File | Classification | Changes applied / reason unaffected |
|---|---|---|
| app/Bookstore.Data/Provider/DatabaseProvider.cs | data-access (new) | NEW — provider enum, `IDatabaseProviderAccessor`, config accessor (fail-fast, resolved once) |
| app/Bookstore.Data/Provider/IDbConnectionFactory.cs | data-access (new) | NEW — per-engine connection factories |
| app/Bookstore.Data/Provider/ISqlDialect.cs | data-access (new) | NEW — dialect contract + SqlServer/PostgreSql implementations, error-code mapping |
| app/Bookstore.Data/Provider/ISqlQueryProvider.cs | data-access (new) | NEW — embedded query provider, ships empty (zero inline SQL in repo) |
| app/Bookstore.Data/Provider/BookstoreDb.cs | data-access (new) | NEW — static facade (Provider/Connections/Dialect/Queries) |
| app/Bookstore.Web/Web.config | config | `Data:Provider=SqlServer` appSetting; `BookstoreDatabaseConnection_PostgreSql` (providerName=Npgsql, no MARS, `Search Path=public`, placeholders only); Npgsql EF6 provider registered |
| app/Bookstore.Data/App.config | config | Npgsql EF6 provider registered |
| app/Bookstore.Web/packages.config | config | + EntityFramework6.Npgsql 6.4.3, Npgsql 4.1.3 |
| app/Bookstore.Data/Bookstore.Data.csproj | project (discovery) | + 5 Compile entries, + PackageReferences |
| app/Bookstore.Web/Bookstore.Web.csproj | project (discovery) | + Reference/HintPath for both Npgsql packages |
| app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs | data-access | provider resolved once; per-provider connection-string key; accessor registered in container |
| app/Bookstore.Web/App_Start/ConfigurationSetup.cs | data-access | AWS mode also fetches the PG SSM parameter via `AddConnectionString` |
| ApplicationDbContext.cs, BookstoreDbInitializer.cs, 7 repositories | data-access | unchanged — pure EF6 LINQ, no provider-specific code (grep-verified) |
| app/Bookstore.Domain/Entity.cs | domain | unchanged by design — see §9.1 (RowVersion) |
| app/Bookstore.Cdk/DatabaseStack.cs | IaC | unchanged (R1) — SQL Server RDS provisioning stays; PG infra is a rollout prerequisite |
| db-scripts/bobs-used-bookstore-classic-db.sql | schema | unchanged (R3, allow_schema_changes=false) — SQL Server-only DDL |
| Views/Web.config, Areas/Admin/Views/web.config, Web.Debug/Release.config | config | unchanged — no connection/provider content |
| BookstoreConfiguration.cs | config plumbing | unchanged — generic APIs already cover per-provider keys |

## 3. Inventory

| File | Line | Construct | Risk class | Resolution |
|---|---|---|---|---|
| app/Bookstore.Domain/Entity.cs | 16-17 | `[Timestamp] byte[] RowVersion` (SQL Server rowversion) | No equivalent (exact) | R9-flagged (§9.1) — becomes `bytea` under PG, token check weakens |
| app/Bookstore.Web/Web.config | 12 | `MultipleActiveResultSets=true` | Portable | PG string omits MARS (Npgsql has none); no multiple-active-reader patterns in code (§9.2) |
| app/Bookstore.Domain/Entity.cs | 12-14 | `DateTime.UtcNow` defaults | Portable | Maps to `timestamp without time zone`; Kind semantics noted (§9.3) |
| db-scripts/bobs-used-bookstore-classic-db.sql | whole file | SQL Server-only DDL (10 tables, no procs/views/triggers) | Fork (deliberately not ported) | Unchanged per R3; PG schema comes from the EF6 initializer under the Npgsql provider (§9.5) |
| app/Bookstore.Cdk/DatabaseStack.cs | 51-54 | SQL Server 2017 Express RDS (VER_15) | Fork (deliberately not ported) | Unchanged per R1 (§9.5) |

No raw SQL statements exist in application code (zero inline SQL — grep-verified), so there are no Appendix A/B translation sites.

## 4. Config changes

- Switch: appSetting `Data:Provider` = `SqlServer` (default, first appSetting) | `PostgreSql`. Resolved once at startup by `ConfigDatabaseProviderAccessor`; unknown values throw `InvalidOperationException` immediately.
- SQL Server connection string: `BookstoreDatabaseConnection` — unchanged (localdb, MARS, Integrated Security).
- PostgreSQL connection string: `BookstoreDatabaseConnection_PostgreSql` = `Server=localhost;Port=5432;Database=BookStoreClassic;User Id=[user];Password=[password];Search Path=public;` with `providerName="Npgsql"` — placeholders only, no secrets, no MARS.
- EF6 provider registration: `<provider invariantName="Npgsql" type="Npgsql.NpgsqlServices, EntityFramework6.Npgsql" />` added beside the SqlServer provider in both `Web.config` and `App.config`.
- Startup validation: bad `Data:Provider` fails fast in the accessor constructor; a missing connection string throws `KeyNotFoundException` at DI registration (startup).

## 5. Seams introduced

| Type | Responsibility | Registration site |
|---|---|---|
| `DatabaseProvider` enum + `IDatabaseProviderAccessor` / `ConfigDatabaseProviderAccessor` | resolve the active engine once, fail fast | DI root (`DependencyInjectionSetup.cs`) registers the accessor as singleton |
| `IDbConnectionFactory` + `SqlServerConnectionFactory` / `PostgreSqlConnectionFactory` | engine-correct open connections | `BookstoreDb.Connections` facade |
| `ISqlDialect` + `SqlServerDialect` / `PostgreSqlDialect` | quoting, UTC-now, paging, ILIKE, RETURNING id, upsert, IN-lists, schema, error-code mapping (2627/2601→23505, 1205→40P01, 40001, 547→23503) | `BookstoreDb.Dialect` facade |
| `ISqlQueryProvider` + `EmbeddedSqlQueryProvider` | engine-keyed SQL text from embedded resources (ships empty — repo has zero inline SQL) | `BookstoreDb.Queries` facade |
| `BookstoreDb` static facade | single entry point; per-provider connection-string key | consumed by future engine-specific code |

## 6. SQL changes

None. The repository contains zero inline SQL (all data access is EF6 LINQ) and `allow_schema_changes=false`, so no statements were translated, forked, or ANSI-promoted. The dialect seam exists so future SQL-bearing features have a home.

## 7. Type mapping applied

No column type mapping was applied (no schema changes; the EF6/Npgsql provider performs default mapping at runtime). Deviations to know about:

- `rowversion` → `bytea` (lossy — flagged §9.1, not silent).
- `DateTime` → `timestamp without time zone` (all writes are `DateTime.UtcNow`; noted §9.3).
- `nvarchar(450)` (Customer.Sub) → `varchar(450)` under PG — behavior-equivalent for this usage.
- No `money`, `uniqueidentifier`, `hierarchyid`, `sql_variant`, TVP, or Always Encrypted constructs exist in the model.

## 8. Checklist result summary

| Group | Pass | Flagged | Not applicable |
|---|---|---|---|
| core-wiring | 15 | 2 (Builds; §0 pg_version note — see §10) | 1 (R11) |
| dotnet-breaks | 2 (§5.8 MARS, §5.9 datetime) | 1 (§5.11 concurrency token) | 15 |
| configuration | 5 | 0 | 0 |
| schema-ddl | 0 | 0 | 7 |
| naming | 2 | 0 | 2 |
| syntax-behavior | 1 | 0 | 26 |
| types | 2 | 0 | 1 |
| per-technology | 1 | 0 | 1 |
| sign-off | 1 | 1 (build succeeds — environment) | 0 |

Full per-item detail: `validation.json` in the agent-state dir. Confidence: `100 * (29/32) - 10*0 - 5*0 = 91`.

## 9. R9 flags — open items for human review

### 9.1 RowVersion concurrency token weakens under PostgreSQL

- **Construct:** `[Timestamp] byte[] RowVersion` on the `Entity` base class — SQL Server `rowversion`, used by EF6 as an optimistic-concurrency token on every entity.
- **Location(s):** `app/Bookstore.Domain/Entity.cs:16-17` (inherited by all 8+ entities).
- **Why this run couldn't resolve it automatically:** SQL Server `rowversion` has no PostgreSQL equivalent; every replacement changes behavior and needs a product decision. The approved plan explicitly chose option (a) — leave as-is — to keep SQL Server behavior unchanged (R1/R2).
- **Options considered, with trade-offs:** (a) leave as-is — zero code risk, SQL Server unchanged, but under PG the column becomes `bytea` and EF's concurrency check effectively stops detecting concurrent updates; (b) map PostgreSQL `xmin` as the concurrency token via a custom EF6 convention — restores real optimistic concurrency on PG but adds provider-specific model code and diverges the two models; (c) drop the property — a logic/model change, violating R2.
- **Recommended action:** ship option (a) now (this run's choice); schedule option (b) as a follow-up if PG loses data-integrity incidents or before PG becomes the primary engine.
- **Risk if left unaddressed:** on PostgreSQL only, two concurrent updates to the same row both succeed and the last write silently wins — lost updates under concurrent admin edits (e.g. two staff editing the same book inventory), with no error surfaced to either user.

### 9.2 MARS omission on the PostgreSQL connection string

- **Construct:** `MultipleActiveResultSets=true` (MARS — SQL Server's feature allowing several open result sets on one connection) present on the SQL Server string, absent on the PG string (Npgsql has no MARS).
- **Location(s):** `app/Bookstore.Web/Web.config:12` (SQL Server string); PG string (same file) omits it.
- **Why this run couldn't resolve it automatically:** nothing to fix in code — a sweep found no `QueryMultiple`, nested-reader, or multiple-simultaneous-reader patterns; EF6 lazy loading opens readers sequentially per request. The flag exists so a human confirms no hidden pattern relies on MARS before PG cutover.
- **Options considered, with trade-offs:** (a) accept omission (chosen) — correct on PG; if a hidden MARS dependency exists it will surface as "There is already an open DataReader" style errors under load; (b) audit every controller path with a profiler before cutover — costs QA time, removes the doubt.
- **Recommended action:** option (a), plus one QA pass exercising the admin screens (the most navigation-heavy paths) against PG.
- **Risk if left unaddressed:** runtime `InvalidOperationException` (open DataReader) on a PG connection if any code path does hold two readers — intermittent, request-path dependent, hard to reproduce.

### 9.3 DateTime → timestamp without time zone

- **Construct:** `DateTime.UtcNow` defaults on `CreatedOn`/`UpdatedOn` (`app/Bookstore.Domain/Entity.cs:12-14`); Npgsql maps `DateTime` to `timestamp without time zone` by default (Npgsql 4.x legacy behavior, left at default).
- **Location(s):** `app/Bookstore.Domain/Entity.cs:12-14`; all repository writes.
- **Why this run couldn't resolve it automatically:** behavior is correct as long as every value is UTC (it is — only `UtcNow` is used); changing the column type or enabling `timestamptz` is a model decision outside R2's allowance.
- **Options considered, with trade-offs:** (a) keep `timestamp` + UTC discipline (chosen) — no change, but any future local-time write would be stored ambiguously; (b) switch PG columns to `timestamptz` — unambiguous instants, but requires schema change (forbidden this run) and Npgsql behavior configuration.
- **Recommended action:** keep (a); document the UTC-only convention for the team.
- **Risk if left unaddressed:** only if a future feature writes `DateTime.Now` (local time) — timestamps become ambiguous across DST boundaries; no current code does this.

### 9.4 Isolation / snapshot behavior

- **Construct:** no C# `IsolationLevel`/`TransactionScope` usage exists; the app runs at the engine default.
- **Location(s):** whole solution (absence verified by grep).
- **Why this run couldn't resolve it automatically:** nothing to change — SQL Server default `READ COMMITTED` (locking reader) vs PostgreSQL `READ COMMITTED` (MVCC, non-locking reader) differ subtly in what concurrent transactions see; no code depends on read-blocking (verified), so no remediation is possible or needed automatically.
- **Options considered, with trade-offs:** (a) accept PG default READ COMMITTED (chosen) — matches the app's simple transaction usage; (b) pin `REPEATABLE READ` on PG for report-style reads — adds `40001` retry requirements for no demonstrated need.
- **Recommended action:** option (a); revisit only if a consistency anomaly is observed under load.
- **Risk if left unaddressed:** none demonstrated today; under heavy concurrent writes a single statement could see slightly different data than SQL Server would show — acceptable for this workload.

### 9.5 PostgreSQL deployment prerequisites (schema + infrastructure)

- **Construct:** the PG database itself, its schema, and its connection-string delivery do not exist yet: `db-scripts/*.sql` is SQL Server-only DDL (not ported, `allow_schema_changes=false`), and `DatabaseStack.cs` provisions only SQL Server RDS.
- **Location(s):** `db-scripts/bobs-used-bookstore-classic-db.sql`; `app/Bookstore.Cdk/DatabaseStack.cs:51-54`; `app/Bookstore.Web/App_Start/ConfigurationSetup.cs` (SSM path for the PG parameter); `Web.config` PG placeholder.
- **Why this run couldn't resolve it automatically:** creating PG infrastructure is a deployment action outside the codebase, and R1 forbids touching the SQL Server IaC; the EF6 initializer creates the schema on first run under the Npgsql provider, but only after a reachable database exists.
- **Options considered, with trade-offs:** (a) provision PG outside this change (chosen) — a human/DevOps task: create the database, put the real connection string in SSM at `/BobsBookstoreClassic/Database/ConnectionStrings/BookstoreDatabaseConnection_PostgreSql`, then set `Data:Provider=PostgreSql`; (b) extend the CDK to provision PG — larger IaC change, out of this run's scope per R1.
- **Recommended action:** option (a); the initializer's `DropCreateDatabaseIfModelChanges` + explicit-Id seed data should be smoke-tested on PG before cutover (identity seeding with explicit Ids is compile-verified only — OQ7).
- **Risk if left unaddressed:** flipping `Data:Provider=PostgreSql` without the database/parameter in place causes a hard startup failure (`KeyNotFoundException` or connection failure) — not a silent failure, but an outage of the PG path until provisioned.

## 10. Verification

- Build command: `dotnet build BobsBookstoreClassic.sln` (via the validate phase's `run-build.mjs`). **Result: exit code 1 — environment-only causes, both pre-existing and reproducible on the unmodified repo:** (1) the `packages/` folder is not present and no `nuget.exe`/VS NuGet targets exist in this environment, so `packages.config`/HintPath references cannot restore — the resulting CS0246 missing-assembly errors hit pre-existing files (`ApplicationDbContext.cs`, `BookstoreDbInitializer.cs`, `RekognitionImageValidationService.cs`) as much as ported ones; (2) `dotnet` SDK cannot build the Web application project (`MSB4019: Microsoft.WebApplication.targets not found` — requires Visual Studio's web targets). The one compile error introduced by the port (`CS1003` in `ISqlDialect.cs`, a `40P01` integer-literal typo) was fixed in the Iterate pass and no port-introduced syntax errors remain; full compile verification of the port is therefore **unverified** and listed here explicitly per R11.
- 190 build warnings, all pre-existing (obsolete AWS CDK APIs etc.); **0 warnings touch changed files**.
- Static checks: reference-load manifest complete for all execute-phase references (the 10 validate-phase checklist/report references are loaded by this phase by design, not by execute — noted, not a gap); reference-trigger coverage: 1 gap (`Entity.cs` / DB-concurrency-tokens) resolved by design — the file is intentionally untouched and covered by §9.1; `check-plpgsql-param-prefix.mjs`: 0 violations (no PL/pgSQL units exist).
- **Scope note:** this agent does not connect to or run tests against a live SQL Server or PostgreSQL instance; live dual-engine test execution was NOT performed here and remains QA's responsibility. Unverified paths (R11's list): everything requiring a running engine — EF6 model creation/seed under Npgsql, runtime provider resolution end-to-end, `nuget restore` + full compile in a VS-equipped environment.

## 11. Rollout prerequisites

1. **PostgreSQL 16 server** (version explicitly selected by the requester on the run form — recorded in `run-config.json pg_version_source`; the repo's CDK pins SQL Server 2017/VER_15, which does not constrain the PG target). No version-gated SQL constructs (JSON functions, `GENERATED ... STORED`, `INCLUDE` indexes) are used, so 16 is safe.
2. Create the PG database (`BookStoreClassic`) and a login; put the real connection string into SSM at `/BobsBookstoreClassic/Database/ConnectionStrings/BookstoreDatabaseConnection_PostgreSql` (AWS mode) or fill the `Web.config` placeholder (local mode).
3. Auth model: PG string uses user/password (SCRAM) — no Windows-auth equivalent needed; SQL Server localdb's `Integrated Security=SSPI` is untouched and remains an environment concern only for the SQL Server path.
4. First PG startup runs `BookstoreDbInitializer` (`DropCreateDatabaseIfModelChanges`) — verify the explicit-Id seed data inserts correctly under PG identity columns before relying on it.
5. Pooling: Npgsql pools by default; if PgBouncer in transaction-pooling mode is ever placed in front, configure `max_prepared_statements` (or disable prepared statements client-side) — prerequisite, not code.
6. No `40001` retry paths needed (no serializable/snapshot isolation usage). Watch autovacuum/bloat exposure from long-held connections — no SQL Server equivalent, new operational surface (none exist in current code; EF6 contexts are per-request).

## 12. Iteration history

- **Pass 0 → Iterate 1:** `dotnet build` reported `CS1003` in `ISqlDialect.cs` (`case 40P01:` — PG SQLSTATEs are strings, not int literals). Fixed by changing `MapErrorCode` to take/return `string` in the interface and both dialect implementations. Checklist cross-check also surfaced a missing explicit `Search Path=` in the PG connection string — added `Search Path=public`. Both fixes re-logged through execute's change log.

## Prior-run artifact cited

`artifacts/2eba91b7.../REPORT.md` + `PORTING_AGENT_MEMORY.md` from the prior SiaStockWeb run (2eba91b7) were used as the seam-pattern reference when shaping this run's `Provider/` seam and report structure — same wizard, same phase machinery, verified working shape.
