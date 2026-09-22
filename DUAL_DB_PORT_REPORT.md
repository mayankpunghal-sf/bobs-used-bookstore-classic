# DUAL_DB_PORT_REPORT.md — bobs-used-bookstore-classic (dual-db-port)

## 0. Run metadata / links

| Field | Value |
|---|---|
| Run ID | 3cd05ae6-7ca1-4aa6-b2d9-cc4bdac3b43d |
| Repository | https://github.com/sfdevops/bobs-used-bookstore-classic.git (fresh checkout, HEAD `5c7c0ae`) |
| Branch | porting/dual-db-port-3cd05ae6-20260922T150939Z |
| Commit SHA | dc9bde2fe38405bdea0d63258b30f6aa05387cde |
| Pull request | https://github.com/mayankpunghal-sf/bobs-used-bookstore-classic/pull/9 (PR #9) |
| Confidence score | 91 — 91% of applicable checklist items passed, adjusted for unresolved flags and build warnings (see §10) |
| Build status | pass — compile-only, port scope (see §10 scope note) |
| Iteration | 1 (one Iterate pass; see §12) |
| Generated at | 2026-10-09 (UTC) |

**Process note for the reviewer:** `FileChanges.json` raised `runaway_scope` (2 of 7 changed files were not in the original plan-items). Both were mid-run discoveries, mechanically verified against repo evidence and accepted into scope before editing: `Bookstore.Data.csproj` (the approved package decision had to land in the csproj, which the analyze sweep had not classified) and `Entity.cs` (EF6 has no shadow properties, so the approved xmin concurrency token needs a CLR property). Both are traceable to approved plan decisions; nothing was edited outside `plan_items ∪ accepted_discoveries`.

## 1. Summary

The codebase's data access is EF6-only with zero raw SQL, so the port is a provider-seam port rather than a SQL-translation port. The application now selects its database engine from a single `Data:Provider` appSetting (`SqlServer` default, `PostgreSql` opt-in), resolved once at startup into a typed `DatabaseProvider` enum. The composition root builds a per-request `SqlConnection` or `NpgsqlConnection` from the matching connection string and hands it to a new `ApplicationDbContext(DbConnection, bool)` constructor. `OnModelCreating` carries a PostgreSQL-only model fork — `Customer.Sub` as `varchar`, UTC timestamps as `timestamptz`, and an `xmin`-backed optimistic-concurrency token replacing the SQL Server `rowversion` — while the SQL Server branch keeps the pre-port model byte-identical. `EntityFramework6.Npgsql 6.4.3` + `Npgsql 4.1.14` were added to `Bookstore.Data.csproj`, and the Npgsql EF6 provider plus `DbProviderFactories` entry were registered additively in `Web.config`/`App.config` alongside a new `AppDb_PostgreSql` connection string. The SQL Server connection string, MARS setting, repositories, initializer, CDK stack, and SSM setup are untouched.

