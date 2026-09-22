using System.Collections.Generic;

namespace Bookstore.Data.Provider
{
    /// <summary>
    /// Supplies engine-specific SQL text by logical key. Queries live in
    /// embedded resources under Sql/{Common,SqlServer,PostgreSql}/ - never
    /// inline in C#.
    /// </summary>
    public interface ISqlQueryProvider
    {
        string GetQuery(string key);
    }

    /// <summary>
    /// Reads SQL from embedded Sql/{Common,SqlServer,PostgreSql}/ resources.
    /// This repository contains zero inline SQL (all data access is EF6
    /// LINQ), so the provider ships with an empty registry - it exists to
    /// keep the seam in place for future SQL-bearing features without
    /// changing current behavior.
    /// </summary>
    public sealed class EmbeddedSqlQueryProvider : ISqlQueryProvider
    {
        private static readonly Dictionary<string, string> Queries = new Dictionary<string, string>();

        public string GetQuery(string key)
        {
            string value;

            if (Queries.TryGetValue(key, out value))
            {
                return value;
            }

            throw new KeyNotFoundException($"No SQL query registered for key '{key}'.");
        }
    }
}
