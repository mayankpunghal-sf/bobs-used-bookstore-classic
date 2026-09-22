# DUAL_DB_PORT_REPORT.md — bobs-used-bookstore-classic (dual-db-port)

## 0. Run metadata / links

| Field | Value |
|---|---|
| Run ID | fc370422-db45-4080-bee7-621f5e3c7f88 |
| Repository | https://github.com/sfdevops/bobs-used-bookstore-classic.git (fresh checkout, GitHub-sourced) |
| Branch | porting/dual-db-port-fc370422 (filled in by the git phase) |
| Commit SHA | pending — filled in by the git phase after this report is written |
| Pull request | pending — filled in by the git phase after this report is written |
| Confidence score | 94 — 94% of applicable checklist items passed, adjusted for unresolved flags and build warnings (see §10) |
| Build status | pass — compile-only, full solution (see §10 scope note) |
| Iteration | 1 (one Iterate pass; see §12) |
| Generated at | 2026-09-22 (UTC) |

**Process note for the reviewer:** `FileChanges.json` raised `runaway_scope` (3 of 9 changed files were not in the original plan-items). All three were mid-run discoveries, mechanically verified against repo evidence (`verify-discovery.mjs`) and accepted into scope before editing: `Bookstore.Data.csproj` (the approved package decision lands in the csproj), `Entity.cs` (EF6 has no shadow properties, so the approved xmin token needs a CLR property), and `Bookstore.Web.csproj` (the packages.config-era Web project needs explicit Npgsql references for `NpgsqlConnection` in `DependencyInjectionSetup.cs`). All three trace to approved plan decisions; nothing was edited outside `plan_items ∪ accepted_discoveries`.

## 1. Summary

The codebase's data access is EF6-only with zero raw SQL, so the port is a provider-seam port rather than a SQL-translation port. The application now selects its database engine from a single `Data:Provider` appSetting (`SqlServer` default, `PostgreSql` opt-in), resolved once at startup into a typed `DatabaseProvider` enum that **throws on invalid values** (an improvement over the prior run, which silently defaulted). The composition root builds a per-request `SqlConnection` or `NpgsqlConnection` from the matching connection string and hands it to a new `ApplicationDbContext(DbConnection, bool)` constructor. `OnModelCreating` carries a PostgreSQL-only model fork — `Customer.Sub` as `varchar`, UTC timestamps as `timestamptz`, and an `xmin`-backed optimistic-concurrency token (additive `uint` CLR property, `IsConcurrencyToken` + `Computed`) replacing the SQL Server `rowversion` — while the SQL Server branch keeps the pre-port model byte-identical. `EntityFramework6.Npgsql 6.4.3` + `Npgsql 4.1.14` (nuget-resolved; the 4.1.14 pin clears the NU1903 advisory that Npgsql 4.1.3 carried) were added to `Bookstore.Data.csproj` as PackageReferences and to the Web project as packages.config entries + Reference/HintPath, with the Npgsql EF6 provider and `DbProviderFactories` registered additively in `Web.config`/`App.config` alongside a new `AppDb_PostgreSql` connection string and a `Data:Provider` appSetting. The SQL Server connection string, MARS setting, repositories, initializer, CDK stack, and SSM setup are untouched.

