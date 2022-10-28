using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SyslogLogging;

namespace PuppyProxy
{ 
    /// <summary>
    /// CONNECT tunnel.
    /// </summary>
    public class Tunnel : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// UTC timestamp when the session was started.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Source IP address.
        /// </summary>
        public string SourceIp { get; set; }

        /// <summary>
        /// Source TCP port.
        /// </summary>
        public int SourcePort { get; set; } 

        /// <summary>
        /// Destination IP address.
        /// </summary>
        public string DestIp { get; set; }

        /// <summary>
        /// Destination TCP port.
        /// </summary>
        public int DestPort { get; set; }

        /// <summary>
        /// Destination hostname.
        /// </summary>
        public string DestHostname { get; set; }

        /// <summary>
        /// Destination host port.
        /// </summary>
        public int DestHostPort { get; set; }

        /// <summary>
        /// The TCP client instance for the requestor.
        /// </summary>
        public TcpClient ClientTcpClient { get; set; }

        /// <summary>
        /// The TCP client instance for the server.
        /// </summary>
        public TcpClient ServerTcpClient { get; set; }

        /// <summary>
        /// The data stream for the client.
        /// </summary>
        public Stream ClientStream { get; set; }

        /// <summary>
        /// The data stream for the server.
        /// </summary>
        public Stream ServerStream { get; set; }

        #endregion

        #region Private-Members

        private LoggingModule _Logging;
        private bool _Active = true;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Construct a Tunnel object.
        /// </summary>
        public Tunnel()
        {

        }

        /// <summary>
        /// Construct a Tunnel object.
        /// </summary>
        /// <param name="logging">Logging module instance.</param>
        /// <param name="sourceIp">Source IP address.</param>
        /// <param name="sourcePort">Source TCP port.</param>
        /// <param name="destIp">Destination IP address.</param>
        /// <param name="destPort">Destination TCP port.</param>
        /// <param name="destHostname">Destination hostname.</param>
        /// <param name="destHostPort">Destination host port.</param>
        /// <param name="client">TCP client instance of the client.</param>
        /// <param name="server">TCP client instance of the server.</param>
        public Tunnel(
            LoggingModule logging, 
            string sourceIp, 
            int sourcePort, 
            string destIp, 
            int destPort, 
            string destHostname,
            int destHostPort,
            TcpClient client, 
            TcpClient server)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (String.IsNullOrEmpty(sourceIp)) throw new ArgumentNullException(nameof(sourceIp));
            if (String.IsNullOrEmpty(destIp)) throw new ArgumentNullException(nameof(destIp));
            if (String.IsNullOrEmpty(destHostname)) throw new ArgumentNullException(nameof(destHostname));
            if (sourcePort < 0) throw new ArgumentOutOfRangeException(nameof(sourcePort));
            if (destPort < 0) throw new ArgumentOutOfRangeException(nameof(destPort));
            if (destHostPort < 0) throw new ArgumentOutOfRangeException(nameof(destHostPort));
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (server == null) throw new ArgumentNullException(nameof(server));

            _Logging = logging;
            TimestampUtc = DateTime.Now.ToUniversalTime();
            SourceIp = sourceIp;
            SourcePort = sourcePort; 
            DestIp = destIp;
            DestPort = destPort;
            DestHostname = destHostname;
            DestHostPort = destHostPort;

            ClientTcpClient = client;
            ClientTcpClient.NoDelay = true;
            ClientTcpClient.Client.NoDelay = true;

            ServerTcpClient = server;
            ServerTcpClient.NoDelay = true;
            ServerTcpClient.Client.NoDelay = true;
            
            ClientStream = client.GetStream();
            ServerStream = server.GetStream();
             
            Task.Run(() => ClientReaderAsync());
            Task.Run(() => ServerReaderAsync());

