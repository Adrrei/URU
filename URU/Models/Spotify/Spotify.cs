using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace URU.Models
{
    public class Spotify
    {
        private readonly IConfiguration _configuration;
        private static string accessToken;
        private static DateTimeOffset tokenExpiryDate;

        public Spotify(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> VerifyAndIssueAccessToken()
        {
            if (string.IsNullOrEmpty(accessToken) || DateTimeOffset.Now.CompareTo(tokenExpiryDate) > 0)
            {
                await GetAccessToken();
            }

            return accessToken;
        }

        public async Task GetAccessToken()
        {

            var sectionSpotify = _configuration.GetSection("Spotify");
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(sectionSpotify["TokenUri"]),
                Content = new StringContent(
                    "grant_type=client_credentials",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"
                )
            };

            var clientId = sectionSpotify["ClientId"];
            var clientSecret = sectionSpotify["ClientSecret"];
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", clientId, clientSecret)));

            var httpClient = new HttpClient() {
                DefaultRequestHeaders =
                {
                    { "Authorization", "Basic " + base64Credentials },
                    { "Connection", "Keep-Alive" },
                    { "Accept", "application/json" }
                }
            };

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(httpRequestMessage.RequestUri, httpRequestMessage.Content);
                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResponse = await response.Content.ReadAsAsync<JObject>();
                    accessToken = (string)jsonResponse["access_token"];
                    string expiresIn = (string)jsonResponse["expires_in"];
                    tokenExpiryDate = DateTimeOffset.Now.AddSeconds(double.Parse(expiresIn) - 100);
                }
            }
            catch (WebException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<T> GetSpotify<T>(User user, Method method, string parameters = "")
        {
            if (user == null)
                return default;

            string spotifyUrl = "";
            switch (method)
            {
                case Method.GetPlaylist:
                    spotifyUrl += GetPlaylist(user, parameters);
                    break;

                case Method.GetPlaylists:
                    spotifyUrl += GetPlaylists(user, parameters);
                    break;

                case Method.GetPlaylistTracks:
                    spotifyUrl += GetPlaylistTracks(user, parameters);
                    break;

                default:
                    return default;
            }

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(spotifyUrl),
                Headers =
                {
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" }
                }
            };

            user.Token = await VerifyAndIssueAccessToken();
            var httpClient = new HttpClient()
            {
                DefaultRequestHeaders =
                {
                    { "Authorization", "Bearer " + user.Token },
                    { "Connection", "Keep-Alive" },
                    { "Accept", "application/json" }
                }
            };

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(httpRequestMessage.RequestUri);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsAsync<T>();
                }
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception ex)
            {
                throw;
            }

            return default;
        }

        public async Task<Playlist> GetPlaylists<Playlist>(User user)
        {
            if (user == null)
                return default;


            string spotifyUrl = GetPlaylistTracks(user, "?offset=" + user.Offset + "&limit=" + user.Limit);
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(spotifyUrl),
                Headers =
                {
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" }
                }
            };

            user.Token = await VerifyAndIssueAccessToken();
            var httpClient = new HttpClient()
            {
                DefaultRequestHeaders =
                {
                    { "Authorization", "Bearer " + user.Token },
                    { "Connection", "Keep-Alive" },
                    { "Accept", "application/json" }
                }
            };

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(httpRequestMessage.RequestUri);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsAsync<Playlist>();
                }
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception ex)
            {
                throw;
            }

            return default;
        }

        public async Task<long> GetSpotifyPlaytime<T>(User user, long numberOfSongs)
        {
            if (user == null)
                return default;
            
            IList<HttpRequestMessage> urls = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongs)
            {
                string spotifyUrl = GetPlaylistTracks(user, "?offset=" + user.Offset + "&fields=items(track(duration_ms))");
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(spotifyUrl),
                    Headers =
                    {
                        { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" }
                    }
                };

                urls.Add(httpRequestMessage);
                user.Offset += 100;
            }

            user.Token = await VerifyAndIssueAccessToken();
            var httpClient = new HttpClient()
            {
                DefaultRequestHeaders =
                {
                    { "Authorization", "Bearer " + user.Token },
                    { "Connection", "Keep-Alive" },
                    { "Accept", "application/json" }
                }
            };

            long milliseconds = 0;

            try
            {
                var requests = urls.Select(url => httpClient.GetAsync(url.RequestUri));
                var responses = requests.Select(task => task.Result);

                foreach (var response in responses)
                {
                    Playlist playlist = await response.Content.ReadAsAsync<Playlist>();

                    foreach (var track in playlist.Items)
                    {
                        milliseconds += track.Track.DurationMs;
                    }
                }

                return milliseconds;
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public enum Method
        {
            GetPlaylist,
            GetPlaylists,
            GetPlaylistTracks
        }

        public string GetPlaylist(User user, string parameters)
        {
            string endpoint = _configuration.GetSection("Spotify")["Endpoint"];
            string data = $"users/{user.UserId}/playlists/{user.PlaylistId}";
            return endpoint + data + parameters;
        }

        public string GetPlaylists(User user, string parameters)
        {
            string endpoint = _configuration.GetSection("Spotify")["Endpoint"];
            string data = $"users/{user.UserId}/playlists";
            return endpoint + data + parameters;
        }

        public string GetPlaylistTracks(User user, string parameters)
        {
            string endpoint = _configuration.GetSection("Spotify")["Endpoint"];
            string data = $"playlists/{user.PlaylistId}/tracks";
            return endpoint + data + parameters;
        }
    }
}