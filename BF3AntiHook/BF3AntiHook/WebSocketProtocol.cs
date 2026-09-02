using System;
using Newtonsoft.Json;

namespace BF3AntiHook.BF3AntiHook
{
    public sealed class WebSocketEnvelope
    {
        public string Type { get; set; }
        public string RequestId { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string SessionId { get; set; }
        public object Payload { get; set; }

        public static WebSocketEnvelope Create(string type, object payload, string requestId = null, string sessionId = null)
        {
            return new WebSocketEnvelope
            {
                Type = type,
                RequestId = requestId ?? Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                SessionId = sessionId,
                Payload = payload
            };
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public static WebSocketEnvelope Parse(string json)
        {
            if (String.IsNullOrWhiteSpace(json) || json.Length > 1024 * 1024)
                throw new ArgumentException("Mensaje vacío o demasiado grande.", "json");

            var result = JsonConvert.DeserializeObject<WebSocketEnvelope>(json);
            if (result == null || String.IsNullOrWhiteSpace(result.Type) || result.Type.Length > 80)
                throw new FormatException("Envelope WebSocket inválido.");
            return result;
        }
    }

    public static class WebSocketMessageTypes
    {
        public const string AuthLogin = "auth.login";
        public const string AuthOk = "auth.ok";
        public const string AuthError = "auth.error";
        public const string ServersSubscribe = "servers.subscribe";
        public const string ServersUpdated = "servers.updated";
        public const string AdminUsersList = "admin.users.list";
        public const string AdminUserAction = "admin.user.action";
        public const string AdminEvent = "admin.event";
        public const string SpeechNotification = "speech.notification";
        public const string Ping = "ping";
        public const string Pong = "pong";
    }

    public sealed class AuthLoginPayload
    {
        public string User { get; set; }
        public string Password { get; set; }
        public string ClientVersion { get; set; }
        public string Hwid { get; set; }
    }

    public sealed class ServersUpdatedPayload
    {
        public string GameId { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public System.Collections.Generic.List<Servers> Servers { get; set; }
    }
}
