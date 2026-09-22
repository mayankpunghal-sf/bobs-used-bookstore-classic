using System.Linq;

namespace Bookstore.Data.Provider
{
    /// <summary>
    /// The engine-specific surface every data-access feature needs: identifier
    /// quoting, current-UTC expression, paging, pattern matching, identity
    /// retrieval, upsert, IN-lists, schema naming, and native error-code
    /// mapping to stable logical codes.
    /// </summary>
    public interface ISqlDialect
    {
        /// <summary>Quotes an identifier for the active engine ([x] vs "x").</summary>
        string QuoteIdentifier(string identifier);

        /// <summary>Expression producing the current UTC timestamp.</summary>
        string UtcNowExpression();

        /// <summary>OFFSET/FETCH paging clause fragment.</summary>
        string PageClause(int offset, int fetch);

        /// <summary>LIKE (SQL Server) vs ILIKE (PostgreSQL) pattern-match keyword.</summary>
        string LikeKeyword { get; }

        /// <summary>Wraps an INSERT so it returns the generated id column's value.</summary>
        string InsertReturningId(string insertSql, string idColumn);

        /// <summary>Renders an upsert (MERGE / INSERT ... ON CONFLICT) for the given table.</summary>
        string UpsertSql(string table, string keyColumns, string updateColumns);

        /// <summary>Renders an IN-list with the given number of parameters.</summary>
        string InList(string parameterPrefix, int count);

        /// <summary>Default schema name for the active engine (dbo / public).</summary>
        string DefaultSchema { get; }

        /// <summary>
        /// Maps a native engine error code to a stable logical code:
        /// unique violation 23505, deadlock 40P01, serialization 40001,
        /// foreign-key violation 23503. Unmapped codes return null.
        /// SQL Server error numbers and PG SQLSTATEs are both passed as
        /// strings (SQLSTATEs are not numeric).
        /// </summary>
        string MapErrorCode(string nativeErrorCode);
    }

    public sealed class SqlServerDialect : ISqlDialect
    {
        public string QuoteIdentifier(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

        public string UtcNowExpression() => "SYSUTCDATETIME()";

        public string PageClause(int offset, int fetch) =>
            $"OFFSET {offset} ROWS FETCH NEXT {fetch} ROWS ONLY";

        public string LikeKeyword => "LIKE";

        public string InsertReturningId(string insertSql, string idColumn) =>
            insertSql + "; SELECT SCOPE_IDENTITY();";

        public string UpsertSql(string table, string keyColumns, string updateColumns) =>
            $"MERGE INTO {table} WITH (HOLDLOCK) AS target USING (SELECT 1) AS source " +
            $"ON {keyColumns} WHEN MATCHED THEN UPDATE SET {updateColumns} " +
            "WHEN NOT MATCHED THEN INSERT DEFAULT VALUES;";

        public string InList(string parameterPrefix, int count) =>
            "IN (" + string.Join(", ", System.Linq.Enumerable.Range(0, count).Select(i => "@" + parameterPrefix + i)) + ")";

        public string DefaultSchema => "dbo";

        public string MapErrorCode(string nativeErrorCode)
        {
            switch (nativeErrorCode)
            {
                case "2627": // unique constraint violation
                case "2601": // duplicate key
                    return "23505";
                case "1205": // deadlock victim
                    return "40P01";
                case "40001": // serialization failure
                    return "40001";
                case "547": // foreign-key violation
                    return "23503";
                default:
                    return null;
            }
        }
    }

    public sealed class PostgreSqlDialect : ISqlDialect
    {
        public string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

        public string UtcNowExpression() => "NOW() AT TIME ZONE 'UTC'";

        public string PageClause(int offset, int fetch) =>
            $"LIMIT {fetch} OFFSET {offset}";

        public string LikeKeyword => "ILIKE";

        public string InsertReturningId(string insertSql, string idColumn) =>
            insertSql + $" RETURNING {QuoteIdentifier(idColumn)}";

        public string UpsertSql(string table, string keyColumns, string updateColumns) =>
            $"INSERT INTO {table} ON CONFLICT ({keyColumns}) DO UPDATE SET {updateColumns}";

        public string InList(string parameterPrefix, int count) =>
            "IN (" + string.Join(", ", System.Linq.Enumerable.Range(0, count).Select(i => "@" + parameterPrefix + i)) + ")";

        public string DefaultSchema => "public";

        public string MapErrorCode(string nativeErrorCode)
        {
            // PostgreSQL surfaces SQLSTATE codes natively as strings; they are
            // already the stable logical codes this contract speaks.
            switch (nativeErrorCode)
            {
                case "23505":
                case "40P01":
                case "40001":
                case "23503":
                    return nativeErrorCode;
                default:
                    return null;
            }
        }
    }
}
