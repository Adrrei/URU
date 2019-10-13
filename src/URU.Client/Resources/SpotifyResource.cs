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
        private readonly string Endpoint;
        private readonly HttpClient Client;

        public SpotifyResource(HttpClient client)
        {
            Endpoint = "https://api.spotify.com/v1";
            Client = client;
        }

        public string ConstructEndpoint(User user, Method method, (string query, string value)[] parameters = null!)
        {
            var spotifyUrl = new StringBuilder(Endpoint);

            _ = method switch
            {
                Method.GetPlaylist => spotifyUrl.Append($"/users/{user.UserId}/playlists/{user.PlaylistId}"),
                Method.GetPlaylists => spotifyUrl.Append($"/users/{user.UserId}/playlists"),
                Method.GetPlaylistTracks => spotifyUrl.Append($"/playlists/{user.PlaylistId}/tracks"),
                _ => throw new NotSupportedException()
            };

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

        public async Task<T> GetObject<T>(string spotifyUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, spotifyUrl);
            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            try
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException();

                string result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(result);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Artists> GetDetailsArtists<T>(User user, long numberOfSongsInPlaylist)
        {
            var urls = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(id, duration_ms, artists(name, uri), album(id)))")
                };

                string spotifyUrl = ConstructEndpoint(user, Method.GetPlaylistTracks, parameters);
                var request = new HttpRequestMessage(HttpMethod.Get, spotifyUrl);

                urls.Add(request);
                user.Offset += 100;
            }

            var artistsCount = new Dictionary<string, (int, string)>();
            long milliseconds = 0;
            int songs = 0;

            try
            {
                foreach (var request in urls)
                {
                    var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    var playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    songs += playlist.Items?.Length ?? 0;
                    foreach (var item in playlist.Items ?? new Item[0])
                    {
                        if (item.Track == null)
                            continue;

                        milliseconds += item.Track.DurationMs;
                        foreach (var artist in item.Track.Artists ?? new Owner[0])
                        {
                            if (item.Track.Artists == null || string.IsNullOrWhiteSpace(artist.Name))
                                continue;

                            if (artistsCount.TryGetValue(artist.Name, out (int Count, string Uri) val))
                            {
                                artistsCount[artist.Name] = (val.Count + 1, val.Uri);
                            }
                            else if (!string.IsNullOrWhiteSpace(artist.Uri))
                            {
                                artistsCount.Add(artist.Name, (1, artist.Uri));
                            }
                        }
                    }
                }

                return new Artists
                {
                    Counts = artistsCount.OrderByDescending(a => a.Value).Take(100).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Hours = (int)TimeSpan.FromMilliseconds(milliseconds).TotalHours,
                    Songs = songs
                };
            }
            catch
            {
                throw;
            }
        }

        public async Task<TracksByYear> GetTracksByYear(User user, long numberOfSongsInPlaylist)
        {
            var trackRequests = new List<HttpRequestMessage>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(id))")
                };

                string spotifyTracksUrl = ConstructEndpoint(user, Method.GetPlaylistTracks, parameters);
                var request = new HttpRequestMessage(HttpMethod.Get, spotifyTracksUrl);

                trackRequests.Add(request);
                user.Offset += 100;
            }

            var trackIds = new List<string>();

            try
            {
                foreach (var request in trackRequests)
                {
                    var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    var playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    foreach (var item in playlist.Items ?? new Item[0])
                    {
                        if (item.Track == null || string.IsNullOrWhiteSpace(item.Track.Id))
                            continue;

                        trackIds.Add(item.Track.Id);
                    }
                }
            }
            catch
            {
                throw;
            }

            var albumRequests = new List<HttpRequestMessage>();

            string spotifyAlbumsUrl = $"{Endpoint}/tracks/?ids=";

            StringBuilder trackIdsConcat;
            for (int i = 0; i < trackIds.Count; i += 50)
            {
                trackIdsConcat = new StringBuilder().AppendJoin(',', trackIds.Skip(i).Take(50));

                var request = new HttpRequestMessage(HttpMethod.Get, spotifyAlbumsUrl + trackIdsConcat.ToString());
                albumRequests.Add(request);
            }

            var songsByYear = new Dictionary<string, int>();

            try
            {
                foreach (var request in albumRequests)
                {
                    var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    var tracks = JsonConvert.DeserializeObject<Tracks>(result);

                    foreach (var track in tracks.AllTracks ?? new Track[0])
                    {
                        if (track.Album == null || string.IsNullOrWhiteSpace(track.Album.ReleaseDate))
                            continue;

                        string year = track.Album.ReleaseDate.Substring(0, 4);

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

                return new TracksByYear()
                {
                    Counts = songsByYear.OrderByDescending(a => a.Key).ToArray()
                };
            }
            catch
            {
                throw;
            }
        }
    }
}