using System;
using System.Net.Http;
using URU.Client.Resources;

namespace URU.Client
{
    public class Client
    {
        public HttpClient HttpClient;

        public SpotifyResource Spotify { get; set; }

        public Client(HttpClient httpClient)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Spotify = new SpotifyResource(httpClient);
        }
    }
}