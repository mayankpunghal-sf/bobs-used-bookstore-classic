using System;
using System.Collections.Generic;
using BobsBookstoreClassic.Data;

namespace Bookstore.Data
{
    /// <summary>
    /// The database engine backing this deployment. Resolved exactly once at
    /// startup from configuration; never sniffed from connection strings and
    /// never re-checked at query time.
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSql
    }

    /// <summary>
    /// Single resolution point for the engine switch (config key
    /// "Data:Provider"). Missing or invalid values fail fast naming the key
    /// and the allowed values — there is no default engine and no
    /// auto-detect mode. SQL Server stays the unchanged legacy path
    /// ("BookstoreDatabaseConnection"); PostgreSQL is opt-in via
    /// "BookstoreDatabaseConnection_PostgreSql".
    /// </summary>
    public static class DatabaseProviderAccessor
    {
        public const string SwitchKey = "Data:Provider";
        public const string SqlServerConnectionStringName = "BookstoreDatabaseConnection";
        public const string PostgreSqlConnectionStringName = "BookstoreDatabaseConnection_PostgreSql";

        private static readonly DatabaseProvider current = Resolve();
        private static readonly string connectionStringName = ResolveConnectionStringName(current);

        public static DatabaseProvider Current
        {
            get { return current; }
        }

        public static string ConnectionStringName
        {
            get { return connectionStringName; }
        }

        private static DatabaseProvider Resolve()
        {
            string value;

            try
            {
                value = BookstoreConfiguration.GetSetting(SwitchKey);
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidOperationException(
                    $"Missing required configuration key '{SwitchKey}'. Allowed values: 'SqlServer', 'PostgreSql'.", e);
            }

            if (string.Equals(value, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseProvider.SqlServer;
            }

            if (string.Equals(value, "PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseProvider.PostgreSql;
            }

            throw new InvalidOperationException(
                $"Invalid value '{value}' for configuration key '{SwitchKey}'. Allowed values: 'SqlServer', 'PostgreSql'.");
        }

        private static string ResolveConnectionStringName(DatabaseProvider provider)
        {
            var name = provider == DatabaseProvider.PostgreSql
                ? PostgreSqlConnectionStringName
                : SqlServerConnectionStringName;

            // Startup assertion (§6): the selected provider's connection string
            // must exist and be non-empty.
            string connectionString;

            try
            {
                connectionString = BookstoreConfiguration.GetConnectionString(name);
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidOperationException(
                    $"No connection string named '{name}' is configured for database provider '{provider}'.", e);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"The connection string '{name}' for database provider '{provider}' is empty.");
            }

            return name;
        }
    }
}
