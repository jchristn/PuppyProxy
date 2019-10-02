using System;
using System.IO;
using System.Text;
using WatsonWebserver;

namespace PuppyProxy
{
    /// <summary>
    /// System settings.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Operating environment, either 'windows' or 'linux'.  Reserved for future use.
        /// </summary>
		public string Environment { get; set; }

        /// <summary>
        /// Enable or disable the console.
        /// </summary>
        public bool EnableConsole { get; set; }
         
        /// <summary>
        /// Logging settings.
        /// </summary>
        public SettingsLogging Logging { get; set; }

        /// <summary>
        /// Proxy server settings.
        /// </summary>
        public SettingsProxy Proxy { get; set; }

        /// <summary>
        /// Default values for system settings.
        /// </summary>
        /// <returns>Settings object.</returns>
        public static Settings Default() 
        {
            Settings ret = new Settings();
            ret.Environment = "linux";
            ret.EnableConsole = true; 
            ret.Logging = SettingsLogging.Default();
            ret.Proxy = SettingsProxy.Default();
            return ret;
        }

        /// <summary>
        /// Load system settings from file.
        /// </summary>
        /// <param name="filename">The file from which to load.</param>
        /// <returns>Settings object.</returns>
        public static Settings FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            Settings ret = Settings.Default();

            if (!File.Exists(filename))
            {
                Console.WriteLine("Creating default configuration in " + filename);
                File.WriteAllBytes(filename, Encoding.UTF8.GetBytes(Common.SerializeJson(ret, true)));
                return ret;
            }
            else
            {
                ret = Common.DeserializeJson<Settings>(File.ReadAllBytes(filename));
                return ret;
            }
        }
	}
     
    /// <summary>
    /// Logging settings.
    /// </summary>
    public class SettingsLogging
    {
        /// <summary>
        /// Enable or disable logging to syslog.
        /// </summary>
        public bool SyslogEnable { get; set; }

        /// <summary>
        /// Enable or disable logging to the console.
        /// </summary>
        public bool ConsoleEnable { get; set; }

        /// <summary>
        /// Minimum severity required before sending a log message (0 through 7).
        /// </summary>
        public int MinimumLevel { get; set; }

        /// <summary>
        /// IP address of the syslog server.
        /// </summary>
        public string SyslogServerIp { get; set; }

        /// <summary>
        /// UDP port on which the syslog server is listening.
        /// </summary>
        public int SyslogServerPort { get; set; }

        /// <summary>
        /// Default values for logging settings.
        /// </summary>
        /// <returns>SettingsLogging object.</returns>
        public static SettingsLogging Default()
        {
            SettingsLogging ret = new SettingsLogging();
            ret.SyslogEnable = true;
            ret.ConsoleEnable = true;
            ret.MinimumLevel = 0;
            ret.SyslogServerIp = "127.0.0.1";
            ret.SyslogServerPort = 514;
            return ret;
        }
    }

    /// <summary>
    /// Proxy server settings.
    /// </summary>
    public class SettingsProxy
    {
        /// <summary>
        /// Enable or disable connections to sites with certificates that cannot be validated.
        /// </summary>
        public bool AcceptInvalidCertificates { get; set; }

        /// <summary>
        /// The TCP port on which to listen.
        /// </summary>
        public int ListenerPort { get; set; }

        /// <summary>
        /// The DNS hostname or IP address on which to listen.
        /// </summary>
        public string ListenerIpAddress { get; set; }

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl { get; set; }

        /// <summary>
        /// Maximum number of threads to support.
        /// </summary>
        public int MaxThreads { get; set; }
         
        /// <summary>
        /// Default values for proxy server settings.
        /// </summary>
        /// <returns>SettingsProxy object.</returns>
        public static SettingsProxy Default()
        {
            SettingsProxy ret = new SettingsProxy();
            ret.AcceptInvalidCertificates = true;
            ret.ListenerPort = 8000;
            ret.ListenerIpAddress = "0.0.0.0";
            ret.Ssl = false;
            ret.MaxThreads = 100; 
            return ret;
        }
    }
}
