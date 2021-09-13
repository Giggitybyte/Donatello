using System;
using System.Net.Http;
using Donatello.Rest.Endpoints;

namespace Donatello.Rest
{
    /// <summary></summary>
    public class DiscordApi : IDisposable
    {
        private HttpClient _httpClient;

        /// <summary></summary>
        /// <param name="token"></param>
        /// <param name="isBearer">Whether the provided token is an OAuth2 bearer token.</param>
        public DiscordApi(string token, bool isBearer = false)
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            };

            _httpClient = new HttpClient(handler);

            _httpClient.BaseAddress = new Uri("https://discord.com/api/v9");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Donatello.Rest 0.0.1 - thegiggitybyte#8099");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"{(isBearer ? "Bearer" : "Bot")} {token}");

            this.Guild = new(_httpClient);
        }

        public GuildEndpoint Guild { get; init; }

        public void Dispose()
            => _httpClient.Dispose();
    }
}
