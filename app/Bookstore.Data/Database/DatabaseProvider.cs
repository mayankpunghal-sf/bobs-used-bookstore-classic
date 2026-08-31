namespace Bookstore.Data
{
    /// <summary>
    /// The database engine the application talks to. Resolved once at process start
    /// from configuration; never re-evaluated on query paths.
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,

        PostgreSql
    }
}
