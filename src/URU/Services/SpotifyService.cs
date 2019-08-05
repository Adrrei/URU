using Microsoft.Extensions.Configuration;
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
        private static string AccessToken;
        private static DateTimeOffset TokenExpires;

        public SpotifyService()
        {
            CreateHttpClient();
        }

        private void CreateHttpClient()
        {
            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            var httpClient = new HttpClient(httpClientHandler, false)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");
            httpClient.DefaultRequestHeaders.ConnectionClose = false;
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Client = new Client.Client(httpClient);
        }

        public async Task SetAuthorizationHeader()
        {
            if (!string.IsNullOrWhiteSpace(AccessToken) || DateTimeOffset.Now.CompareTo(TokenExpires) < 0)
            {
                Client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpRequestHeader.Authorization.ToString(), "Bearer " + AccessToken);
                return;
            }

            var request = AccessTokenRequestMessage();

            try
            {
                var response = await Client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException();

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(result);
                AccessToken = jsonResponse["access_token"].ToString();
                string expiresIn = jsonResponse["expires_in"].ToString();
                TokenExpires = DateTimeOffset.Now.AddSeconds(double.Parse(expiresIn) - 25);

                Client.HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {AccessToken}");
            }
            catch
            {
                throw;
            }
        }

        private HttpRequestMessage AccessTokenRequestMessage()
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .Build();

            var clientConfig = new ClientConfiguration()
            {
                ClientId = configuration["spotify_clientId"],
                ClientSecret = configuration["spotify_clientSecret"],
                Domain = configuration["spotify_domain"]
            };

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