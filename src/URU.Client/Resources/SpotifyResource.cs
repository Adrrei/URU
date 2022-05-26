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

        public string ConstructEndpoint(User user, Method method, (string query, string value)[]? parameters)
        {
            var spotifyUrl = new StringBuilder(Endpoint);

            _ = method switch
            {
                Method.GetPlaylist => spotifyUrl.Append($"/users/{user.UserId}/playlists/{user.PlaylistId}"),
                Method.GetPlaylists => spotifyUrl.Append($"/users/{user.UserId}/playlists"),
                Method.GetPlaylistTracks => spotifyUrl.Append($"/playlists/{user.PlaylistId}/tracks"),
                _ => throw new NotSupportedException()
            };

            if (parameters?.Length > 0)
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

        private async Task<T> GetObject<T>(string spotifyUrl)
        {
            var response = await Client.GetAsync(spotifyUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException();

            string result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(result)!;
        }

        private async Task<Item[]> QueryPlaylistItems(string playlistUrl)
        {
            var response = await Client.GetAsync(playlistUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            string result = await response.Content.ReadAsStringAsync();
            var playlist = JsonConvert.DeserializeObject<Playlist>(result);

            if (playlist?.Items == null)
                return Array.Empty<Item>();

            return playlist.Items;
        }

        private async Task<Track[]> QueryAllTracks(string trackUrl)
        {
            var response = await Client.GetAsync(trackUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            string result = await response.Content.ReadAsStringAsync();
            var tracks = JsonConvert.DeserializeObject<Tracks>(result);

            if (tracks?.AllTracks == null)
                return Array.Empty<Track>();

            return tracks.AllTracks;
        }

        public async Task<Playlist> GetPlaylist(User user, (string, string)[]? parameters)
        {
            string spotifyUrl = ConstructEndpoint(user, Method.GetPlaylist, parameters);
            var playlist = await GetObject<Playlist>(spotifyUrl);

            if (playlist.Tracks == null)
                throw new NullReferenceException();

            return playlist;
        }

        public async Task<Playlist> GetPlaylists(User user, (string, string)[]? parameters)
        {
            string spotifyUrl = ConstructEndpoint(user, Method.GetPlaylists, parameters);
            var playlist = await GetObject<Playlist>(spotifyUrl);

            if (playlist.Items == null)
                throw new NullReferenceException();

            return playlist;
        }

        public async Task<Favorites> GetFavoriteSongs(User user, (string, string)[] parameters)
        {
            Playlist favorites = await GetPlaylist(user, parameters);

            var random = new Random();

            var favoriteTrackIds = favorites.Tracks!.Items
                !.Select(t => t?.Track?.Id ?? "").OrderBy(order => random.Next())
                .ToArray();

            return new Favorites(favoriteTrackIds);
        }

        public async Task<Genres> GetGenres(User user, (string, string)[] parameters)
        {
            Playlist personalPlaylist = await GetPlaylists(user, parameters);
            var orderedPlaylists = personalPlaylist.Items!.OrderByDescending(t => t?.Tracks?.Total);

            user.Offset = personalPlaylist.Items![0].Tracks?.Total - 1 ?? 1L;

            var playlists = new Dictionary<string, (long, string)>();
            var listedGenres = new ListedGenres().Genres;

            foreach (var playlist in orderedPlaylists)
            {
                if (string.IsNullOrWhiteSpace(playlist.Name) || string.IsNullOrWhiteSpace(playlist.Uri))
                    continue;

                string name = playlist.Name;
                bool isValid = listedGenres.Any(id => name.Contains(id));
                if (isValid && playlist.Tracks != null)
                {
                    playlists.Add(name, (playlist.Tracks.Total, playlist.Uri));
                }
            }

            return new Genres(playlists);
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

            foreach (var url in playlistUrls)
            {
                Item[] playlistItems = await QueryPlaylistItems(url);

                foreach (var item in playlistItems)
                {
                    if (string.IsNullOrWhiteSpace(item.Track?.Id))
                        continue;

                    trackIds.Add(item.Track.Id);
                }
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

            foreach (var url in trackUrls)
            {
                Track[] allTracks = await QueryAllTracks(url);

                foreach (var track in allTracks)
                {
                    if (string.IsNullOrWhiteSpace(track?.Album?.ReleaseDate))
                        continue;

                    string year = track.Album.ReleaseDate[..4];

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

            foreach (var url in urls)
            {
                Item[] playlistItems = await QueryPlaylistItems(url);

                if (playlistItems.Length == 0)
                    continue;

                songs += playlistItems.Length;
                foreach (var item in playlistItems)
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
    }
}