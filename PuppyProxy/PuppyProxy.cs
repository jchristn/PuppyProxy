using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RestWrapper;
using SyslogLogging;
using WatsonWebserver;
using System.Text;

namespace PuppyProxy
{
    class MainClass
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static string _SettingsFile = null;
        private static Settings _Settings;
        private static LoggingModule _Logging;

        private static RequestManager _Requests;
        private static TunnelManager _Tunnels;
        private static Server _ApiServer;
        private static SecurityModule _SecurityModule;

        private static TcpListener _TcpListener;

        private static CancellationTokenSource _CancelTokenSource;
        private static CancellationToken _CancelToken;
        private static int _ActiveThreads = 0;

        private static readonly EventWaitHandle Terminator = new EventWaitHandle(false, EventResetMode.ManualReset, "UserIntervention");

        #endregion

        #region Main

        /// <summary>
        /// Entry point for PuppyProxy.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            #region Setup

            SetupConsole();
            LoadConfiguration(args);
            Welcome();

            #endregion

            #region Initialize-Globals

            _Logging = new LoggingModule(
                _Settings.Logging.SyslogServerIp,
                _Settings.Logging.SyslogServerPort,
                _Settings.Logging.ConsoleEnable,
                (LoggingModule.Severity)_Settings.Logging.MinimumLevel,
                false,
                true,
                true,
                false,
                true,
                false);

            _Requests = new RequestManager(_Logging);
            _Tunnels = new TunnelManager(_Logging);
            _SecurityModule = new SecurityModule(_Logging);

            _ApiServer = new Server(
                _Settings.Server.DnsHostname,
                _Settings.Server.Port,
                _Settings.Server.Ssl,
                ApiHandler,
                _Settings.Server.DebugServer);

            _CancelTokenSource = new CancellationTokenSource();
            _CancelToken = _CancelTokenSource.Token;

            Task.Run(() => AcceptConnections(), _CancelToken);

            #endregion

            #region Console

            if (_Settings.EnableConsole)
            {
                string userInput = "";
                bool runForever = true;

                while (runForever)
                {
                    userInput = Common.InputString("Command [? for help] >", null, false);
                    switch (userInput.ToLower().Trim())
                    {
                        case "?":
                            Menu();
                            break;

                        case "c":
                        case "cls":
                            Console.Clear();
                            break;

                        case "q":
                        case "quit":
                            Terminator.Set();
                            _CancelTokenSource.Cancel();
                            runForever = false;
                            break;

                        case "requests":
                            Dictionary<int, Request> requests = _Requests.Get();
                            if (requests != null && requests.Count > 0)
                            {
                                foreach (KeyValuePair<int, Request> curr in requests)
                                {
                                    Console.WriteLine(" ID " + curr.Key + ": " + curr.Value.ToString());
                                }
                            }
                            else
                            {
                                Console.WriteLine("None");
                            }
                            break;

                        case "tunnels":
                            Dictionary<int, Tunnel> tunnels = _Tunnels.GetMetadata();
                            if (tunnels != null && tunnels.Count > 0)
                            {
                                foreach (KeyValuePair<int, Tunnel> curr in tunnels)
                                {
                                    Console.WriteLine(" ID " + curr.Key + ": " + curr.Value.ToString());
                                }
                            }
                            else
                            {
                                Console.WriteLine("None");
                            }
                            break;
                    }
                }
            }

            #endregion

