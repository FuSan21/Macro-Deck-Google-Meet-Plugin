using Newtonsoft.Json;
using SuchByte.MacroDeck.Plugins;

namespace FuSan21.MacroDeck.GoogleMeet
{
    public class Configuration
    {
        /// <summary>
        /// The port ChrisRegado's extension dials. Changing it here means changing it in the
        /// extension too, so the only reason to move is a clash with something else on the
        /// machine — most likely his own Stream Deck plugin, which owns the same number.
        /// </summary>
        public const int DefaultPort = 2394;

        [JsonIgnore]
        private readonly GoogleMeetPlugin _plugin;

        /// <summary>
        /// Whether the integration runs at all. Turning it off closes the socket and clears
        /// the variables, without having to uninstall the plugin.
        /// </summary>
        public bool Enabled { get; set; } = true;

        public int Port { get; set; } = DefaultPort;

        public Configuration(GoogleMeetPlugin plugin)
        {
            if (plugin != null)
            {
                _plugin = plugin;
                Reload();
            }
        }

        public void Save()
        {
            PluginConfiguration.SetValue(_plugin, "config", JsonConvert.SerializeObject(this));
        }

        public void Reload()
        {
            var json = PluginConfiguration.GetValue(_plugin, "config");
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                var config = JsonConvert.DeserializeObject<Configuration>(json);
                if (config == null)
                {
                    return;
                }

                Enabled = config.Enabled;
                Port = config.Port > 0 && config.Port <= 65535 ? config.Port : DefaultPort;
            }
            catch { }
        }
    }
}