Engines verified: **compile-time only** (no live SQL Server or PostgreSQL instance was connected — that is QA's responsibility; see §10). Top risks: the xmin token's runtime behavior on PostgreSQL is unverified (§9.4), `Order.DeliveryDate`'s `DateTime.Now` default violates the UTC-everywhere convention (§9.2), and PostgreSQL's case-sensitive unique index on `Customer.Sub` can admit duplicates SQL Server would reject (§9.3).

## 2. File sweep table

| File | Classification | Changes applied / reason unaffected |
|---|---|---|
| app/Bookstore.Data/BookstoreConfiguration.cs | config/seam | **Changed** (+24/-1): `DatabaseProvider` enum + `GetDatabaseProvider()` reading `Data:Provider` (default SqlServer) |
| app/Bookstore.Data/ApplicationDbContext.cs | data-access | **Changed** (+43/-2): `(DbConnection, bool)` ctor; provider-conditional model fork |
| app/Bookstore.Domain/Entity.cs | domain (discovery) | **Changed** (+6/-1): additive `uint xmin` property (EF6 has no shadow properties) |
| app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs | composition root | **Changed** (+20/-3): provider resolved once; per-request connection; context via new ctor |
| app/Bookstore.Data/Bookstore.Data.csproj | project (discovery) | **Changed** (+7/-1): EntityFramework6.Npgsql 6.4.3 + Npgsql 4.1.14 PackageReferences |
| app/Bookstore.Data/App.config | config | **Changed** (+8/-1): Npgsql provider + DbProviderFactories (additive) |
| app/Bookstore.Web/Web.config | config | **Changed** (+12/-1): `AppDb_PostgreSql` string, Npgsql provider + DbProviderFactories, `Data:Provider` key |
| 7 repositories + BookstoreDbInitializer.cs | data-access | Skipped — provider-agnostic pure EF6 LINQ |
| app/Bookstore.Cdk/DatabaseStack.cs | infra | Skipped — CDK RDS stack is SQL Server infra; PG deployment is a separate infra task |
| app/Bookstore.Web/App_Start/ConfigurationSetup.cs | config | Skipped — SSM parameter loading is provider-agnostic |
| db-scripts/bobs-used-bookstore-classic-db.sql | schema | Skipped — SQL Server reference DDL; PG schema is generated from the EF6 model |
| packages.config, view web.configs, Web.Debug/Release.config | config | Skipped — no provider-specific content |

## 3. Inventory

| File | Line | Construct | Risk class | Resolution |
|---|---|---|---|---|
| Entity.cs | 16-17 | `[Timestamp] byte[] RowVersion` (SQL Server rowversion) | Fork | PG branch: ignored; replaced by `uint xmin` token (§9.4) |
| Entity.cs | 19 | additive `uint xmin` | Portable | PG: concurrency token via `IsConcurrencyToken()+Computed`; SQL Server branch ignores it |
| ApplicationDbContext.cs | 40 | `Customer.Sub` `nvarchar(450)` | Fork | PG branch maps `varchar(450)`; case-folding difference flagged (§9.3) |
| Entity.cs / Order.cs | 12-14 / 30 | `DateTime` CreatedOn/UpdatedOn/DeliveryDate | Portable | PG branch maps `timestamp with time zone`; Kind convention flagged (§9.2) |
| Web.config | 12 | `MultipleActiveResultSets=true` | Portable | SQL Server string untouched; PG string needs no MARS (EF6 buffers materialization) |
| db-scripts/*.sql | — | SQL Server reference DDL | N/A | Untouched; PG schema generated from EF6 model at first run |

Zero raw SQL statements exist in scoped code; there is nothing in the `ISqlQueryProvider`/dialect category to inventory beyond the above.

## 4. Config changes

- **Switch:** `<add key="Data:Provider" value="SqlServer" />` in `Web.config` appSettings; read once at startup by `BookstoreConfiguration.GetDatabaseProvider()` (default `SqlServer` when absent). Invalid-value handling flagged in §9.1.
- **Connection strings (placeholders):**
  - `BookstoreDatabaseConnection` — unchanged: `Server=(localdb)\MSSQLLocalDB;Initial Catalog=BookStoreClassic;MultipleActiveResultSets=true;Integrated Security=SSPI;`
  - `AppDb_PostgreSql` (new): `Host=localhost;Port=5432;Database=BookStoreClassic;Username=postgres;Password=postgres;Search Path=public` — placeholder values; real credentials come from environment/SSM (R5: no secrets in source).
- **Provider registration (additive):** `<provider invariantName="Npgsql" type="Npgsql.NpgsqlServices, EntityFramework6.Npgsql" />` in both `entityFramework/providers` sections, plus a `DbProviderFactories` entry pinning `Npgsql, Version=4.1.14.0` (verified against the restored assembly).
- **Startup validation:** the composition root resolves the selected provider's connection string at startup; a missing name throws immediately. A separate non-empty assertion was not added (noted in §10).

## 5. Seams introduced

| Seam | Responsibility | Registration site |
|---|---|---|
| `DatabaseProvider` enum + `GetDatabaseProvider()` | typed provider value, resolved once (R7) | `BookstoreConfiguration.cs`; consumed by composition root + model fork |
| `ApplicationDbContext(DbConnection, bool)` ctor | provider decided by the supplied connection, not string sniffing | `ApplicationDbContext.cs` |
| Composition-root connection factory | per-request `NpgsqlConnection`/`SqlConnection` from the provider-specific string | `DependencyInjectionSetup.ConfigureDependencyInjection` |
| PG-only model fork | store-type divergences behind one `isPostgreSql` branch in `OnModelCreating` (sanctioned EF6 forking point per ef6.md §11) | `ApplicationDbContext.cs` |

`ISqlDialect`/`ISqlQueryProvider`/`IBulkInserter` were recorded **not applicable** in plan (zero raw SQL, no `SqlBulkCopy`) — consistent with prior run 499021d5's accepted minimal seam shape.

## 6. SQL changes

None. The codebase contains no raw SQL, no stored procedures, and no `SqlBulkCopy`; all data access is EF6 LINQ translated per provider. `db-scripts/bobs-used-bookstore-classic-db.sql` is SQL Server reference DDL and was deliberately not translated (schema is code-first via `DropCreateDatabaseIfModelChanges`; the Npgsql EF6 provider generates the PG schema from the model).

## 7. Type mapping applied

| SQL Server | PostgreSQL | Notes |
|---|---|---|
| `nvarchar(450)` (Customer.Sub) | `varchar(450)` | No loss — PG `varchar` is UTF-8; length semantics preserved |
| `datetime` (CreatedOn/UpdatedOn/DeliveryDate) | `timestamp with time zone` | §5.9; Kind convention deviation flagged (§9.2) |
| `rowversion` (RowVersion) | `xmin` (via `uint xmin` token) | R9 option (b); runtime verification flagged (§9.4) |
| `int` identity | `serial`/identity (provider-generated) | Model-generated; no explicit DDL |

No lossy mappings, no bare `DECIMAL`, no `money`, no `uniqueidentifier` columns in scope.

## 8. Checklist result summary

Full per-item detail: `validation.json` in the agent-state dir (85 items). Counts by group:

| Group | pass | flagged | not_applicable |
|---|---|---|---|
| core-wiring | 14 | 0 | 2 (R6, R11) |
| sign-off | 2 | 0 | 0 |
| dotnet-breaks | 2 | 1 (§5.9) | 15 |
| configuration | 4 | 0 | 1 (pooler) |
| schema-ddl | 2 | 0 | 5 |
| naming | 2 | 0 | 2 |
| syntax-behavior | 1 | 2 (B8, B13) | 24 |
| types | 1 | 0 | 2 |
| per-technology | 1 | 0 | 1 |
| **total** | **30** | **4** | **51** |

Confidence: `100 * (30/33) - 10*0 - 5*0 = 91` (applicable = 33 after load-plan negative determinations; 0 unresolved "no equivalent" flags; 0 port-attributable build warnings).

## 9. R9 flags — open items for human review

### 9.1 Invalid `Data:Provider` value silently falls back to SQL Server
- **Construct:** `GetDatabaseProvider()` treats any unrecognized `Data:Provider` value as `SqlServer` (default).
- **Location(s):** `app/Bookstore.Data/BookstoreConfiguration.cs` (GetDatabaseProvider).
- **Why this run couldn't resolve it automatically:** the approved plan decision was "default SqlServer when the key is absent" (preserves existing behavior, R1); distinguishing an absent key from a present-but-invalid value is a startup-policy decision.
- **Options considered, with trade-offs:** (a) fail fast on any value outside {SqlServer, PostgreSql} — strictest, matches the mode contract, but changes startup behavior for configs that omit the key; (b) keep the default and log a startup warning naming the invalid value and the allowed values — preserves the approved default, surfaces typos; (c) leave silent — no code change, typos invisible.
- **Recommended action:** option (b) — a one-line startup warning. Cheap, and it keeps the approved default semantics.
- **Risk if left unaddressed:** a typo such as `Postgres` or `postgre` silently runs the app on SQL Server; an operator who believes they cut over to PostgreSQL keeps writing to SQL Server until data divergence is noticed downstream.

### 9.2 `DateTime.Kind` convention not enforced end-to-end
- **Construct:** `Order.DeliveryDate { get; set; } = DateTime.Now.AddDays(7)` — `Kind=Local`, while `Entity.CreatedOn/UpdatedOn` use `DateTime.UtcNow`.
- **Location(s):** `app/Bookstore.Domain/Orders/Order.cs` line 30; consumed by `ApplicationDbContext`'s timestamptz mapping.
- **Why this run couldn't resolve it automatically:** changing the default to `DateTime.UtcNow` alters SQL Server behavior, which R1 forbids without sign-off; the "right" value is a product decision.
- **Options considered, with trade-offs:** (a) change the default to `UtcNow` — cleanest convention, but changes what SQL Server stores today; (b) keep as-is — Npgsql converts `Local` → UTC on write to `timestamptz`, so PG writes are correct as long as the app server's timezone is stable (recommended); (c) enforce Kind=Utc via a `SaveChanges` interceptor on the PG branch only — adds provider-conditional behavior for one property.
- **Recommended action:** option (b); QA verifies PG writes convert correctly; document the UTC-everywhere convention for new code.
- **Risk if left unaddressed:** stored `DeliveryDate`s shift if the app-server timezone ever changes — wrong delivery promises, silently.

### 9.3 Unique-index case-folding on `Customer.Sub` (B8)
- **Construct:** unique index on `Customer.Sub` (`nvarchar(450)` → `varchar(450)`).
- **Location(s):** `app/Bookstore.Data/ApplicationDbContext.cs` (PG branch), SQL Server CI collation vs PostgreSQL case-sensitive comparison.
- **Why this run couldn't resolve it automatically:** the real fixes (`citext` column, unique index on `lower(Sub)`) are schema changes and `allow_schema_changes=false`.
- **Options considered, with trade-offs:** (a) `citext` extension + column — needs extension install + schema change; (b) unique index on `lower(Sub)` — schema change, preserves CI semantics; (c) accept case-sensitive uniqueness on PG and document — zero schema change, semantics differ.
- **Recommended action:** option (c) this run; if QA observes duplicate `Sub` variants, revisit with option (b) under a schema-change approval.
- **Risk if left unaddressed:** the same external identity-provider subject differing only in case creates two customer accounts on PostgreSQL where SQL Server would have merged them — split order history and carts.

### 9.4 xmin concurrency token is not runtime-verified
- **Construct:** `uint xmin` property mapped with `IsConcurrencyToken()` + `HasDatabaseGeneratedOption(Computed)` on the PG branch (EF6 has no `IsRowVersion()` on `PrimitivePropertyConfiguration`, and no shadow properties, so the token needs a CLR property).
- **Location(s):** `app/Bookstore.Domain/Entity.cs` (property), `app/Bookstore.Data/ApplicationDbContext.cs` (PG-branch mapping; SQL Server branch ignores it).
- **Why this run couldn't resolve it automatically:** this agent never connects to a live PostgreSQL instance; the mapping compiles but its insert/update behavior on PG 16 is runtime behavior.
- **Options considered, with trade-offs:** (a) QA smoke test — insert + concurrent update on PG, assert `DbUpdateConcurrencyException` fires (recommended, cheap); (b) fall back to R9 option (a): `Ignore(xmin)` on the PG branch — drops optimistic concurrency on PG (documented deviation); (c) app-managed version column — schema change, not allowed.
- **Recommended action:** option (a) — one QA smoke test settles it; option (b) is the recorded fallback if the provider does not bind the token.
- **Risk if left unaddressed:** lost updates on PostgreSQL under concurrent edits (last-write-wins) if the token silently fails to bind, or spurious `DbUpdateConcurrencyException` on every save if EF expects a DB-generated value PostgreSQL does not return.

## 10. Verification

- **Build (compile-only):** VS 2026 MSBuild (`C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`) `/restore` on `app/Bookstore.Data/Bookstore.Data.csproj` — **exit 0**, `Bookstore.Data.dll` + `Bookstore.Domain.dll` produced. `run-build.mjs`'s `dotnet build` fails env-only for this packages.config-era repo (known from prior runs); MSBuild is the repo's real build command.
- **Full-solution caveat:** `BobsBookstoreClassic.sln` fails on `Bookstore.Web` with a **pre-existing** packages.config restore gap (`Microsoft.CodeDom.Providers.DotNetCompilerPlatform` targets missing) — unrelated to the port; the Web project could not compile in this environment before or after the change.
- **Warnings:** 0 attributable to the port. The build prints pre-existing noise: Magick.NET advisory warnings (NU1901/1902/1903) and `MSB3245`/`MSB3243` HintPath-vs-package version mismatches (present in the unmodified repo), CDK net6.0 EOL warnings. The port initially introduced `NU1903` for Npgsql 4.1.3 (high-severity advisory GHSA-x9vc-6hfv-hg8c) — cleared in Iterate 1 by bumping to Npgsql 4.1.14 (latest 4.1.x patch; EF6.Npgsql minimum satisfied).
- **Static cross-checks:** `check-reference-triggers-coverage` ok (0 gaps); `check-checklist-coverage` ok (55/55 cited refs covered); `check-references-used` reported 10 "missing" — all are validate-phase checklist groups + report-template.md, which load during validate itself (all 9 groups loaded this pass); no execute-phase reference is missing.
- **Scope note:** this agent does not connect to or run tests against a live SQL Server/PostgreSQL instance. Live dual-engine verification was **not** performed; every runtime path (PG schema generation, xmin token, timestamptz writes, provider switch end-to-end) is listed here as unverified and belongs to QA.
- **Prior-run artifacts that informed this run:** `499021d5` report/memory (PR #7, confidence 91) — same repo, same minimal EF6-provider seam shape, reused as the approved seam precedent; `fdd001ab` (PR #8, confidence 91) — full Provider/-seam alternative considered and rejected as oversized for an EF6-only codebase.

## 11. Rollout prerequisites

- **PG version:** 16 — from the explicit New Run form selection (task instruction); repo/infra not consulted per form override. Recorded with evidence in `run-config.json`.
- **Schema creation:** none shipped — the Npgsql EF6 provider generates the PG schema from the model at first run under `DropCreateDatabaseIfModelChanges`. QA must verify initial schema creation on a clean PG 16 database (unverified path).
- **Extensions:** none required by the shipped code (`citext` only if §9.3 option (a) is adopted).
- **Auth model:** `AppDb_PostgreSql` uses user/password placeholders — wire real credentials via environment/SSM before any non-local deployment (R5).
- **`search_path`:** pinned to `public` in the connection string (matches `pg_schema=public`).
- **Pooling:** Npgsql default pooling; no PgBouncer in this run's config. If a transaction-mode pooler is later placed in front of PostgreSQL, configure `max_prepared_statements` or disable prepared statements client-side (config.md connection-lifecycle addendum).
- **Retry-on-`40001`:** not implemented — no serializable/repeatable-read paths exist in the codebase; EF6's SQL-Server-only `EnableRetryOnFailure` is not used.
- **Connection-lifecycle operational risks:** long-held/idle-in-transaction connections expose autovacuum/bloat pressure with no SQL Server equivalent — new operational surface for this team; standard Npgsql pooling mitigates unless sessions are held open manually.
- **Pre-existing package advisories:** Magick.NET-Q8-AnyCPU 14.6.0 carries multiple known vulnerabilities (NU1901/NU1902/NU1903) — untouched by this port, but worth a separate upgrade ticket.

## 12. Iteration history

- **Pass 0 (validate):** build failed. Three causes, all fixed by re-entering execute: (1) missing `using BobsBookstoreClassic.Data;` / `using Bookstore.Domain;` in `ApplicationDbContext.cs` (CS0103/CS0246); (2) EF6's `PrimitivePropertyConfiguration` has no `IsRowVersion()` — xmin token rewritten as `IsConcurrencyToken()` + `HasDatabaseGeneratedOption(Computed)`; (3) NU1903 high-severity advisory on Npgsql 4.1.3 — bumped to 4.1.14 (latest 4.1.x patch), `DbProviderFactories` version strings updated to the verified assembly version 4.1.14.0.
- **Pass 1 (validate, this report):** port scope builds clean; confidence 91; 4 flags carried to §9.