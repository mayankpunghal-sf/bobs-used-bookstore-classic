using System;

namespace Bookstore.Data
{
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSql
    }

    /// <summary>
    /// Resolves the target database engine once at startup from the appSettings key
    /// "Data/Provider" (allowed values: SqlServer | PostgreSql). The resolved value is
    /// immutable for the life of the process. A missing or invalid value fails fast:
    /// no default engine is applied and the connection string is never sniffed.
    /// </summary>
    public sealed class DatabaseProviderAccessor
    {
        public const string ConfigKey = "Data/Provider";

        public DatabaseProviderAccessor(DatabaseProvider provider)
        {
            Provider = provider;
        }

        public DatabaseProvider Provider { get; }

        public string ConnectionStringName => Provider == DatabaseProvider.PostgreSql
            ? "BookstoreDatabaseConnection_PostgreSql"
            : "BookstoreDatabaseConnection";

        public string ProviderInvariant => Provider == DatabaseProvider.PostgreSql
            ? "Npgsql"
            : "System.Data.SqlClient";

        public static DatabaseProvider Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Missing required appSetting '{ConfigKey}'. Allowed values: SqlServer, PostgreSql. No default engine is applied.");
            }

            var trimmed = value.Trim();

            if (string.Equals(trimmed, nameof(DatabaseProvider.SqlServer), StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseProvider.SqlServer;
            }

            if (string.Equals(trimmed, nameof(DatabaseProvider.PostgreSql), StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseProvider.PostgreSql;
            }

            throw new InvalidOperationException(
                $"Invalid appSetting '{ConfigKey}' value '{value}'. Allowed values: SqlServer, PostgreSql.");
        }
    }
}
