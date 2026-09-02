using BF3AntiHook.BF3AntiHook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Antihookclient
{
    /// <summary>
    /// Transporte WebSocket v1. Mantiene una sesión persistente y recibe snapshots push.
    /// El servidor debe aplicar autenticación, autorización y validación de esquema.
    /// </summary>
    public sealed class WebSocketAntiHookClient : IDisposable
    {
        private readonly ClientWebSocket socket = new ClientWebSocket();
        private CancellationTokenSource cancellation;
        private Task receiveLoop;
        private string sessionId;
        private string token;

        public event Action<bool> ConnectionChanged;
        public event Action<List<Servers>> ServersUpdated;
        public event Action<string> Notification;

        public bool IsConnected { get { return socket.State == WebSocketState.Open; } }

        public async Task<bool> ConnectAndLoginAsync(string host, int port, string username, string password, string hwid, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(host) || port < 1 || port > 65535)
                throw new ArgumentException("Host o puerto inválido.");

            cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var uri = new Uri(String.Format("ws://{0}:{1}/ws", host, port));
            await socket.ConnectAsync(uri, cancellation.Token).ConfigureAwait(false);
            ConnectionChanged?.Invoke(true);

            receiveLoop = Task.Run(() => ReceiveLoopAsync(cancellation.Token), cancellation.Token);
            var login = WebSocketEnvelope.Create(WebSocketMessageTypes.AuthLogin, new AuthLoginPayload
            {
                User = username,
                Password = password,
                ClientVersion = "1.0.0",
                Hwid = hwid
            });
            await SendAsync(login, cancellation.Token).ConfigureAwait(false);
            return true;
        }

        public Task SubscribeBattlefield3Async(CancellationToken cancellationToken)
        {
            return SendAsync(WebSocketEnvelope.Create(WebSocketMessageTypes.ServersSubscribe, new { GameId = "bf3" }, sessionId: sessionId), cancellationToken);
        }

        public async Task SendAdminActionAsync(string action, string userId, string reason, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(action) || String.IsNullOrWhiteSpace(userId) || String.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Toda acción administrativa requiere acción, usuario y motivo.");

            await SendAsync(WebSocketEnvelope.Create(WebSocketMessageTypes.AdminUserAction, new
            {
                Action = action,
                UserId = userId,
                Reason = reason,
                Confirmed = true
            }, sessionId: sessionId), cancellationToken).ConfigureAwait(false);
        }

        private async Task SendAsync(WebSocketEnvelope message, CancellationToken cancellationToken)
        {
            if (!IsConnected)
                throw new InvalidOperationException("La sesión WebSocket no está conectada.");

            byte[] bytes = Encoding.UTF8.GetBytes(message.ToJson());
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[64 * 1024];
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    var builder = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                            return;
                        builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        if (builder.Length > 1024 * 1024)
                            throw new InvalidOperationException("Mensaje WebSocket demasiado grande.");
                    }
                    while (!result.EndOfMessage);

                    var envelope = WebSocketEnvelope.Parse(builder.ToString());
                    if (envelope.Type == WebSocketMessageTypes.AuthOk)
                    {
                        var payload = JsonConvert.DeserializeObject<AuthOkPayload>(JsonConvert.SerializeObject(envelope.Payload));
                        sessionId = payload.SessionId;
                        token = payload.Token;
                        await SubscribeBattlefield3Async(cancellationToken).ConfigureAwait(false);
                    }
                    else if (envelope.Type == WebSocketMessageTypes.ServersUpdated)
                    {
                        var payload = JsonConvert.DeserializeObject<ServersUpdatedPayload>(JsonConvert.SerializeObject(envelope.Payload));
                        ServersUpdated?.Invoke(payload.Servers ?? new List<Servers>());
                    }
                    else if (envelope.Type == WebSocketMessageTypes.SpeechNotification || envelope.Type == WebSocketMessageTypes.AdminEvent)
                    {
                        Notification?.Invoke(JsonConvert.SerializeObject(envelope.Payload));
                    }
                    else if (envelope.Type == WebSocketMessageTypes.Ping)
                    {
                        await SendAsync(WebSocketEnvelope.Create(WebSocketMessageTypes.Pong, null, sessionId: sessionId), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException ex) { Notification?.Invoke("WebSocket: " + ex.Message); }
            finally { ConnectionChanged?.Invoke(false); }
        }

        public async Task DisconnectAsync()
        {
            if (cancellation != null) cancellation.Cancel();
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre solicitado", CancellationToken.None).ConfigureAwait(false);
            ConnectionChanged?.Invoke(false);
        }

        public void Dispose()
        {
            if (cancellation != null) cancellation.Cancel();
            socket.Dispose();
            if (cancellation != null) cancellation.Dispose();
        }

        private sealed class AuthOkPayload
        {
            public string SessionId { get; set; }
            public string Token { get; set; }
        }
    }
}
