using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using Npgsql;

namespace Bookstore.Data
{
    public enum DatabaseProvider
    {
        SqlServer,

        PostgreSql
    }

    // Resolved once at startup into a typed value; never sniffed from the
    // connection string or type-checked at query time.
    public interface IDatabaseProviderAccessor
    {
        DatabaseProvider Provider { get; }
    }

    public sealed class DatabaseProviderAccessor : IDatabaseProviderAccessor
    {
        private static readonly Lazy<DatabaseProviderAccessor> Lazy = new Lazy<DatabaseProviderAccessor>(() => new DatabaseProviderAccessor());

        public static DatabaseProviderAccessor Instance => Lazy.Value;

        public DatabaseProvider Provider { get; }

        private DatabaseProviderAccessor()
        {
            string raw = null;

            try
            {
                raw = BobsBookstoreClassic.Data.BookstoreConfiguration.GetSetting("Data:Provider");
            }
            catch (KeyNotFoundException)
            {
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException("Missing appSetting 'Data:Provider'. Allowed values: 'SqlServer', 'PostgreSql'.");
            }

            DatabaseProvider provider;

            if (!Enum.TryParse(raw.Trim(), true, out provider) || !Enum.IsDefined(typeof(DatabaseProvider), provider))
            {
                throw new InvalidOperationException($"Invalid appSetting 'Data:Provider' value '{raw}'. Allowed values: 'SqlServer', 'PostgreSql'.");
            }

            Provider = provider;
        }
    }

    public interface IDbConnectionFactory
    {
        DbConnection CreateConnection();
    }

    public sealed class DatabaseConnectionFactory : IDbConnectionFactory
    {
        private readonly IDatabaseProviderAccessor providerAccessor;

        public DatabaseConnectionFactory(IDatabaseProviderAccessor providerAccessor)
        {
            this.providerAccessor = providerAccessor;
        }

        public DbConnection CreateConnection()
        {
            switch (providerAccessor.Provider)
            {
                case DatabaseProvider.SqlServer:
                    return new SqlConnection(GetRequiredConnectionString("BookstoreDatabaseConnection"));

                case DatabaseProvider.PostgreSql:
                    return new NpgsqlConnection(GetRequiredConnectionString("AppDb_PostgreSql"));

                default:
                    throw new InvalidOperationException($"Unknown database provider '{providerAccessor.Provider}'. Allowed values: 'SqlServer', 'PostgreSql'.");
            }
        }

        private static string GetRequiredConnectionString(string name)
        {
            var value = BobsBookstoreClassic.Data.BookstoreConfiguration.GetConnectionString(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Connection string '{name}' is missing or empty for the selected database provider.");
            }

            return value;
        }
    }
}
