using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebserver;

namespace PuppyProxy
{
    /// <summary>
    /// HTTP request and metadata.
    /// </summary>
    internal class Request
    {
        #region Public-Members

        /// <summary>
        /// UTC timestamp when the request was received.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// The HTTP request.
        /// </summary>
        public HttpRequest Req { get; set; }
        
        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Construct a Request object.
        /// </summary>
        public Request()
        {

        }

        /// <summary>
        /// Construct a Request object.
        /// </summary>
        /// <param name="req">The HTTP request.</param>
        public Request(HttpRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Req = req;
            TimestampUtc = DateTime.Now.ToUniversalTime();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Human-readable string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            return Req.TimestampUtc.ToString("MM/dd/yyyy HH:mm:ss") + " " + Source() + " to " + Destination() + " " + Req.Method + " " + Req.FullUrl;
        }

        /// <summary>
        /// Returns the source IP and port.
        /// </summary>
        /// <returns>String.</returns>
        public string Source()
        {
            return Req.SourceIp + ":" + Req.SourcePort;
        }

        /// <summary>
        /// Returns the destination hostname and port.
        /// </summary>
        /// <returns>String.</returns>
        public string Destination()
        {
            return Req.DestHostname + ":" + Req.DestHostPort;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