            Terminator.WaitOne();
        }

        #endregion

        #region Private-Setup-Methods

        private static void Welcome()
        {
            Console.WriteLine("---");
            Console.WriteLine("PuppyProxy starting on " + _Settings.Server.DnsHostname + ":" + _Settings.Server.Port);
            if (String.IsNullOrEmpty(_SettingsFile)) Console.WriteLine("Use --cfg=<filename> to load from a configuration file");
        }

        private static void LoadConfiguration(string[] args)
        {
            bool display = false;
            _SettingsFile = null;

            if (args != null && args.Length > 0)
            {
                foreach (string curr in args)
                {
                    if (curr.StartsWith("--cfg="))
                    {
                        _SettingsFile = curr.Substring(6);
                    }
                    else if (curr.Equals("--display-cfg"))
                    {
                        display = true;
                    }
                }
            }

            if (!String.IsNullOrEmpty(_SettingsFile))
            {
                _Settings = Settings.FromFile(_SettingsFile);
            }
            else
            {
                _Settings = Settings.Default();
            }

            if (display)
            {
                Console.WriteLine("--- Configuration ---");
                Console.WriteLine(WatsonCommon.SerializeJson(_Settings));
                Console.WriteLine("");
            }
        }

        #endregion

        #region Private-Console-Methods

        private static void SetupConsole()
        {
            if (Console.WindowWidth < 80) Console.WindowWidth = 80;
            return;
        }

        private static void Menu()
        {
            //                          1         2         3         4         5         6         7
            //                 1234567890123456789012345678901234567890123456789012345678901234567890123456789
            Console.WriteLine("--- Available Commands ---");
            Console.WriteLine(" ?           Help, this menu");
            Console.WriteLine(" c/cls       Clear the screen");
            Console.WriteLine(" q/quit      Exit PuppyProxy");
            Console.WriteLine(" requests    Show active proxied requests");
            Console.WriteLine(" tunnels     List current CONNECT tunnels");
            Console.WriteLine("");
        }

        #endregion

        #region Private-Request-Handler-Methods

        private static HttpResponse ApiHandler(HttpRequest req)
        {
            HttpResponse ret = new HttpResponse(req, false, 500, null, "application/json", "Internal server error", false);

            try
            {
                if (req == null) throw new ArgumentNullException(nameof(req));

                #region Enumerate

                _Logging.Log(LoggingModule.Severity.Debug, "ApiHandler " + req.SourceIp + ":" + req.SourcePort + " " + req.Method + " " + req.FullUrl);

                if (_Settings.Server.DebugRequests)
                {
                    _Logging.Log(LoggingModule.Severity.Debug, "Request received");
                    _Logging.Log(LoggingModule.Severity.Debug, req.ToString());
                }

                #endregion

                #region Unauthenticated-APIs

                switch (req.Method.ToString().ToLower().Trim())
                {
                    case "get":
                        if (WatsonCommon.UrlEqual(req.RawUrlWithoutQuery, "/favicon.ico", false))
                        {
                            ret = new HttpResponse(req, true, 200, null, null, null, true);
                            return ret;
                        }

                        if (WatsonCommon.UrlEqual(req.RawUrlWithoutQuery, "/_puppy/loopback", false))
                        {
                            ret = new HttpResponse(req, true, 200, null, "text/plain", "Hello from PuppyProxy!", true);
                            return ret;
                        }
                        break;

                    case "put":
                    case "post":
                    case "delete":
                    default:
                        break;
                }

                #endregion

                #region Authenticate

                #endregion

                #region Authenticated-APIs

                switch (req.Method.ToString().ToLower().Trim())
                {
                    case "get":
                    case "put":
                    case "post":
                    case "delete":
                    default:
                        break;
                }

                #endregion

                _Logging.Log(LoggingModule.Severity.Warn, "ApiHandler unknown method or endpoint: " + req.Method + " " + req.RawUrlWithoutQuery);
                ret = new HttpResponse(req, false, 400, null, "application/json", "Unknown method or endpoint", false);
                return ret;
            }
            catch (Exception e)
            {
                _Logging.LogException("PuppyProxy", "ApiHandler", e);
                return ret;
            }
            finally
            {
                #region Enumerate

                if (_Settings.Server.DebugResponses)
                {
                    _Logging.Log(LoggingModule.Severity.Debug, "Sending response");
                    _Logging.Log(LoggingModule.Severity.Debug, ret.ToString());
                }

                #endregion 
            }
        }

        private static void AcceptConnections()
        {
            try
            {
                if (String.IsNullOrEmpty(_Settings.Proxy.ListenerIpAddress))
                {
                    _TcpListener = new TcpListener(IPAddress.Any, _Settings.Proxy.ListenerPort);
                    Console.WriteLine("Starting TCP server on *:" + _Settings.Proxy.ListenerPort);
                }
                else
                {
                    _TcpListener = new TcpListener(IPAddress.Parse(_Settings.Proxy.ListenerIpAddress), _Settings.Proxy.ListenerPort);
                    Console.WriteLine("Starting TCP server on " + _Settings.Proxy.ListenerIpAddress + ":" + _Settings.Proxy.ListenerPort);
                }

                _TcpListener.Start();

                while (!_CancelToken.IsCancellationRequested)
                {
                    TcpClient client = _TcpListener.AcceptTcpClient();

                    Task.Run(() =>
                    {
                        string clientIp = "";
                        int clientPort = 0;
                        int connectionId = Thread.CurrentThread.ManagedThreadId;
                        _ActiveThreads++;

                        try
                        {
                            #region Check-if-Max-Exceeded

                            if (_ActiveThreads >= _Settings.Proxy.MaxThreads)
                            {
                                _Logging.Log(LoggingModule.Severity.Warn, "AcceptConnections current connection count " + _ActiveThreads + " exceeds configured max of " + _Settings.Proxy.MaxThreads + ", waiting for disconnect");
                                while (_ActiveThreads >= _Settings.Proxy.MaxThreads)
                                {
                                    Thread.Sleep(100);
                                }
                            }

                            #endregion

                            #region Enumerate

                            IPEndPoint clientIpEndpoint = client.Client.RemoteEndPoint as IPEndPoint;
                            IPEndPoint serverIpEndpoint = client.Client.LocalEndPoint as IPEndPoint;

                            string clientEndpoint = clientIpEndpoint.ToString();
                            string serverEndpoint = serverIpEndpoint.ToString();

                            clientIp = clientIpEndpoint.Address.ToString();
                            clientPort = clientIpEndpoint.Port;

                            string serverIp = serverIpEndpoint.Address.ToString();
                            int serverPort = serverIpEndpoint.Port;

                            #endregion

                            #region Build-HttpRequest

                            HttpRequest req = HttpRequest.FromTcpClient(client);
                            if (req == null)
                            {
                                _Logging.Log(LoggingModule.Severity.Warn, "AcceptConnections unable to build HTTP request from " + clientEndpoint);
                                _ActiveThreads--;
                                return;
                            }

                            req.SourceIp = clientIp;
                            req.SourcePort = clientPort;
                            req.DestIp = serverIp;
                            req.DestPort = serverPort;

                            _Logging.Log(LoggingModule.Severity.Debug, "AcceptConnections conn " + connectionId + " active " + _ActiveThreads + " " + clientEndpoint + " to " + serverEndpoint + " " + req.Method + " " + req.FullUrl);

                            #endregion

                            #region Security-Check

                            string denyReason = null;
                            bool isPermitted = _SecurityModule.IsPermitted(req, out denyReason);
                            if (!isPermitted)
                            {
                                _Logging.Log(LoggingModule.Severity.Info, "Request denied by security module " +
                                    req.SourceIp + ":" + req.SourcePort + " to " +
                                    req.FullUrl + 
                                    " [" + denyReason + "]"); 
                            }

                            #endregion

                            #region Process-Connection

                            if (isPermitted)
                            {
                                switch (req.Method.ToLower().Trim())
                                {
                                    case "connect":
                                        ConnectRequest(connectionId, client, req);
                                        break;

                                    default:
                                        byte[] respData = ProxyRequest(req);
                                        if (respData != null)
                                        {
                                            NetworkStream networkStream = client.GetStream();
                                            networkStream.Write(respData, 0, respData.Length);
                                            networkStream.Flush();
                                        }
                                        break;
                                }
                            }

                            #endregion

                            #region Close-Down

                            client.Close();
                            _ActiveThreads--;

                            #endregion
                        }
                        catch (IOException)
                        {

                        }
                        catch (Exception eInner)
                        {
                            _Logging.LogException("PuppyProxy", "AcceptConnections", eInner);
                        }
                    }, _CancelToken);
                }
            }
            catch (Exception eOuter)
            {
                _Logging.LogException("PuppyProxy", "AcceptConnections", eOuter);
            }
        }

        private static byte[] ProxyRequest(HttpRequest req)
        {
            Request curr = new Request(req);
            _Requests.Add(Thread.CurrentThread.ManagedThreadId, curr);

            try
            {
                RestResponse ret = RestRequest.SendRequestSafe(
                    req.FullUrl,
                    req.ContentType,
                    req.Method,
                    null, null, false,
                    _Settings.Proxy.AcceptInvalidCertificates,
                    req.Headers,
                    req.Data);

                if (ret == null)
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ProxyRequest no server response from " + req.Method + " " + req.FullUrl + " for " + req.SourceIp + ":" + req.SourcePort);
                    return null;
                }

                HttpResponse resp = new HttpResponse();
                resp.ProtocolVersion = req.ProtocolVersion;
                resp.SourceIp = req.SourceIp;
                resp.SourcePort = req.SourcePort;
                resp.DestIp = req.DestIp;
                resp.DestPort = req.DestPort;
                resp.Method = req.Method;
                resp.RawUrlWithoutQuery = req.RawUrlWithoutQuery;
                resp.StatusCode = ret.StatusCode;
                resp.StatusDescription = ret.StatusDescription;
                resp.Headers = ret.Headers;
                resp.ContentType = ret.ContentType;
                resp.ContentLength = ret.ContentLength;
                resp.Data = ret.Data;

                byte[] respData = resp.ToHttpBytes();
                return respData;
            }
            catch (Exception e)
            {
                _Logging.LogException("PuppyProxy", "ProxyRequest", e);
                return null;
            }
            finally
            {
                _Requests.Remove(Thread.CurrentThread.ManagedThreadId);
            }
        }

        private static void ConnectRequest(int connectionId, TcpClient client, HttpRequest req)
        {
            Tunnel currTunnel = null;
            TcpClient server = null;

            try
            {
                client.NoDelay = true;
                client.Client.NoDelay = true;

                server = new TcpClient();

                try
                {
                    server.Connect(req.DestHostname, req.DestHostPort);
                    // _Logging.Log(LoggingModule.Severity.Debug, "ConnectRequest established TCP connection to " + req.DestHostname + ":" + req.DestHostPort);
                }
                catch (Exception e)
                {
                    _Logging.Log(LoggingModule.Severity.Debug, "ConnectRequest connect failed to " + req.DestHostname + ":" + req.DestHostPort);
                    return;
                }

                server.NoDelay = true;
                server.Client.NoDelay = true;

                byte[] connectResponse = ConnectResponse();
                client.Client.Send(connectResponse);

                currTunnel = new Tunnel(
                    _Logging,
                    req.SourceIp,
                    req.SourcePort,
                    req.DestIp,
                    req.DestPort,
                    req.DestHostname,
                    req.DestHostPort,
                    client,
                    server);
                _Tunnels.Add(connectionId, currTunnel);

                while (currTunnel.IsActive())
                {
                    Task.Delay(100).Wait();
                }
            }
            catch (SocketException)
            {
                // do nothing
            }
            catch (Exception e)
            {
                _Logging.LogException("PuppyProxy", "ConnectRequest", e);
            }
            finally
            {
                if (currTunnel != null) _Logging.Log(LoggingModule.Severity.Debug, "ConnectRequest conn " + connectionId + " close " + currTunnel.Source() + " to " + currTunnel.Destination());
                _Tunnels.Remove(connectionId);

                if (client != null)
                {
                    client.Dispose();
                }

                if (server != null)
                {
                    server.Dispose();
                }
            }
        }

        private static byte[] ConnectResponse()
        {
            string resp = "HTTP/1.1 200 Connection Established\r\nConnection: close\r\n\r\n";
            return Encoding.UTF8.GetBytes(resp);
        }

        #endregion
    }
}
