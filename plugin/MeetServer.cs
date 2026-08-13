using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FuSan21.MacroDeck.GoogleMeet
{
    /// <summary>
    /// The loopback WebSocket server the browser extension connects to.
    ///
    /// The direction is deliberate and is what makes the whole design work without a
    /// native-messaging host: the extension is a plain content script that dials out to
    /// <c>ws://127.0.0.1:2394</c>, so there is no registry manifest, no helper executable,
    /// and no service worker to keep alive. All this plugin has to do is answer.
    ///
    /// The handshake is written by hand rather than handed to <see cref="HttpListener"/>,
    /// because http.sys reserves URL prefixes machine-wide: binding one needs either an
    /// elevated process or a <c>netsh http add urlacl</c> run once as administrator. A
    /// <see cref="TcpListener"/> on the loopback address needs neither, and the handshake
    /// itself is thirty lines — one header parse and one SHA-1.
    ///
    /// Several Meet tabs may be open at once and each runs its own copy of the content
    /// script, so this is a broadcast bus, not a single connection: commands go to every
    /// tab, and any tab may report state. In practice only one tab is ever in a call.
    /// </summary>
    internal class MeetServer
    {
        /// <summary>
        /// The GUID from RFC 6455 §1.3, concatenated with the client's key before hashing.
        /// It exists so that a cache or proxy replaying an ordinary HTTP response can never
        /// be mistaken for a valid handshake.
        /// </summary>
        private const string HandshakeGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        /// <summary>
        /// Origins allowed to open a socket. The browser sets this header itself and a page
        /// cannot forge it, so this is what stops any random website you visit from opening
        /// a socket to this port and hanging up your calls. Extension origins are accepted
        /// as well so the same server works if the script is ever moved into a background
        /// context, where the origin is the extension rather than the page.
        /// </summary>
        private static readonly string[] AllowedOriginPrefixes =
        {
            "https://meet.google.com",
            "chrome-extension://",
            "moz-extension://",
        };

        private const int MaxHandshakeBytes = 8 * 1024;
        private const int MaxMessageBytes = 256 * 1024;

        private readonly ConcurrentDictionary<Guid, Client> _clients = new ConcurrentDictionary<Guid, Client>();
        private readonly object _lifecycle = new object();

        private TcpListener _listener;
        private CancellationTokenSource _cancellation;

        /// <summary>Raised on a background thread for every JSON message a tab sends.</summary>
        public event EventHandler<JObject> MessageReceived;

        /// <summary>Raised whenever a tab connects or disconnects.</summary>
        public event EventHandler ClientsChanged;

        public bool IsRunning => _listener != null;

        public bool HasClients => !_clients.IsEmpty;

        public int ClientCount => _clients.Count;

        /// <summary>The last thing that went wrong, for the configuration dialog. Null when healthy.</summary>
        public string LastError { get; private set; }

        public void Start(int port)
        {
            lock (_lifecycle)
            {
                if (_listener != null)
                {
                    return;
                }

                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();

                    _listener = listener;
                    _cancellation = new CancellationTokenSource();
                    LastError = null;

                    var token = _cancellation.Token;
                    Task.Run(() => AcceptLoopAsync(listener, token));

                    PluginInstance.Logger.Info("Listening for the Google Meet extension on 127.0.0.1:{0}", port);
                }
                catch (SocketException ex)
                {
                    // Almost always "address already in use" — either a second Macro Deck,
                    // or ChrisRegado's Stream Deck plugin, which owns the same port.
                    _listener = null;
                    LastError = $"Could not listen on port {port}: {ex.Message}";
                    PluginInstance.Logger.Error("{0}", LastError);
                }
                catch (Exception ex)
                {
                    _listener = null;
                    LastError = ex.Message;
                    PluginInstance.Logger.Error("Failed to start the Google Meet server:\n{0}", ex);
                }
            }
        }

        public void Stop()
        {
            CancellationTokenSource cancellation;
            TcpListener listener;

            lock (_lifecycle)
            {
                cancellation = _cancellation;
                listener = _listener;
                _cancellation = null;
                _listener = null;
            }

            if (listener == null)
            {
                return;
            }

            try { cancellation?.Cancel(); } catch { }
            try { listener.Stop(); } catch { }

            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }
            _clients.Clear();

            try { cancellation?.Dispose(); } catch { }

            ClientsChanged?.Invoke(this, EventArgs.Empty);
            PluginInstance.Logger.Info("Stopped listening for the Google Meet extension");
        }

        /// <summary>
        /// Sends one message to every connected tab. Returns false when nobody is listening,
        /// which is how the actions tell the user their extension is not running.
        /// </summary>
        public bool Broadcast(object message)
        {
            if (_clients.IsEmpty)
            {
                return false;
            }

            var json = JsonConvert.SerializeObject(message);
            var payload = Encoding.UTF8.GetBytes(json);
            var sent = false;

            foreach (var client in _clients.Values)
            {
                if (client.TrySend(payload))
                {
                    sent = true;
                }
            }

            return sent;
        }

        #region Accepting connections

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient tcp;
                try
                {
                    tcp = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    // The listener was torn down under us.
                    return;
                }

                _ = Task.Run(() => HandleClientAsync(tcp, token), CancellationToken.None);
            }
        }

        private async Task HandleClientAsync(TcpClient tcp, CancellationToken token)
        {
            var id = Guid.NewGuid();
            Client client = null;

            try
            {
                tcp.NoDelay = true;
                var stream = tcp.GetStream();

                var headers = await ReadHandshakeAsync(stream, token).ConfigureAwait(false);
                if (headers == null || !IsWebSocketUpgrade(headers))
                {
                    await RejectAsync(stream, "400 Bad Request", token).ConfigureAwait(false);
                    return;
                }

                headers.TryGetValue("origin", out var origin);
                if (!IsOriginAllowed(origin))
                {
                    PluginInstance.Logger.Warning(
                        "Refused a WebSocket connection from origin '{0}'", origin ?? "(none)");
                    await RejectAsync(stream, "403 Forbidden", token).ConfigureAwait(false);
                    return;
                }

                var accept = ComputeAcceptKey(headers["sec-websocket-key"]);
                var response =
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Sec-WebSocket-Accept: " + accept + "\r\n\r\n";
                var responseBytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token).ConfigureAwait(false);

                var socket = WebSocket.CreateFromStream(
                    stream,
                    isServer: true,
                    subProtocol: null,
                    keepAliveInterval: TimeSpan.FromSeconds(30));

                client = new Client(tcp, socket);
                _clients[id] = client;
                ClientsChanged?.Invoke(this, EventArgs.Empty);
                PluginInstance.Logger.Info("A Google Meet tab connected ({0} now connected)", _clients.Count);

                await ReceiveLoopAsync(client, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                PluginInstance.Logger.Trace("Google Meet connection ended: {0}", ex.Message);
            }
            finally
            {
                if (_clients.TryRemove(id, out var removed))
                {
                    removed.Dispose();
                    ClientsChanged?.Invoke(this, EventArgs.Empty);
                    PluginInstance.Logger.Info("A Google Meet tab disconnected ({0} still connected)", _clients.Count);
                }
                else
                {
                    client?.Dispose();
                    try { tcp.Close(); } catch { }
                }
            }
        }

        private async Task ReceiveLoopAsync(Client client, CancellationToken token)
        {
            var buffer = new byte[8192];

            while (client.Socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await client.Socket
                        .ReceiveAsync(new ArraySegment<byte>(buffer), token)
                        .ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Answer the close frame instead of just dropping the socket.
                        // Without the reply the browser reports every disconnect as
                        // unclean and logs an error in the extension's console, which
                        // makes an ordinary tab close look like a fault.
                        try
                        {
                            await client.Socket
                                .CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                        catch { }

                        return;
                    }

                    if (message.Length + result.Count > MaxMessageBytes)
                    {
                        PluginInstance.Logger.Warning("Discarding an oversized message from a Meet tab");
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType != WebSocketMessageType.Text || message.Length == 0)
                {
                    continue;
                }

                var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                JObject parsed;
                try
                {
                    parsed = JObject.Parse(text);
                }
                catch (JsonException)
                {
                    PluginInstance.Logger.Warning("Ignoring malformed JSON from a Meet tab: {0}", text);
                    continue;
                }

                try
                {
                    MessageReceived?.Invoke(this, parsed);
                }
                catch (Exception ex)
                {
                    PluginInstance.Logger.Error("Failed to handle a message from Google Meet:\n{0}", ex);
                }
            }
        }

        #endregion

        #region Handshake

        /// <summary>
        /// Reads the request line and headers one byte at a time, stopping at the blank
        /// line. Byte-at-a-time is not the fast path, but it guarantees we never swallow
        /// the first WebSocket frame into a read buffer we then throw away.
        /// </summary>
        private static async Task<Dictionary<string, string>> ReadHandshakeAsync(Stream stream, CancellationToken token)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));

            var raw = new MemoryStream();
            var one = new byte[1];
            var matched = 0;
            var terminator = new byte[] { 13, 10, 13, 10 };

            while (matched < terminator.Length)
            {
                var read = await stream.ReadAsync(one, 0, 1, timeout.Token).ConfigureAwait(false);
                if (read == 0)
                {
                    return null;
                }

                raw.WriteByte(one[0]);
                matched = one[0] == terminator[matched] ? matched + 1 : (one[0] == 13 ? 1 : 0);

                if (raw.Length > MaxHandshakeBytes)
                {
                    return null;
                }
            }

            var text = Encoding.ASCII.GetString(raw.GetBuffer(), 0, (int)raw.Length);
            var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // lines[0] is the request line, which we do not care about beyond it being a
            // GET — the Upgrade headers are what actually authorise the switch.
            for (var i = 1; i < lines.Length; i++)
            {
                var colon = lines[i].IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
            }

            return headers;
        }

        private static bool IsWebSocketUpgrade(Dictionary<string, string> headers)
        {
            return headers.TryGetValue("upgrade", out var upgrade)
                && upgrade.IndexOf("websocket", StringComparison.OrdinalIgnoreCase) >= 0
                && headers.TryGetValue("sec-websocket-key", out var key)
                && !string.IsNullOrWhiteSpace(key);
        }

        private static bool IsOriginAllowed(string origin)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return false;
            }

            foreach (var prefix in AllowedOriginPrefixes)
            {
                if (origin.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ComputeAcceptKey(string clientKey)
        {
            var hash = SHA1.HashData(Encoding.ASCII.GetBytes(clientKey + HandshakeGuid));
            return Convert.ToBase64String(hash);
        }

        private static async Task RejectAsync(Stream stream, string status, CancellationToken token)
        {
            try
            {
                var bytes = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 " + status + "\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
                await stream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
            }
            catch { }
        }

        #endregion

        /// <summary>
        /// One connected tab. The semaphore serialises sends: broadcasting from several
        /// action buttons at once would otherwise interleave frames on the same socket,
        /// which <see cref="WebSocket"/> rejects outright.
        /// </summary>
        private class Client : IDisposable
        {
            private readonly TcpClient _tcp;
            private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

            public WebSocket Socket { get; }

            public Client(TcpClient tcp, WebSocket socket)
            {
                _tcp = tcp;
                Socket = socket;
            }

            public bool TrySend(byte[] payload)
            {
                if (Socket.State != WebSocketState.Open)
                {
                    return false;
                }

                if (!_sendLock.Wait(TimeSpan.FromSeconds(2)))
                {
                    PluginInstance.Logger.Warning("Timed out waiting to send to a Meet tab");
                    return false;
                }

                try
                {
                    Socket.SendAsync(
                        new ArraySegment<byte>(payload),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        CancellationToken.None).GetAwaiter().GetResult();
                    return true;
                }
                catch (Exception ex)
                {
                    PluginInstance.Logger.Trace("Could not send to a Meet tab: {0}", ex.Message);
                    return false;
                }
                finally
                {
                    try { _sendLock.Release(); } catch { }
                }
            }

            public void Dispose()
            {
                try { Socket.Abort(); } catch { }
                try { Socket.Dispose(); } catch { }
                try { _tcp.Close(); } catch { }
                try { _sendLock.Dispose(); } catch { }
            }
        }
    }
}
