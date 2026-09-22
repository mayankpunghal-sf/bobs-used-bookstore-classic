using System;
using System.Collections.Generic;
using System.Configuration;

namespace BobsBookstoreClassic.Data
{
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSql
    }

    public sealed class BookstoreConfiguration
    {
        private static readonly Lazy<BookstoreConfiguration> Lazy = new Lazy<BookstoreConfiguration>(() => new BookstoreConfiguration());

        private static BookstoreConfiguration Instance => Lazy.Value;

        // Resolved once at startup into an immutable per-process value; never re-read at query time.
        private static readonly Lazy<DatabaseProvider> LazyDatabaseProvider = new Lazy<DatabaseProvider>(ResolveDatabaseProviderOnce);

        private readonly Dictionary<string, string> _appSettings = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();

        private BookstoreConfiguration()
        {
            foreach (string key in ConfigurationManager.AppSettings)
            {
                _appSettings[key] = ConfigurationManager.AppSettings[key];

                if (Environment.GetEnvironmentVariable(key) != null)
                {
                    _appSettings[key] = Environment.GetEnvironmentVariable(key);
                }
            }

            foreach (ConnectionStringSettings connectionStringSettings in ConfigurationManager.ConnectionStrings)
            {
                _connectionStrings[connectionStringSettings.Name] = connectionStringSettings.ConnectionString;

            }
        }

        public static void AddSetting(string key, string value)
        {
            Instance._appSettings[key] = value;
        }

        public static string GetSetting(string key)
        {
            return Instance._appSettings[key];
        }

        public static T GetSetting<T>(string key)
        {
            var value = Instance._appSettings[key];

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void AddConnectionString(string key, string value)
        {
            Instance._connectionStrings[key] = value;
        }

        public static string GetConnectionString(string key)
        {
            return Instance._connectionStrings[key];
        }

        public static DatabaseProvider GetDatabaseProvider()
        {
            return LazyDatabaseProvider.Value;
        }

        private static DatabaseProvider ResolveDatabaseProviderOnce()
        {
            string value;

            if (!Instance._appSettings.TryGetValue("Data:Provider", out value) || string.IsNullOrWhiteSpace(value))
            {
                // No switch configured: keep existing SQL Server deployments on their current engine.
                return DatabaseProvider.SqlServer;
            }

            if (string.Equals(value, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseProvider.SqlServer;
            }

            if (string.Equals(value, "PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseProvider.PostgreSql;
            }

            throw new ConfigurationErrorsException("Invalid value '" + value + "' for appSettings key 'Data:Provider'. Allowed values: SqlServer, PostgreSql.");
        }

    }
}