            _Active = true;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Human-readable string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            return TimestampUtc.ToString("MM/dd/yyyy HH:mm:ss") + " " + Source() + " to " + Destination();
        }

        /// <summary>
        /// Returns the source IP and port.
        /// </summary>
        /// <returns>String.</returns>
        public string Source()
        {
            return SourceIp + ":" + SourcePort;
        }

        /// <summary>
        /// Returns the destination IP and port along wit destination hostname and port.
        /// </summary>
        /// <returns>String.</returns>
        public string Destination()
        {
            return DestIp + ":" + DestPort + " [" + DestHostname + ":" + DestHostPort + "]";
        }

        /// <summary>
        /// Determines whether or not the tunnel is active.
        /// </summary>
        /// <returns>True if both connections are active.</returns>
        public bool IsActive()
        {
            bool clientActive = false;
            bool serverActive = false;
            bool clientSocketActive = false;
            bool serverSocketActive = false;

            if (ClientTcpClient != null)
            {
                clientActive = ClientTcpClient.Connected;

                if (ClientTcpClient.Client != null)
                {
                    TcpState clientState = GetTcpRemoteState(ClientTcpClient); 

                    if (clientState == TcpState.Established
                        || clientState == TcpState.Listen
                        || clientState == TcpState.SynReceived
                        || clientState == TcpState.SynSent
                        || clientState == TcpState.TimeWait)
                    {
                        clientSocketActive = true;
                    }
                }
            }

            if (ServerTcpClient != null)
            {
                serverActive = ServerTcpClient.Connected;

                if (ServerTcpClient.Client != null)
                {
                    // see https://github.com/jchristn/PuppyProxy/compare/master...waldekmastykarz:PuppyProxy:master

                    /*
                    TcpState serverState = GetTcpRemoteState(ServerTcpClient);

                    if (serverState == TcpState.Established
                        || serverState == TcpState.Listen
                        || serverState == TcpState.SynReceived
                        || serverState == TcpState.SynSent
                        || serverState == TcpState.TimeWait)
                    {
                        serverSocketActive = true;
                    }
                    */

                    serverSocketActive = true;
                }
            }

            // Console.WriteLine(" " + Active + " " + clientActive + " " + clientSocketActive + " " + serverActive + " " + serverSocketActive);
            _Active = _Active && clientActive && clientSocketActive && serverActive && serverSocketActive;
            return _Active;
        }

        /// <summary>
        /// Returns the metadata of the tunnel.
        /// </summary>
        /// <returns>Tunnel object without TCP instances or streams.</returns>
        public Tunnel Metadata()
        {
            Tunnel ret = new Tunnel();
            ret.DestHostname = DestHostname;
            ret.DestHostPort = DestHostPort;
            ret.DestIp = DestIp;
            ret.DestPort = DestPort;
            ret.SourceIp = SourceIp;
            ret.SourcePort = SourcePort;
            ret.TimestampUtc = TimestampUtc;
            return ret;
        }

        /// <summary>
        /// Tear down the tunnel object and resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Private-Methods

        protected virtual void Dispose(bool disposing)
        {
            if (ClientStream != null)
            {
                ClientStream.Close();
                ClientStream.Dispose();
            }

            if (ServerStream != null)
            {
                ServerStream.Close();
                ServerStream.Dispose();
            }

            if (ClientTcpClient != null)
            {
                ClientTcpClient.Dispose();
            }

            if (ServerTcpClient != null)
            {
                ServerTcpClient.Dispose();
            }
        }

        private bool StreamReadSync(TcpClient client, out byte[] data)
        {
            data = null;

            try
            { 
                Stream stream = client.GetStream();
                 
                int read = 0; 
                long bufferSize = 65536;
                byte[] buffer = new byte[bufferSize];

                read = stream.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    if (read == bufferSize)
                    {
                        data = buffer;
                        return true;
                    }
                    else
                    {
                        data = new byte[read];
                        Buffer.BlockCopy(buffer, 0, data, 0, read);
                        return true;
                    }
                }
                else
                {
                    data = null;
                    return true;
                }
            }
            catch (InvalidOperationException)
            { 
                _Active = false;
                return false;
            }
            catch (IOException)
            { 
                _Active = false;
                return false;
            }
            catch (Exception e)
            {
                _Logging.Exception("Tunnel", "StreamReadSync", e);
                _Active = false;
                return false;
            }
            finally
            {
            }
        }

        private async Task<byte[]> StreamReadAsync(TcpClient client)
        {
            try
            { 
                Stream stream = client.GetStream();
                byte[] buffer = new byte[65536];

                using (MemoryStream memStream = new MemoryStream())
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        if (read == buffer.Length)
                        {
                            return buffer;
                        }
                        else
                        {
                            byte[] data = new byte[read];
                            Buffer.BlockCopy(buffer, 0, data, 0, read);
                            return data;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                _Active = false;
                return null;
            }
            catch (IOException)
            {
                _Active = false;
                return null;
            }
            catch (Exception e)
            {
                _Logging.Exception("Tunnel", "StreamReadAsync", e);
                _Active = false;
                return null;
            }
            finally
            {
            }
        }

        private TcpState GetTcpLocalState(TcpClient tcpClient)
        {
            var state = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .FirstOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return state != null ? state.State : TcpState.Unknown;
        }

        private TcpState GetTcpRemoteState(TcpClient tcpClient)
        {
            var state = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .FirstOrDefault(x => x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint));
            return state != null ? state.State : TcpState.Unknown;
        }

        private void ClientReaderSync()
        {
            try
            {
                // Logging.Log(LoggingModule.Severity.Debug, "ClientReaderSync started for " + Source() + " to " + Destination());

                byte[] data = null;
                while (true)
                {
                    if (StreamReadSync(ClientTcpClient, out data))
                    {
                        if (data != null && data.Length > 0)
                        {
                            // Logging.Log(LoggingModule.Severity.Debug, "ClientReaderSync " + Source() + " to " + Destination() + " read " + data.Length + " bytes");
                            ServerTcpClient.Client.Send(data);
                            data = null;
                        }
                        else
                        {
                            // Logging.Log(LoggingModule.Severity.Debug, "ClientReaderSync no data returned");
                        }
                    }
                    else
                    { 
                        _Active = false;
                        return;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                _Active = false;
            }
            catch (SocketException)
            {
                _Active = false;
            }
            catch (Exception e)
            {
                _Logging.Exception("Tunnel", "ClientReaderSync", e);
                _Active = false;
            }
        }

        private void ServerReaderSync()
        {
            try
            {
                // Logging.Log(LoggingModule.Severity.Debug, "ServerReaderSync started for " + Source() + " to " + Destination());

                byte[] data = null;
                while (true)
                {
                    if (StreamReadSync(ServerTcpClient, out data))
                    {
                        if (data != null && data.Length > 0)
                        {
                            // Logging.Log(LoggingModule.Severity.Debug, "ServerReaderSync " + Destination() + " to " + Source() + " read " + data.Length + " bytes");
                            ClientTcpClient.Client.Send(data);
                            data = null;
                        }
                        else
                        {
                            // Logging.Log(LoggingModule.Severity.Debug, "ServerReaderSync no data returned");
                        }
                    }
                    else
                    { 
                        _Active = false;
                        return;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                _Active = false;
            }
            catch (SocketException)
            {
                _Active = false;
            }
            catch (Exception e)
            {
                _Logging.Exception("Tunnel", "ServerReaderSync", e);
                _Active = false;
            }
        }

        private async void ClientReaderAsync()
        {
            try
            {
                // Logging.Log(LoggingModule.Severity.Debug, "ClientReaderAsync started for " + Source() + " to " + Destination());

                byte[] data = null;
                while (true)
                {
                    data = await StreamReadAsync(ClientTcpClient);
                    if (data != null && data.Length > 0)
                    {
                        // Logging.Log(LoggingModule.Severity.Debug, "ClientReaderAsync " + Source() + " to " + Destination() + " read " + data.Length + " bytes");
                        ServerTcpClient.Client.Send(data);
                        data = null;
                    }

                    if (!_Active) break;
                }
            }
            catch (ObjectDisposedException)
            {
                _Active = false;
            }
            catch (SocketException)
            {
                _Active = false;
            }
            catch (Exception e)
            {
                _Logging.Exception("Tunnel", "ClientReaderAsync", e);
                _Active = false;
            }
        }

        private async void ServerReaderAsync()
        {
            try
            {
                // Logging.Log(LoggingModule.Severity.Debug, "ServerReaderAsync started for " + Source() + " to " + Destination());

                byte[] data = null;
                while (true)
                {
                    data = await StreamReadAsync(ServerTcpClient);
                    if (data != null && data.Length > 0)
                    {
                        // Logging.Log(LoggingModule.Severity.Debug, "ServerReaderAsync " + Destination() + " to " + Source() + " read " + data.Length + " bytes");
                        ClientTcpClient.Client.Send(data);
                        data = null;
                    }

                    if (!_Active) break;
                }
            }
            catch (ObjectDisposedException)
            {
                _Active = false;
            }
            catch (SocketException)
            {
                _Active = false;
            }
            catch (Exception e)
            {
                _Logging.Exception("Tunnel", "ServerReaderAsync", e);
                _Active = false;
            }
        }

        private void ClientSend(byte[] data)
        {
            if (data == null || data.Length < 1) return;
            ClientTcpClient.Client.Send(data);
        }

        private void ServerSend(byte[] data)
        {
            if (data == null || data.Length < 1) return;
            ServerTcpClient.Client.Send(data);
        }

        #endregion
    }
}
