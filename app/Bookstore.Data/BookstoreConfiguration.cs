using Microsoft.Extensions.Configuration;

namespace BobsBookstoreClassic.Data
{
    public sealed class BookstoreConfiguration
    {
        private static BookstoreConfiguration? _instance;

        private readonly Dictionary<string, string> _appSettings = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _connectionStrings = new(StringComparer.OrdinalIgnoreCase);

        private BookstoreConfiguration(IConfiguration configuration)
        {
            // Load all key-value pairs, normalising ':' separator to '/'
            foreach (var kvp in configuration.AsEnumerable())
            {
                if (kvp.Value != null)
                {
                    _appSettings[kvp.Key.Replace(":", "/")] = kvp.Value;
                }
            }

            // Load connection strings under their short names
            var connSection = configuration.GetSection("ConnectionStrings");
            foreach (var child in connSection.GetChildren())
            {
                if (child.Value != null)
                    _connectionStrings[child.Key] = child.Value;
            }
        }

        public static void Initialize(IConfiguration configuration)
        {
            _instance = new BookstoreConfiguration(configuration);
        }

        private static BookstoreConfiguration Instance =>
            _instance ?? throw new InvalidOperationException(
                "BookstoreConfiguration has not been initialised. Call BookstoreConfiguration.Initialize(configuration) first.");

        public static void AddSetting(string key, string value)
        {
            // Support "ConnectionStrings/Key" by routing to connection-string store
            const string csPrefix = "ConnectionStrings/";
            if (key.StartsWith(csPrefix, StringComparison.OrdinalIgnoreCase))
                Instance._connectionStrings[key.Substring(csPrefix.Length)] = value;
            else
                Instance._appSettings[key] = value;
        }

        public static string GetSetting(string key)
        {
            return Instance._appSettings.TryGetValue(key, out var v) ? v : string.Empty;
        }

        public static T GetSetting<T>(string key)
        {
            var value = GetSetting(key);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void AddConnectionString(string key, string value)
        {
            Instance._connectionStrings[key] = value;
        }

        public static string GetConnectionString(string key)
        {
            return Instance._connectionStrings.TryGetValue(key, out var v) ? v : string.Empty;
        }
    }
}
