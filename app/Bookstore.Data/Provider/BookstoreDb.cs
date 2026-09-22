using System;
using BobsBookstoreClassic.Data;

namespace Bookstore.Data.Provider
{
    /// <summary>
    /// Single entry point for everything engine-specific. The provider is
    /// resolved once from configuration; every member returns the
    /// implementation matching that provider for the lifetime of the process.
    /// </summary>
    public static class BookstoreDb
    {
        private static readonly IDatabaseProviderAccessor Accessor = new ConfigDatabaseProviderAccessor();

        private static readonly Lazy<IDbConnectionFactory> LazyConnections =
            new Lazy<IDbConnectionFactory>(CreateConnectionFactory);

        private static readonly Lazy<ISqlDialect> LazyDialect =
            new Lazy<ISqlDialect>(CreateDialect);

        private static readonly Lazy<ISqlQueryProvider> LazyQueries =
            new Lazy<ISqlQueryProvider>(() => new EmbeddedSqlQueryProvider());

        public static DatabaseProvider Provider => Accessor.Provider;

        public static IDbConnectionFactory Connections => LazyConnections.Value;

        public static ISqlDialect Dialect => LazyDialect.Value;

        public static ISqlQueryProvider Queries => LazyQueries.Value;

        private static string ConnectionStringKey =>
            Provider == DatabaseProvider.PostgreSql
                ? "BookstoreDatabaseConnection_PostgreSql"
                : "BookstoreDatabaseConnection";

        private static IDbConnectionFactory CreateConnectionFactory()
        {
            var connectionString = BookstoreConfiguration.GetConnectionString(ConnectionStringKey);

            if (Provider == DatabaseProvider.PostgreSql)
            {
                return new PostgreSqlConnectionFactory(connectionString);
            }

            return new SqlServerConnectionFactory(connectionString);
        }

        private static ISqlDialect CreateDialect()
        {
            if (Provider == DatabaseProvider.PostgreSql)
            {
                return new PostgreSqlDialect();
            }

            return new SqlServerDialect();
        }
    }
}
