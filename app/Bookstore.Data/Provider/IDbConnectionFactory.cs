using System.Data.Common;
using System.Data.SqlClient;
using Npgsql;

namespace Bookstore.Data.Provider
{
    /// <summary>
    /// Creates open connections for the active database engine. One
    /// implementation per engine; the caller never knows which engine it is
    /// talking to.
    /// </summary>
    public interface IDbConnectionFactory
    {
        DbConnection CreateOpenConnection();
    }

    public sealed class SqlServerConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlServerConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbConnection CreateOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }

    public sealed class PostgreSqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public PostgreSqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbConnection CreateOpenConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
