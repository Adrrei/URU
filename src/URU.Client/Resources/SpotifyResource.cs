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
            var response = await Client.GetAsync(spotifyUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

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
            var urls = new List<string>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(id, duration_ms, artists(name, uri), album(id)))")
                };

                string spotifyUrl = ConstructEndpoint(user, Method.GetPlaylistTracks, parameters);

                urls.Add(spotifyUrl);
                user.Offset += 100;
            }

            var artistsCount = new Dictionary<string, (int, string)>();
            long milliseconds = 0;
            int songs = 0;

            try
            {
                foreach (var url in urls)
                {
                    var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    var playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    if (playlist.Items == null)
                        continue;

                    songs += playlist.Items.Length;
                    foreach (var item in playlist.Items)
                    {
                        if (item.Track?.Artists == null)
                            continue;

                        milliseconds += item.Track.DurationMs;
                        foreach (var artist in item.Track.Artists)
                        {
                            if (string.IsNullOrWhiteSpace(artist.Name))
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
            var playlistUrls = new List<string>();

            while (user.Offset < numberOfSongsInPlaylist)
            {
                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("fields", "items(track(id))")
                };

                string playlistUrl = ConstructEndpoint(user, Method.GetPlaylistTracks, parameters);

                playlistUrls.Add(playlistUrl);
                user.Offset += 100;
            }

            var trackIds = new List<string>();

            try
            {
                foreach (var url in playlistUrls)
                {
                    var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    var playlist = JsonConvert.DeserializeObject<Playlist>(result);

                    if (playlist.Items == null)
                        continue;

                    foreach (var item in playlist.Items)
                    {
                        if (string.IsNullOrWhiteSpace(item.Track?.Id))
                            continue;

                        trackIds.Add(item.Track.Id);
                    }
                }
            }
            catch
            {
                throw;
            }

            var trackUrls = new List<string>();

            string trackUrl = $"{Endpoint}/tracks/?ids=";

            StringBuilder trackIdsConcat;
            for (int i = 0; i < trackIds.Count; i += 50)
            {
                trackIdsConcat = new StringBuilder().AppendJoin(',', trackIds.Skip(i).Take(50));

                trackUrls.Add(trackUrl + trackIdsConcat.ToString());
            }

            var songsByYear = new Dictionary<string, int>();

            try
            {
                foreach (var url in trackUrls)
                {
                    var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string result = await response.Content.ReadAsStringAsync();
                    var tracks = JsonConvert.DeserializeObject<Tracks>(result);

                    if (tracks.AllTracks == null)
                        continue;

                    foreach (var track in tracks.AllTracks)
                    {
                        if (string.IsNullOrWhiteSpace(track?.Album?.ReleaseDate))
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