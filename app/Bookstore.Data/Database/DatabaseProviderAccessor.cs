using System;
using System.Data.Entity.Infrastructure.Interception;
using BobsBookstoreClassic.Data;

namespace Bookstore.Data
{
    /// <summary>
    /// Resolves the target database engine exactly once, at startup, from the
    /// "Data:Provider" application setting (allowed values: "SqlServer" | "PostgreSql").
    ///
    /// A missing or unknown value fails fast naming the key and the allowed values -
    /// there is no default engine and no runtime auto-detection.
    /// </summary>
    public static class DatabaseProviderAccessor
    {
        public const string SwitchKey = "Data:Provider";

        public const string SqlServerConnectionStringName = "BookstoreDatabaseConnection";

        public const string PostgreSqlConnectionStringName = "BookstoreDatabaseConnection_PostgreSql";

        private static readonly Lazy<DatabaseProvider> LazyProvider = new Lazy<DatabaseProvider>(Resolve);

        private static readonly Lazy<string> LazyConnectionStringName = new Lazy<string>(ResolveConnectionStringName);

        /// <summary>
        /// The provider resolved for this process. Immutable once read.
        /// </summary>
        public static DatabaseProvider Current => LazyProvider.Value;

        /// <summary>
        /// The name of the connection string entry (in config) that matches the resolved
        /// provider. The entry's providerName attribute selects the EF6 provider, so the
        /// context is constructed with this name rather than a raw connection string.
        /// </summary>
        public static string CurrentConnectionStringName => LazyConnectionStringName.Value;

        /// <summary>
        /// Startup validation and engine-specific registration. Call once at application
        /// start, after configuration is loaded. Fails fast when the selected provider's
        /// connection string is missing or empty, and registers the PostgreSQL command
        /// interceptor when (and only when) PostgreSQL is the resolved engine.
        /// </summary>
        public static void Configure()
        {
            var provider = Current;

            var connectionString = BookstoreConfiguration.GetConnectionString(CurrentConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"The connection string '{CurrentConnectionStringName}' for database provider '{provider}' is missing or empty. " +
                    $"Add a non-empty connection string entry named '{CurrentConnectionStringName}' to the <connectionStrings> section.");
            }

            if (provider == DatabaseProvider.PostgreSql)
            {
                // Registered only on the PostgreSQL path so the SQL Server command
                // stream is byte-for-byte what it was before this port.
                DbInterception.Add(new NpgsqlLikeInterceptor());
            }
        }

        private static DatabaseProvider Resolve()
        {
            string rawValue;

            try
            {
                rawValue = BookstoreConfiguration.GetSetting(SwitchKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Missing required application setting '{SwitchKey}'. Allowed values: 'SqlServer', 'PostgreSql'. There is no default engine.", ex);
            }

            if (Enum.TryParse(rawValue?.Trim(), ignoreCase: true, result: out DatabaseProvider provider))
            {
                return provider;
            }

            throw new InvalidOperationException(
                $"Invalid value '{rawValue}' for application setting '{SwitchKey}'. Allowed values: 'SqlServer', 'PostgreSql'. There is no default engine.");
        }

        private static string ResolveConnectionStringName()
        {
            return Current == DatabaseProvider.PostgreSql
                ? PostgreSqlConnectionStringName
                : SqlServerConnectionStringName;
        }
    }
}
