using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace URU.Models
{
    public class Spotify
    {
        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;
        private readonly IConfiguration _configuration;
        private static string accessToken;
        private static DateTimeOffset tokenExpiryDate;

        public Spotify(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private void CreateHttpClient()
        {
            _httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            _httpClient = new HttpClient(_httpClientHandler, false)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            var sectionSpotify = _configuration.GetSection("Spotify");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");
            _httpClient.DefaultRequestHeaders.ConnectionClose = false;
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            ServicePointManager.FindServicePoint(new Uri(sectionSpotify["TokenUri"])).ConnectionLeaseTimeout = 60 * 1000;
            ServicePointManager.FindServicePoint(new Uri(sectionSpotify["Endpoint"])).ConnectionLeaseTimeout = 60 * 1000;
        }

        private void EnsureHttpClientCreated()
        {
            if (_httpClient == null)
            {
                CreateHttpClient();
            }
        }

        public async Task VerifyAndIssueAccessToken()
        {
            if (string.IsNullOrEmpty(accessToken) || DateTimeOffset.Now.CompareTo(tokenExpiryDate) > 0)
            {
                await GetAccessToken();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpRequestHeader.Authorization.ToString(), "Bearer " + accessToken);
        }

        public async Task GetAccessToken()
        {
            var sectionSpotify = _configuration.GetSection("Spotify");
            var clientId = sectionSpotify["ClientId"];
            var clientSecret = sectionSpotify["ClientSecret"];
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            try
            {
                EnsureHttpClientCreated();
                _httpClient.DefaultRequestHeaders.Add(HttpRequestHeader.Authorization.ToString(), "Basic " + base64Credentials);

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

                using (var response = await _httpClient.PostAsync(httpRequestMessage.RequestUri, httpRequestMessage.Content))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(result);
                        accessToken = jsonResponse["access_token"].ToString();
                        string expiresIn = jsonResponse["expires_in"].ToString();
                        tokenExpiryDate = DateTimeOffset.Now.AddSeconds(double.Parse(expiresIn) - 100);
                    }
                }
            }
            catch (WebException)
            {
                throw;
            }
            catch (Exception)
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

        public string GetEndpoint(User user, Method method, (string query, string value)[] parameters = null)
        {
            var spotifyUrl = new StringBuilder(_configuration.GetSection("Spotify")["Endpoint"]);
            switch (method)
            {
                case Method.GetPlaylist:
                    spotifyUrl.Append($"users/{user.UserId}/playlists/{user.PlaylistId}");
                    break;

                case Method.GetPlaylists:
                    spotifyUrl.Append($"users/{user.UserId}/playlists");
                    break;

                case Method.GetPlaylistTracks:
                    spotifyUrl.Append($"playlists/{user.PlaylistId}/tracks");
                    break;

                default:
                    return "";
            }

            if (parameters != null && parameters.Length > 0)
            {
                var query = new StringBuilder();
                foreach (var parameter in parameters)
                {
                    query.Append("&" + parameter.query + "=" + parameter.value);
                }

                spotifyUrl.Append("?" + query);
            }

            return spotifyUrl.ToString();
        }

        public async Task<T> GetSpotify<T>(string spotifyUrl)
        {
            EnsureHttpClientCreated();
            await VerifyAndIssueAccessToken();

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(spotifyUrl)
            };

            try
            {
                using (HttpResponseMessage response = await _httpClient.GetAsync(httpRequestMessage.RequestUri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        T jsonResponse = JsonConvert.DeserializeObject<T>(result);
                        return jsonResponse;
                    }
                }
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception)
            {
                throw;
            }

            return default;
        }

        public async Task<long> GetSpotifyPlaytime<T>(User user, long numberOfSongsInPlaylist)
        {
            if (user == null)
                return default;

            IList<HttpRequestMessage> urls = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(duration_ms))")
                };

                string spotifyUrl = GetEndpoint(user, Method.GetPlaylistTracks, parameters);
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(spotifyUrl)
                };

                urls.Add(httpRequestMessage);
                user.Offset += 100;
            }

            EnsureHttpClientCreated();
            await VerifyAndIssueAccessToken();

            long milliseconds = 0;

            try
            {
                var requests = urls.Select(url => _httpClient.GetAsync(url.RequestUri));
                var responses = requests.Select(task => task.Result);

                foreach (var response in responses)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(result);

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
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<dynamic> GetIdDurationArtists<T>(User user, long numberOfSongsInPlaylist)
        {
            if (user == null)
                return default;

            IList<HttpRequestMessage> urls = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(id, duration_ms, artists(name)))")
                };

                string spotifyUrl = GetEndpoint(user, Method.GetPlaylistTracks, parameters);
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(spotifyUrl)
                };

                urls.Add(httpRequestMessage);
                user.Offset += 100;
            }

            EnsureHttpClientCreated();
            await VerifyAndIssueAccessToken();

            var artistsCount = new Dictionary<string, int>();
            long milliseconds = 0;
            string latestAddition = "";

            try
            {
                var requests = urls.Select(url => _httpClient.GetAsync(url.RequestUri));
                var responses = requests.Select(task => task.Result);

                foreach (var response in responses)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    foreach (var item in playlist.Items)
                    {
                        milliseconds += item.Track.DurationMs;
                        foreach (var artist in item.Track.Artists)
                        {
                            if (artistsCount.TryGetValue(artist.Name, out int val))
                            {
                                artistsCount[artist.Name] = val + 1;
                            }
                            else
                            {
                                artistsCount.Add(artist.Name, 1);
                            }
                        }
                        latestAddition = item.Track.Id;
                    }
                }

                int hours = (int)TimeSpan.FromMilliseconds(milliseconds).TotalHours;

                var data = new
                {
                    Artists = artistsCount.OrderByDescending(a => a.Value).Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Time = hours,
                    Latest = latestAddition
                };

                return data;
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetTopArtists<T>(User user, long numberOfSongsInPlaylist, int numArtists)
        {
            if (user == null)
                return default;

            IList<HttpRequestMessage> urls = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(artists(name)))")
                };

                string spotifyUrl = GetEndpoint(user, Method.GetPlaylistTracks, parameters);
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(spotifyUrl)
                };

                urls.Add(httpRequestMessage);
                user.Offset += 100;
            }

            EnsureHttpClientCreated();
            await VerifyAndIssueAccessToken();

            var artistsCount = new Dictionary<string, int>();

            try
            {
                var requests = urls.Select(url => _httpClient.GetAsync(url.RequestUri));
                var responses = requests.Select(task => task.Result);

                foreach (var response in responses)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    foreach (var item in playlist.Items)
                    {
                        foreach (var artist in item.Track.Artists)
                        {
                            if (artistsCount.TryGetValue(artist.Name, out int val))
                            {
                                artistsCount[artist.Name] = val + 1;
                            }
                            else
                            {
                                artistsCount.Add(artist.Name, 1);
                            }
                        }
                    }
                }

                return artistsCount.OrderByDescending(a => a.Value).Take(numArtists).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch (WebException)
            {
                return default;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}