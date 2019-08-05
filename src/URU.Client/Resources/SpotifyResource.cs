using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using URU.Models;

namespace URU.Client.Resources
{
    public class SpotifyResource
    {
        private static string Endpoint { get; } = "https://api.spotify.com/v1/";

        private readonly HttpClient Client;

        public SpotifyResource(HttpClient client)
        {
            Client = client;
        }

        private static HttpRequestMessage CreateRequest(string spotifyUrl)
        {
            return new HttpRequestMessage(HttpMethod.Get, spotifyUrl);
        }

        public async Task<T> GetObject<T>(string spotifyUrl)
        {
            var request = CreateRequest(spotifyUrl);
            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            try
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException();

                string result = await response.Content.ReadAsStringAsync();
                T jsonResponse = JsonConvert.DeserializeObject<T>(result);
                return jsonResponse;
            }
            catch
            {
                throw;
            }
        }

        public string ConstructEndpoint(User user, Method method, (string query, string value)[] parameters = null)
        {
            var spotifyUrl = new StringBuilder(Endpoint);
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

        public async Task<dynamic> GetDetailsArtists<T>(User user, long numberOfSongsInPlaylist)
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

                string spotifyUrl = ConstructEndpoint(user, Method.GetPlaylistTracks, parameters);
                var request = CreateRequest(spotifyUrl);

                urls.Add(request);
                user.Offset += 100;
            }

            var artistsCount = new Dictionary<string, (int, string)>();
            long milliseconds = 0;
            long songs = 0;

            try
            {
                foreach (var request in urls)
                {
                    var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    foreach (var item in playlist.Items)
                    {
                        songs++;
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
                    Artists = artistsCount.OrderByDescending(a => a.Value).Take(100).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Time = hours,
                    Songs = songs
                };

                return data;
            }
            catch
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
        public async Task<dynamic> GetTracksByYear(User user, long numberOfSongsInPlaylist)
        {
            if (user == null)
                return default;

            IList<HttpRequestMessage> trackRequests = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(album(id)))")
                };

                string spotifyTracksUrl = ConstructEndpoint(user, Method.GetPlaylistTracks, parameters);
                var request = CreateRequest(spotifyTracksUrl);

                trackRequests.Add(request);
                user.Offset += 100;
            }

            IList<string> albumIds = new List<string>();

            try
            {
                foreach (var request in trackRequests)
                {
                    var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    foreach (var item in playlist.Items)
                    {
                        albumIds.Add(item.Track.Album.Id);
                    }
                }
            }
            catch
            {
                throw;
            }

            IList<HttpRequestMessage> albumRequests = new List<HttpRequestMessage>();

            string spotifyAlbumsUrl = $"{Endpoint}albums/?ids=";

            StringBuilder albumIdsConcat;
            for (int i = 0; i < albumIds.Count; i += 20)
            {
                albumIdsConcat = new StringBuilder().AppendJoin(',', albumIds.Skip(i).Take(20));

                var request = CreateRequest(spotifyAlbumsUrl + albumIdsConcat.ToString());
                albumRequests.Add(request);
            }

            var songsByYear = new Dictionary<string, int>();

            try
            {
                foreach (var request in albumRequests)
                {
                    var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
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
            catch
            {
                throw;
            }
        }
    }
}