using System;
using System.Collections.Generic;
using System.Configuration;

namespace BobsBookstoreClassic.Data
{
    /// <summary>
    /// Database engine backing the bookstore. Resolved once at startup from the
    /// 'Data:Provider' appSetting (default SqlServer) — dual-db-port R7.
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSql
    }

    public sealed class BookstoreConfiguration
    {
        private static readonly Lazy<BookstoreConfiguration> Lazy = new Lazy<BookstoreConfiguration>(() => new BookstoreConfiguration());

        private static BookstoreConfiguration Instance => Lazy.Value;

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

        /// <summary>
        /// Resolves the configured database provider from the 'Data:Provider'
        /// appSetting (default SqlServer). Read once per composition root start.
        /// </summary>
        public static DatabaseProvider GetDatabaseProvider()
        {
            string value;
            return Instance._appSettings.TryGetValue("Data:Provider", out value)
                && string.Equals(value, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                ? DatabaseProvider.PostgreSql
                : DatabaseProvider.SqlServer;
        }

    }
}
