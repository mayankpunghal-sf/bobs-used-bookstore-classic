using System;
using BobsBookstoreClassic.Data;

namespace Bookstore.Data.Provider
{
    /// <summary>
    /// The database engines this application can run against. Resolved once at
    /// startup from configuration (appSetting "Data:Provider"); never sniffed
    /// from a connection string at runtime.
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSql
    }

    /// <summary>
    /// Resolves the active <see cref="DatabaseProvider"/>. Implementations must
    /// fail fast on a missing or unknown configuration value - never silently
    /// defaulting to one engine.
    /// </summary>
    public interface IDatabaseProviderAccessor
    {
        DatabaseProvider Provider { get; }
    }

    /// <summary>
    /// Reads the provider switch once from configuration. The value is fixed
    /// for the lifetime of the process (resolved in the constructor).
    /// </summary>
    public sealed class ConfigDatabaseProviderAccessor : IDatabaseProviderAccessor
    {
        private readonly DatabaseProvider _provider;

        public ConfigDatabaseProviderAccessor()
        {
            var raw = BookstoreConfiguration.GetSetting("Data:Provider");

            if (string.Equals(raw, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                _provider = DatabaseProvider.SqlServer;
            }
            else if (string.Equals(raw, "PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                _provider = DatabaseProvider.PostgreSql;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown Data:Provider value '{raw}'. Expected 'SqlServer' or 'PostgreSql'.");
            }
        }

        public DatabaseProvider Provider => _provider;
    }
}
