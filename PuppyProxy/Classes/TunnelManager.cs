using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyslogLogging;

namespace PuppyProxy
{   
    /// <summary>
    /// Maintains state about each active tunnel.
    /// </summary>
    internal class TunnelManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LoggingModule _Logging; 
        private Dictionary<int, Tunnel> Tunnels;
        private readonly object TunnelsLock;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Construct the tunnel manager.
        /// </summary>
        public TunnelManager()
        {

        }

        /// <summary>
        /// Construct the tunnel manager.
        /// </summary>
        /// <param name="logging">Logging module instance.</param>
        public TunnelManager(LoggingModule logging)
        {
            _Logging = logging;
            Tunnels = new Dictionary<int, Tunnel>();
            TunnelsLock = new object();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a tunnel.
        /// </summary>
        /// <param name="threadId">Thread ID or other unique ID.</param>
        /// <param name="curr">Tunnel object.</param>
        public void Add(int threadId, Tunnel curr)
        {
            lock (TunnelsLock)
            {
                if (Tunnels.ContainsKey(threadId)) Tunnels.Remove(threadId);
                Tunnels.Add(threadId, curr);
            }
        }

        /// <summary>
        /// Remove a tunnel.
        /// </summary>
        /// <param name="threadId">Thread ID or other unique ID.</param>
        public void Remove(int threadId)
        {
            lock (TunnelsLock)
            {
                if (Tunnels.ContainsKey(threadId))
                {
                    Tunnel curr = Tunnels[threadId];
                    Tunnels.Remove(threadId);
                    curr.Dispose();
                }
            }
        }

        /// <summary>
        /// Retrieve metadata about active tunnels.
        /// </summary>
        /// <returns>Dictionary with thread ID or other unique ID and tunnel metadata.</returns>
        public Dictionary<int, Tunnel> GetMetadata()
        {
            Dictionary<int, Tunnel> ret = new Dictionary<int, Tunnel>();

            lock (TunnelsLock)
            {
                foreach (KeyValuePair<int, Tunnel> curr in Tunnels)
                {
                    ret.Add(curr.Key, curr.Value.Metadata());
                }
            }

            return ret;
        }

        /// <summary>
        /// Retrieve the full tunnel dictionary including TCP and stream objects.
        /// </summary>
        /// <returns>Dictionary with thread ID or other unique ID and tunnel objects.</returns>
        public Dictionary<int, Tunnel> GetFull()
        {
            Dictionary<int, Tunnel> ret = new Dictionary<int, Tunnel>();

            lock (TunnelsLock)
            {
                ret = new Dictionary<int, Tunnel>(Tunnels);
            }

            return ret;
        }

        /// <summary>
        /// Determine if a tunnel is active by thread ID or other unique ID.
        /// </summary>
        /// <param name="threadId">Thread ID or other unique ID.</param>
        /// <returns>True if the tunnel is active.</returns>
        public bool Active(int threadId)
        {
            lock (TunnelsLock)
            {
                if (Tunnels.ContainsKey(threadId)) return true;
            }

            return false;
        }

        /// <summary>
        /// Determine the number of active tunnels.
        /// </summary>
        /// <returns>Integer.</returns>
        public int Count()
        {
            lock (TunnelsLock)
            {
                return Tunnels.Count;
            }
        }

        #endregion

        #region Private-Methods
         
        #endregion
    }
}
