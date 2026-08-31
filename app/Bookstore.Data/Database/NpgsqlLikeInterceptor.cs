using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Text.RegularExpressions;

namespace Bookstore.Data
{
    /// <summary>
    /// EF6 LINQ translates string.Contains/StartsWith/EndsWith into SQL LIKE. On SQL Server
    /// LIKE is case-insensitive under the database's default collation; on PostgreSQL LIKE
    /// is case-sensitive and ILIKE is the case-insensitive form. This interceptor rewrites
    /// the standalone LIKE keyword to ILIKE so user-facing search behaves identically on
    /// both engines (Appendix B6/B7) without forking any query in repository code.
    ///
    /// It is registered with DbInterception only when the resolved provider is PostgreSQL,
    /// so the SQL Server statement stream is unchanged. EF6 routes both sync and async
    /// commands through the *Executing hooks, and search terms are always bound as
    /// parameters (never interpolated into the command text), so the rewrite only ever
    /// touches the keyword. The word-boundary pattern leaves an existing ILIKE untouched
    /// (idempotent).
    /// </summary>
    public class NpgsqlLikeInterceptor : IDbCommandInterceptor
    {
        private static readonly Regex LikeKeyword = new Regex(@"\bLIKE\b", RegexOptions.Compiled);

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Rewrite(command);
        }

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Rewrite(command);
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Rewrite(command);
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
        }

        private static void Rewrite(DbCommand command)
        {
            if (!string.IsNullOrEmpty(command.CommandText) && LikeKeyword.IsMatch(command.CommandText))
            {
                command.CommandText = LikeKeyword.Replace(command.CommandText, "ILIKE");
            }
        }
    }
}
