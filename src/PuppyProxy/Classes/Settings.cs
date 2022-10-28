using System;
using System.IO;
using System.Net;
using System.Text;
using WatsonWebserver;

namespace PuppyProxy
{
    /// <summary>
    /// System settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable the console.
        /// </summary>
        public bool EnableConsole { get; set; } = true;
         
        /// <summary>
        /// Logging settings.
        /// </summary>
        public SettingsLogging Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) _Logging = new SettingsLogging();
                else _Logging = value;
            }
        }

        /// <summary>
        /// Proxy server settings.
        /// </summary>
        public SettingsProxy Proxy
        {
            get
            {
                return _Proxy;
            }
            set
            {
                if (value == null) _Proxy = new SettingsProxy();
                else _Proxy = value;
            }
        }

        #endregion

        #region Private-Members

        private SettingsLogging _Logging = new SettingsLogging();
        private SettingsProxy _Proxy = new SettingsProxy();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Load system settings from file.
        /// </summary>
        /// <param name="filename">The file from which to load.</param>
        /// <returns>Settings object.</returns>
        public static Settings FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            Settings ret = new Settings();

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

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }

    /// <summary>
    /// Logging settings.
    /// </summary>
    public class SettingsLogging
    {
        /// <summary>
        /// Enable or disable logging to syslog.
        /// </summary>
        public bool SyslogEnable { get; set; } = true;

        /// <summary>
        /// Enable or disable logging to the console.
        /// </summary>
        public bool ConsoleEnable { get; set; } = true;

        /// <summary>
        /// Minimum severity required before sending a log message (0 through 7).
        /// </summary>
        public int MinimumLevel
        {
            get
            {
                return _MinimumLevel;
            }
            set
            {
                if (value < 0 || value > 7) throw new ArgumentOutOfRangeException(nameof(MinimumLevel));
            }
        }

        /// <summary>
        /// IP address of the syslog server.
        /// </summary>
        public string SyslogServerIp { get; set; } = "127.0.0.1";

        /// <summary>
        /// UDP port on which the syslog server is listening.
        /// </summary>
        public int SyslogServerPort
        {
            get
            {
                return _SyslogServerPort;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(SyslogServerPort));
            }
        }

        private int _MinimumLevel = 0;
        private int _SyslogServerPort = 514;
    }

    /// <summary>
    /// Proxy server settings.
    /// </summary>
    public class SettingsProxy
    {
        /// <summary>
        /// Enable or disable connections to sites with certificates that cannot be validated.
        /// </summary>
        public bool AcceptInvalidCertificates { get; set; } = true;

        /// <summary>
        /// The TCP port on which to listen.
        /// </summary>
        public int ListenerPort
        {
            get
            {
                return _ListenerPort;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(ListenerPort));
            }
        }

        /// <summary>
        /// The DNS hostname or IP address on which to listen.
        /// </summary>
        public string ListenerIpAddress
        {
            get
            {
                return _ListenerIpAddress;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(ListenerIpAddress));
                _ListenerIpAddress = IPAddress.Parse(value).ToString();
            }
        }

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl { get; set; } = false;

        /// <summary>
        /// Maximum number of threads to support.
        /// </summary>
        public int MaxThreads
        {
            get
            {
                return _MaxThreads;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxThreads));
            }
        }

        private int _ListenerPort = 8000;
        private int _MaxThreads = 256;
        private string _ListenerIpAddress = "127.0.0.1";
    }
}
