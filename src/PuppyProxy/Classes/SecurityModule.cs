using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebserver;
using SyslogLogging;

namespace PuppyProxy
{
    /// <summary>
    /// Security module to check and permit/deny HTTP requests.
    /// </summary>
    internal class SecurityModule
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LoggingModule _Logging;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Construct a SecurityModule object.
        /// </summary>
        /// <param name="logging">Logging module instance.</param>
        public SecurityModule(LoggingModule logging)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _Logging = logging;
        }
         
        #endregion

        #region Public-Methods
         
        /// <summary>
        /// Validate whether or not a request is permitted or not.
        /// </summary>
        /// <param name="req">HttpRequest object.</param>
        /// <param name="denyReason">The reason the request was denied.</param>
        /// <returns>True if permitted.</returns>
        public bool IsPermitted(HttpRequest req, out string denyReason)
        {
            denyReason = null;

            if (req == null) throw new ArgumentNullException(nameof(req));

            /*
             * 
             * 
             * Insert approval logic here
             * 
             * 
             * 
             */

            return true;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
