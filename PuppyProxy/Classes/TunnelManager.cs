using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyslogLogging;

namespace PuppyProxy
{    
    internal class TunnelManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LoggingModule _Logging;
        private Dictionary<int, Tunnel> _Tunnels = new Dictionary<int, Tunnel>();
        private readonly object _TunnelsLock = new object();

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
        }

        #endregion

        #region Internal-Methods
         
        internal void Add(int threadId, Tunnel curr)
        {
            lock (_TunnelsLock)
            {
                if (_Tunnels.ContainsKey(threadId)) _Tunnels.Remove(threadId);
                _Tunnels.Add(threadId, curr);
            }
        }
         
        internal void Remove(int threadId)
        {
            lock (_TunnelsLock)
            {
                if (_Tunnels.ContainsKey(threadId))
                {
                    Tunnel curr = _Tunnels[threadId];
                    _Tunnels.Remove(threadId);
                    curr.Dispose();
                }
            }
        }
         
        internal Dictionary<int, Tunnel> GetMetadata()
        {
            Dictionary<int, Tunnel> ret = new Dictionary<int, Tunnel>();

            lock (_TunnelsLock)
            {
                foreach (KeyValuePair<int, Tunnel> curr in _Tunnels)
                {
                    ret.Add(curr.Key, curr.Value.Metadata());
                }
            }

            return ret;
        }
         
        internal Dictionary<int, Tunnel> GetFull()
        {
            Dictionary<int, Tunnel> ret = new Dictionary<int, Tunnel>();

            lock (_TunnelsLock)
            {
                ret = new Dictionary<int, Tunnel>(_Tunnels);
            }

            return ret;
        }
         
        internal bool Active(int threadId)
        {
            lock (_TunnelsLock)
            {
                if (_Tunnels.ContainsKey(threadId)) return true;
            }

            return false;
        }
         
        internal int Count()
        {
            lock (_TunnelsLock)
            {
                return _Tunnels.Count;
            }
        }

        #endregion

        #region Private-Methods
         
        #endregion
    }
}
