﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using URU.Client.Data;

namespace URU.Services
{
    public class SpotifyService
    {
        public Client.Client Client { get; set; }

        public string? AccessToken { get; set; }

        public DateTimeOffset TokenExpirationTime { get; set; }

        public SpotifyService()
        {
            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            var httpClient = new HttpClient(httpClientHandler, false)
            {
                Timeout = TimeSpan.FromSeconds(60),
            };

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.ConnectionClose = false;
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");

            Client = new Client.Client(httpClient);
        }

        public bool HeaderHasToken()
        {
            if (string.IsNullOrWhiteSpace(AccessToken) || TokenExpirationTime <= DateTimeOffset.UtcNow)
                return false;

            Client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpRequestHeader.Authorization.ToString(), "Bearer " + AccessToken);
            return true;
        }

        public async Task SetAuthorizationHeader()
        {
            if (HeaderHasToken())
                return;

            var request = AccessTokenRequestMessage();

            var response = await Client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException();

            var result = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(result)!;
            AccessToken = jsonResponse["access_token"]!.ToString();
            string expiresIn = jsonResponse["expires_in"]!.ToString();
            TokenExpirationTime = DateTimeOffset.Now.AddSeconds(double.Parse(expiresIn) - 25);

            Client.HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {AccessToken}");
        }

        private HttpRequestMessage AccessTokenRequestMessage()
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<Startup>()
                .Build();

            string clientId = configuration["SPOTIFY_CLIENT_ID"]!;
            string clientSecret = configuration["SPOTIFY_CLIENT_SECRET"]!;

            var clientConfig = new ClientConfiguration(clientId, clientSecret);

            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientConfig.ClientId}:{clientConfig.ClientSecret}"));
            Client.HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Basic {base64Credentials}");

            return new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://accounts.spotify.com/api/token"),
                Content = new StringContent(
                    "grant_type=client_credentials",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"
                )
            };
        }
    }
}