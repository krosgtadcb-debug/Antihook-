using Newtonsoft.Json;
using System;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BF3AntiHook.BF3AntiHook
{
    /// <summary>
    /// Servidor WebSocket v1. Se mantiene separado del listener TCP legado para facilitar
    /// la migración gradual y permitir rollback durante las pruebas de compatibilidad.
    /// </summary>
    public sealed class WebSocketAntiHookServer : IDisposable
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly MysqlConnector database;
        private readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();
        private CancellationTokenSource cancellation;
        private Task acceptLoop;
        private readonly int port;

        public event Action<string> Info;
        public event Action<User, bool> PlayerConnected;

        public WebSocketAntiHookServer(int port, string bdusername, string bd, string bdip, int bdport, string bdpassword)
        {
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException("port");
            this.port = port;
            database = new MysqlConnector(bdip, bdusername, bdpassword, bdport, bd);
        }

        public void Start()
        {
            if (acceptLoop != null) throw new InvalidOperationException("El servidor ya está iniciado.");
            cancellation = new CancellationTokenSource();
            listener.Prefixes.Add(String.Format("http://+:{0}/ws/", port));
            listener.Start();
            acceptLoop = Task.Run(() => AcceptLoopAsync(cancellation.Token));
            _ = Task.Run(() => BroadcastServersAsync(cancellation.Token), cancellation.Token);
            WriteInfo("WebSocket escuchando en /ws/:" + port);
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try { context = await listener.GetContextAsync().ConfigureAwait(false); }
                catch (Exception) { if (!cancellationToken.IsCancellationRequested) WriteInfo("Error aceptando conexión WebSocket."); break; }

                if (!context.Request.IsWebSocketRequest || context.Request.Url.AbsolutePath != "/ws/")
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }
                _ = Task.Run(() => HandleClientAsync(context, cancellationToken), cancellationToken);
            }
        }

        private async Task HandleClientAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            WebSocket socket = null;
            Session session = null;
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                socket = webSocketContext.WebSocket;
                var buffer = new byte[64 * 1024];
                while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var json = await ReceiveTextAsync(socket, buffer, cancellationToken).ConfigureAwait(false);
                    if (json == null) break;
                    WebSocketEnvelope envelope;
                    try { envelope = WebSocketEnvelope.Parse(json); }
                    catch (Exception ex) { await SendAsync(socket, WebSocketEnvelope.Create(WebSocketMessageTypes.AuthError, new { Message = "Mensaje inválido" }), cancellationToken).ConfigureAwait(false); WriteInfo("Mensaje rechazado: " + ex.Message); continue; }

                    if (envelope.Type == WebSocketMessageTypes.AuthLogin)
                    {
                        session = await AuthenticateAsync(socket, envelope, context.Request.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
                        if (session == null) break;
                        session.Socket = socket;
                        sessions[session.Id] = session;
                        PlayerConnected?.Invoke(session.User, true);
                        await SendServerSnapshotAsync(session, cancellationToken).ConfigureAwait(false);
                    }
                    else if (session == null)
                    {
                        await SendAsync(socket, WebSocketEnvelope.Create(WebSocketMessageTypes.AuthError, new { Message = "Autenticación requerida" }), cancellationToken).ConfigureAwait(false);
                    }
                    else if (envelope.Type == WebSocketMessageTypes.ServersSubscribe)
                    {
                        session.SubscribedToServers = true;
                        await SendServerSnapshotAsync(session, cancellationToken).ConfigureAwait(false);
                    }
                    else if (envelope.Type == WebSocketMessageTypes.Ping)
                    {
                        await SendAsync(socket, WebSocketEnvelope.Create(WebSocketMessageTypes.Pong, null, sessionId: session.Id), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) { WriteInfo("WebSocket desconectado: " + ex.Message); }
            finally
            {
                if (session != null)
                {
                    sessions.TryRemove(session.Id, out _);
                    PlayerConnected?.Invoke(session.User, false);
                }
                if (socket != null) socket.Dispose();
            }
        }

        private async Task<Session> AuthenticateAsync(WebSocket socket, WebSocketEnvelope envelope, IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            var payload = JsonConvert.DeserializeObject<AuthLoginPayload>(JsonConvert.SerializeObject(envelope.Payload));
            if (payload == null || String.IsNullOrWhiteSpace(payload.User) || String.IsNullOrWhiteSpace(payload.Password))
            {
                await SendAsync(socket, WebSocketEnvelope.Create(WebSocketMessageTypes.AuthError, new { Message = "Credenciales inválidas" }), cancellationToken).ConfigureAwait(false);
                return null;
            }

            User matched = null;
            foreach (var user in database.GetUsers())
            {
                if (String.Equals(user.Username, payload.User, StringComparison.Ordinal) &&
                    String.Equals(user.Password == null ? "" : user.Password.Trim(), PasswordHasher.HashPassword(payload.Password).Trim(), StringComparison.OrdinalIgnoreCase))
                { matched = user; break; }
            }
            if (matched == null)
            {
                await SendAsync(socket, WebSocketEnvelope.Create(WebSocketMessageTypes.AuthError, new { Message = "Credenciales inválidas" }), cancellationToken).ConfigureAwait(false);
                return null;
            }

            var session = new Session(Guid.NewGuid().ToString("N"), matched, endpoint == null ? "" : endpoint.Address.ToString());
            await SendAsync(socket, WebSocketEnvelope.Create(WebSocketMessageTypes.AuthOk, new { SessionId = session.Id, Token = Guid.NewGuid().ToString("N") }, sessionId: session.Id), cancellationToken).ConfigureAwait(false);
            return session;
        }

        private async Task SendServerSnapshotAsync(Session session, CancellationToken cancellationToken)
        {
            var payload = new ServersUpdatedPayload
            {
                GameId = "bf3",
                GeneratedAtUtc = DateTime.UtcNow,
                Servers = database.GetServers()
            };
            await SendAsync(session.Socket, WebSocketEnvelope.Create(WebSocketMessageTypes.ServersUpdated, payload, sessionId: session.Id), cancellationToken).ConfigureAwait(false);
        }

        public async Task BroadcastServersAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var entry in sessions)
                    if (entry.Value.SubscribedToServers && entry.Value.Socket.State == WebSocketState.Open)
                        await SendServerSnapshotAsync(entry.Value, cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task SendAsync(WebSocket socket, WebSocketEnvelope envelope, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(envelope.ToJson());
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> ReceiveTextAsync(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (builder.Length > 1024 * 1024) throw new InvalidOperationException("Mensaje demasiado grande");
            } while (!result.EndOfMessage);
            return builder.ToString();
        }

        private void WriteInfo(string message) { Info?.Invoke(message); }

        public void Dispose()
        {
            if (cancellation != null) cancellation.Cancel();
            if (listener.IsListening) listener.Stop();
            listener.Close();
        }

        private sealed class Session
        {
            public readonly string Id;
            public readonly User User;
            public readonly string Ip;
            public WebSocket Socket { get; set; }
            public bool SubscribedToServers { get; set; }
            public Session(string id, User user, string ip) { Id = id; User = user; Ip = ip; }
        }
    }
}