Engines verified: **compile-time only** (full solution builds clean under MSBuild `/restore`; no live SQL Server or PostgreSQL instance was connected — that is QA's responsibility; see §10). Top risks: the xmin token's runtime behavior on PostgreSQL is unverified (§9.1), `Order.DeliveryDate`'s `DateTime.Now` default violates the UTC-everywhere convention (§9.2), and PostgreSQL's case-sensitive unique index on `Customer.Sub` can admit duplicates SQL Server would reject (§9.3).

## 2. File sweep table

| File | Classification | Changes applied / reason unaffected |
|---|---|---|
| app/Bookstore.Data/BookstoreConfiguration.cs | config/seam | **Changed** (+27/-1): `DatabaseProvider` enum + `GetDatabaseProvider()` reading `Data:Provider` (default SqlServer, throws on invalid values) |
| app/Bookstore.Data/ApplicationDbContext.cs | data-access | **Changed** (+69/-2): `(DbConnection, bool)` ctor; provider-conditional model fork (SQL Server branch byte-identical except `Ignore(xmin)`) |
| app/Bookstore.Domain/Entity.cs | domain (discovery) | **Changed** (+5/-1): additive `uint xmin` property (EF6 has no shadow properties) |
| app/Bookstore.Web/App_Start/DependencyInjectionSetup.cs | composition root | **Changed** (+19/-3): provider resolved once; per-request connection; context via new ctor |
| app/Bookstore.Data/Bookstore.Data.csproj | project (discovery) | **Changed** (+7/-1): `EntityFramework6.Npgsql 6.4.3` + `Npgsql 4.1.14` PackageReferences |
| app/Bookstore.Web/Bookstore.Web.csproj | project (discovery) | **Changed** (+7/-1): Reference+HintPath for Npgsql.dll / EntityFramework6.Npgsql.dll (packages.config-era project) |
| app/Bookstore.Web/packages.config | config | **Changed** (+3/-1): both Npgsql packages, net48 |
| app/Bookstore.Data/App.config | config | **Changed** (+7/-1): Npgsql provider + DbProviderFactories (4.1.14.0) |
| app/Bookstore.Web/Web.config | config | **Changed** (+13/-1): `AppDb_PostgreSql` string (Search Path=public, no MARS, placeholders), `Data:Provider` appSetting (default SqlServer), Npgsql provider + DbProviderFactories, Npgsql binding redirect |
| 7 × Repositories/*.cs | data-access | Unchanged — pure EF6 LINQ, provider-agnostic |
| app/Bookstore.Data/BookstoreDbInitializer.cs | data-access | Unchanged — provider-agnostic Seed |
| app/Bookstore.Cdk/DatabaseStack.cs | data-access (signature only) | Unchanged — CDK provisions SQL Server infra; PG path is config-level (R1) |
| app/Bookstore.Web/App_Start/ConfigurationSetup.cs | data-access (signature only) | Unchanged — SSM parameter path is provider-agnostic |
| app/Bookstore.Web/Areas/Admin/Views/web.config, Views/Web.config, Web.Debug.config, Web.Release.config | config | Unchanged — no provider references |
| db-scripts/bobs-used-bookstore-classic-db.sql | schema | Unchanged — SQL Server reference DDL; PG schema is EF6-initializer generated (R1) |

## 3. Inventory

No raw SQL statements exist in the codebase (EF6 generates all SQL per provider) — nothing to inventory. The provider-divergent constructs are all EF6 model-level:

| File | Line | Construct | Risk class | Resolution |
|---|---|---|---|---|
| app/Bookstore.Data/ApplicationDbContext.cs | 40 | `Customer.Sub` `HasColumnType("nvarchar")` | Fork | PG branch → `varchar`; SQL Server branch verbatim (R8) |
| app/Bookstore.Domain/Entity.cs | 16-17 | `[Timestamp] byte[] RowVersion` (rowversion) | No equivalent | R9 flag → xmin token on PG fork (§9.1) |
| app/Bookstore.Domain/Entity.cs | 12-14 | `DateTime CreatedOn/UpdatedOn = DateTime.UtcNow` | Portable | PG fork → `timestamptz` (§5.9) |
| app/Bookstore.Domain/Orders/Order.cs | 30 | `DeliveryDate` default `DateTime.Now` | Fork | PG fork → `timestamptz`; Kind=Local flagged (§9.2) |
| app/Bookstore.Web/Web.config | 12 | `MultipleActiveResultSets=true` | Fork | SQL Server string unchanged; PG string omits MARS (§5.8) |

## 4. Config changes

- **Switch:** `Data:Provider` appSetting added (`SqlServer` default; `PostgreSql` opt-in). Invalid values throw at startup (fail-fast, R7).
- **Connection strings:** `BookstoreDatabaseConnection` (SQL Server, localdb + MARS) **byte-for-byte unchanged** (R1). New `AppDb_PostgreSql`: `Host=localhost;Port=5432;Database=BookStoreClassic;Username=postgres;Password=[placeholder];Search Path=public` — placeholder credentials only (R5), Npgsql syntax, no MARS (no equivalent needed).
- **Startup validation:** provider resolved once at composition-root startup; missing connection-string key throws at first use.
- **Provider registration:** Npgsql EF6 provider (`Npgsql.NpgsqlServices, EntityFramework6.Npgsql`) added additively to `<entityFramework><providers>` in both Web.config and App.config; `DbProviderFactories` entry added (`Npgsql.NpgsqlFactory, Npgsql, Version=4.1.14.0, PublicKeyToken=5d8b90d52f46fda7` — token verified from the build's own MSB3247 output); Npgsql binding redirect added to Web.config's runtime section.

## 5. Seams introduced

| Seam | Type | Responsibility | Registration site |
|---|---|---|---|
| `DatabaseProvider` enum + `GetDatabaseProvider()` | static accessor | Resolve `Data:Provider` once into a typed value; fail fast on invalid values (R7) | `BookstoreConfiguration.cs`; consumed by composition root + model fork |
| `ApplicationDbContext(DbConnection, bool)` | constructor overload | Accept an externally-owned connection so the engine choice lives at the composition root | `DependencyInjectionSetup.cs` (Autofac `InstancePerRequest`) |
| Composition-root connection build | factory role | Build `SqlConnection` or `NpgsqlConnection` per resolved provider, per request | `DependencyInjectionSetup.cs` |

`ISqlDialect`, `ISqlQueryProvider`, `IBulkInserter` are **not applicable** — the repo has zero raw SQL and no bulk-copy paths; EF6 generates all SQL per provider (evidence in decisions.json).

## 6. SQL changes

None — zero raw SQL in the codebase (confirmed by sweep signatures + detect-tech: no ADO.NET, no Dapper, no stored procedures). All data access is EF6 LINQ, which the Npgsql EF6 provider translates per engine.

## 7. Type mapping applied

| Construct | SQL Server | PostgreSQL | Note |
|---|---|---|---|
| `Customer.Sub` | `nvarchar(450)` | `varchar(450)` | PG-only fork; lossy-free (Unicode via DB encoding) |
| `Entity.CreatedOn/UpdatedOn`, `Order.DeliveryDate` | `datetime2` | `timestamptz` | PG-only fork per §5.9; `DeliveryDate` default is `DateTime.Now` — flagged (§9.2) |
| `Entity.RowVersion` | `rowversion` | `xmin` system column (uint token) | R9 option (b); SQL Server branch keeps `[Timestamp]` |
| `decimal` | `decimal(p,s)` | `numeric(p,s)` | native Npgsql mapping, no action; 0 bare DECIMAL found |
| `Entity.xmin` (new) | ignored | concurrency token | additive CLR property; SQL Server model ignores it |

## 8. Checklist result summary

83 items cross-checked (full per-item detail in the agent-state dir's `validation.json` / `checklist-results.json`): **30 pass / 2 flagged / 51 not_applicable**. Per group: core-wiring 15 pass / 2 n/a; sign-off 2 pass; dotnet-breaks 2 pass / 1 flagged / 14 n/a; configuration 4 pass / 1 n/a; schema-ddl 1 pass / 6 n/a; naming 2 pass / 2 n/a; syntax-behavior 1 flagged / 27 n/a; types 2 pass / 1 n/a; per-technology 1 pass / 1 n/a. Coverage gates: checklist-coverage ok (55 refs), triggers-coverage ok (0 gaps), references-used: 10 'missing' all validate-phase refs (loaded this pass).

## 9. R9 flags — open items for human review

### 9.1 — `Entity.RowVersion` optimistic-concurrency token under PostgreSQL

- **Construct:** `[Timestamp] public byte[] RowVersion` — EF6's SQL Server `rowversion` (database-generated version stamp used for optimistic concurrency).
- **Location(s):** `app/Bookstore.Domain/Entity.cs:16-17` (declaration); `app/Bookstore.Data/ApplicationDbContext.cs` `ConfigureXminConcurrency`/`ConfigureSqlServerModel` (per-engine mapping).
- **Why this run couldn't resolve it automatically:** SQL Server `rowversion` has no PostgreSQL equivalent; the replacement strategy is a durability/behavior trade-off a human must own. This run implemented the approved option (b), but its **runtime behavior on PostgreSQL is unverified** (compile-only verification — see §10): EF6 has no `IsRowVersion()` on `PrimitivePropertyConfiguration` and no shadow properties, so the token is an additive `uint xmin` CLR property mapped with `IsConcurrencyToken()` + `HasDatabaseGeneratedOption(Computed)`; whether the EF6 initializer can create/attach the `xmin` column cleanly (it shares its name with PostgreSQL's built-in system column) can only be confirmed against a live database.
- **Options considered, with trade-offs:** (a) drop the token on the PG fork only — zero runtime risk, but silently loses optimistic concurrency on PG (concurrent-update overwrites go undetected); (b) xmin-backed uint token (implemented) — preserves real optimistic concurrency, standard Npgsql pattern, but depends on unverified EF6-provider mapping behavior; (c) manual app-managed version column — engine-portable and explicit, but changes business behavior (R2 risk) and touches every write path.
- **Recommended action:** keep (b) as implemented; run a QA smoke test that performs two concurrent updates to the same row under `Data:Provider=PostgreSql` and asserts the second save throws `DbUpdateConcurrencyException`. If the EF6 provider cannot map `xmin` cleanly, fall back to (a) — it is a two-line change in `ConfigureXminConcurrency` (drop the `xmin` mapping, keep `Ignore(RowVersion)`).
- **Risk if left unaddressed:** if (b)'s mapping misbehaves at runtime, either every PG write fails at schema-creation time (initializer throws on first request — immediate, loud) or, worse, the token is silently inert and concurrent edits to the same entity overwrite each other with no error — data loss that only appears under concurrent edits on the PG deployment.

### 9.2 — `Order.DeliveryDate` defaults to `DateTime.Now` (Kind=Local)

- **Construct:** `public DateTime DeliveryDate { get; set; } = DateTime.Now;` — a local-time default flowing into a `timestamptz` column on the PG fork.
- **Location(s):** `app/Bookstore.Domain/Orders/Order.cs:30`.
- **Why this run couldn't resolve it automatically:** changing the default to `DateTime.UtcNow` alters observable business behavior (R2 — no logic changes), and whether "delivery date" is meant as a local calendar notion or an instant is a product decision.
- **Options considered, with trade-offs:** (a) leave as-is — Npgsql converts Local→UTC on write, so values are stored correctly, but the UTC-everywhere convention is violated at the source and any Kind-sensitive comparison in app code can misbehave; (b) change the default to `DateTime.UtcNow` — convention-clean, but shifts the displayed value for anyone relying on local semantics; (c) switch the property to `DateTimeOffset` — most correct, but a model + UI change beyond this port's scope.
- **Recommended action:** (a) for this port (behavior-preserving); file a follow-up ticket for (b)/(c) as a deliberate convention cleanup.
- **Risk if left unaddressed:** low — values are converted correctly on write; the concrete failure mode is an app-side `Kind`-dependent comparison (e.g. `DateTimeKind` checks or client-side display logic) treating a Local-kind value as UTC and showing a shifted delivery date.

### 9.3 — Case-sensitive unique index on `Customer.Sub`

- **Construct:** unique index on `Customer.Sub` (`modelBuilder.Entity<Customer>().HasIndex(x => x.Sub).IsUnique()`), a text column.
- **Location(s):** `app/Bookstore.Data/ApplicationDbContext.cs:41` (index); `:40`/`:109` (the forked `nvarchar`→`varchar` mapping).
- **Why this run couldn't resolve it automatically:** SQL Server's default collation compares text case-insensitively; PostgreSQL's default is case-sensitive. Whether "Sub" values that differ only by case should count as duplicates is a data-integrity policy decision, and `allow_schema_changes=false` rules out schema-level remediation in this run.
- **Options considered, with trade-offs:** (a) accept the difference — PG simply enforces stricter uniqueness; no code change; (b) use a case-insensitive unique index on PG (e.g. `CREATE UNIQUE INDEX ... ON lower(sub)` via a migration or initializer hook) — matches SQL Server semantics but adds schema machinery outside this run's scope; (c) normalize `Sub` to a fixed case in application code before save — behavior change (R2).
- **Recommended action:** (a) for this run, with (b) as the follow-up if case-variant duplicates are unacceptable to the business.
- **Risk if left unaddressed:** under `Data:Provider=PostgreSql`, two customers whose Sub differs only by case ("Alice" vs "alice") can both be inserted where SQL Server would have rejected the second — a duplicate-account condition that surfaces later as ambiguous lookups or login confusion, not as an error.

## 10. Verification

- **Build:** `nuget restore BobsBookstoreClassic.sln` (packages.config projects; 56 packages) then `msbuild BobsBookstoreClassic.sln /restore` — **exit 0, full solution** (Common, Cdk, Domain, Data, Web). `dotnet build` fails env-only on this packages.config-era repo (it cannot restore packages.config) — not port-attributable.
- **Warnings:** 168 total, all pre-existing Magick.NET NU190x advisory / MSB3243 conflict warnings on the untouched package set; **0 port-attributable** (the Npgsql MSB3247 version conflict introduced by the 4.1.14 pin was fixed during Iterate 1 with a binding redirect).
- **Assembly placement:** `Npgsql.dll` + `EntityFramework6.Npgsql.dll` verified present in `Bookstore.Web/bin` (the project whose code instantiates `NpgsqlConnection`).
- **Scope note:** build/compile verification and static checklist cross-checks only — **no live SQL Server or PostgreSQL instance was connected to or tested**; live dual-engine regression testing is QA's responsibility.
- **Unverified paths (all runtime, pending QA):** EF6 initializer schema creation on PG (including the `xmin` column, §9.1); actual query execution on both engines; `Data:Provider=PostgreSql` end-to-end flow; `AppDb_PostgreSql` connectivity with real credentials; xmin concurrency behavior under concurrent updates.

## 11. Rollout prerequisites

- **PG version:** 16 — explicitly selected on the New Run form by the operator (recorded in `run-config.json` `pg_version_source: "task input (New Run form)"`); no repo/infra auto-detect was attempted per task instruction. No version-gated constructs (MERGE, JSON functions, `GENERATED ... STORED`, `INCLUDE` indexes) are used by this port.
- **Database + connection string:** no PostgreSQL infrastructure exists yet — `AppDb_PostgreSql` ships with placeholder values; supply real Host/Username/Password via config or the existing SSM parameter path before enabling `Data:Provider=PostgreSql`.
- **Auth model:** PG string uses Username/Password (SCRAM). The SQL Server path's `Integrated Security=SSPI` (localdb) is untouched and remains an infrastructure concern for the SQL Server deployment only.
- **Schema creation:** the EF6 `DropCreateDatabaseIfModelChanges` initializer creates the PG schema on first use — no DDL script ships for PG; `db-scripts/bobs-used-bookstore-classic-db.sql` remains the SQL Server reference DDL.
- **`search_path`:** `Search Path=public` is set explicitly in the PG connection string.
- **Pooling:** Npgsql's built-in pool is active; no external pooler (PgBouncer) is configured — if one is later added in transaction mode, `max_prepared_statements` (or client-side prepared-statement disable) and `No Reset On Close=true` become prerequisites.
- **Retry on `40001`:** no serialization-failure retry paths exist in the codebase; none were added (R2). If PG runs at `REPEATABLE READ`/`SERIALIZABLE` in some environment, add retry there.
- **Connection lifecycle:** contexts are per-request (DI `InstancePerRequest`); no long-held transactions were introduced. Standard autovacuum/bloat monitoring applies (no SQL Server equivalent — new operational surface for this team).
- **Pre-existing package advisories:** Magick.NET-Q8-AnyCPU 14.6.0 carries multiple known vulnerabilities (NU1901/NU1902/NU1903) — untouched by this port, but worth a separate upgrade ticket.

## 12. Iteration history

- **Pass 0 (validate):** MSBuild `/restore` failed with CS0246/CS1503 in `ApplicationDbContext.cs` — (1) missing `using Bookstore.Domain;` for the `Entity` base type in the new helper signatures; (2) `EntityTypeConfiguration<TEntity>` is invariant, so the xmin/timestamptz helpers had to become generic (`where TEntity : Entity`). A second issue surfaced from the build's MSB3247 warning: the Npgsql `PublicKeyToken` in the new `DbProviderFactories`/Reference entries was wrong — corrected to `5d8b90d52f46fda7` (verified from the build's own conflict message) and an Npgsql binding redirect added to Web.config. All fixes re-entered execute and were re-logged.
- **Pass 1 (validate, this report):** full solution builds clean; confidence 94; 3 flags carried to §9.

## Prior-run artifacts cited

- `artifacts/general/...3cd05ae6.../plan.md`, `decisions.json`, `DUAL_DB_PORT_REPORT.md`, `PORTING_AGENT_MEMORY.md` (prior dual-db-port run on this same repo, PR #9) — the approved minimal EF6-provider seam shape, the R9 option-(b) xmin mapping, and the Npgsql 4.1.14 NU1903 fix all follow that run's validated precedent.
- `artifacts/general/...499021d5.../REPORT.md` and `artifacts/general/...fdd001ab.../REPORT.md` — confirmed the `BookstoreDatabaseConnection` naming deviation and the packages.config-era Web-reference requirement (fdd001ab added the same Web.csproj references).

---
*Confidence: 94 (formula above). Iteration: 1. Generated by porting-agent-wizard run fc370422-db45-4080-bee7-621f5e3c7f88; full per-item checklist detail in the agent-state dir's `validation.json`.*