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
            string value;
            return Instance._appSettings.TryGetValue("Data:Provider", out value) && !string.IsNullOrWhiteSpace(value)
                ? ParseDatabaseProvider(value)
                : DatabaseProvider.SqlServer;
        }

        private static DatabaseProvider ParseDatabaseProvider(string value)
        {
            DatabaseProvider parsed;

            if (!Enum.TryParse(value, ignoreCase: true, result: out parsed) || !Enum.IsDefined(typeof(DatabaseProvider), parsed))
            {
                throw new InvalidOperationException($"Invalid Data:Provider value '{value}'. Valid values: SqlServer, PostgreSql.");
            }

            return parsed;
        }

    }
}
