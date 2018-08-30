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
    /// Maintains state about each in-flight request.
    /// </summary>
    internal class RequestManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LoggingModule _Logging; 
        private ConcurrentDictionary<int, Request> _Requests; 

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Construct the request manager.
        /// </summary>
        public RequestManager()
        {

        }

        /// <summary>
        /// Construct the request manager.
        /// </summary>
        /// <param name="logging">Logging module instance.</param>
        public RequestManager(LoggingModule logging)
        {
            _Logging = logging;
            _Requests = new ConcurrentDictionary<int, Request>();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a request to the request manager.
        /// </summary>
        /// <param name="threadId">Thread ID or other unique ID.</param>
        /// <param name="req">The request object.</param>
        public void Add(int threadId, Request req)
        {
            _Requests.TryAdd(threadId, req);
        }

        /// <summary>
        /// Remove a request from the request manager.
        /// </summary>
        /// <param name="threadId">Thread ID or other unique ID.</param>
        public void Remove(int threadId)
        {
            Request val;
            _Requests.TryRemove(threadId, out val);
        }
        
        /// <summary>
        /// Retrieve the list of open requests.
        /// </summary>
        /// <returns>Dictionary containing thread ID or other unique ID and the request itself.</returns>
        public Dictionary<int, Request> Get()
        {
            return new Dictionary<int, Request>(_Requests);
        }

        #endregion

        #region Private-Methods

        private string Key(Tunnel curr)
        {
            return curr.SourceIp + ":" + curr.SourcePort + "->" + curr.DestIp + ":" + curr.DestPort;
        }

        #endregion
    }
}
