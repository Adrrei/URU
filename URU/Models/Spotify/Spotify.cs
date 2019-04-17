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

        public async Task<dynamic> GetIdDurationArtists<T>(User user, long numberOfSongsInPlaylist)
        {
            if (user == null)
                return default;

            IList<HttpRequestMessage> urls = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(id, duration_ms, artists(name, uri), album(id)))")
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

            var artistsCount = new Dictionary<string, (int, string)>();
            long milliseconds = 0;

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
                            if (artistsCount.TryGetValue(artist.Name, out (int Count, string Uri) val))
                            {
                                artistsCount[artist.Name] = (val.Count + 1, val.Uri);
                            }
                            else
                            {
                                artistsCount.Add(artist.Name, (1, artist.Uri));
                            }
                        }
                    }
                }

                int hours = (int)TimeSpan.FromMilliseconds(milliseconds).TotalHours;

                var data = new
                {
                    Artists = artistsCount.OrderByDescending(a => a.Value).Take(75).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Time = hours
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

        /// <summary>
        /// This method is ugly, but the only way to retrieve a track's release date is via its album,
        /// which has to be fetched via a completely different endpoint (I do cache the data at the client, though!).
        /// Thus, fetch all tracks, get each track's album ID, fetch all albums, get each album's release date.
        ///
        /// I could retrieve the album IDs by storing them in a Session variable while GetIdDurationArtists(..) is executing (since it's doing this method's first part anyway),
        /// and once it finishes execute the latter part of this method using said Session variable, but I fear it will be unreliable across various environments.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="numberOfSongsInPlaylist"></param>
        /// <returns></returns>
        public async Task<dynamic> GetTracksByYear(User user, long numberOfSongsInPlaylist)
        {
            IList<string> albumIds = new List<string>();

            if (user == null)
                return default;

            IList<HttpRequestMessage> tracksUrls = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(album(id)))")
                };

                string spotifyTracksUrl = GetEndpoint(user, Method.GetPlaylistTracks, parameters);
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(spotifyTracksUrl)
                };

                tracksUrls.Add(httpRequestMessage);
                user.Offset += 100;
            }

            EnsureHttpClientCreated();
            await VerifyAndIssueAccessToken();

            try
            {
                var requests = tracksUrls.Select(url => _httpClient.GetAsync(url.RequestUri));
                var responses = requests.Select(task => task.Result);

                foreach (var response in responses)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    foreach (var item in playlist.Items)
                    {
                        albumIds.Add(item.Track.Album.Id);
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

            IList<HttpRequestMessage> albumsUrls = new List<HttpRequestMessage>();

            string spotifyAlbumsUrl = _configuration.GetSection("Spotify")["Endpoint"] + "albums/?ids=";

            for (int i = 0; i < albumIds.Count; i += 20)
            {
                StringBuilder albumIdsConcat = new StringBuilder().AppendJoin(',', albumIds.Skip(i).Take(20));

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(spotifyAlbumsUrl + albumIdsConcat.ToString())
                };

                albumsUrls.Add(httpRequestMessage);
            }

            var songsByYear = new Dictionary<string, int>();

            try
            {
                var requests = albumsUrls.Select(url => _httpClient.GetAsync(url.RequestUri));
                var responses = requests.Select(task => task.Result);

                foreach (var response in responses)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Albums albums = JsonConvert.DeserializeObject<Albums>(result);

                    foreach (var album in albums.Items)
                    {
                        string year = album.ReleaseDate.Substring(0, 4);

                        if (songsByYear.TryGetValue(year, out int val))
                        {
                            songsByYear[year] = val + 1;
                        }
                        else
                        {
                            songsByYear.Add(year, 1);
                        }
                    }
                }

                var data = new
                {
                    TracksByYear = songsByYear.OrderByDescending(a => a.Key).ToArray(),
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
    }
}