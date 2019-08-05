using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using URU.Client.Data;
using URU.Models;
using URU.Services;

namespace URU.Controllers
{
    [ApiController]
    [Route("api/Spotify")]
    [Produces("application/json")]
    public class SpotifyApiController : ControllerBase
    {
        private static SpotifyConfiguration SpotifyConfig;

        public SpotifyService SpotifyService { get; }

        public SpotifyApiController()
        {
            var configuration = new ConfigurationBuilder()
               .AddEnvironmentVariables()
               .AddUserSecrets<Program>()
               .Build();

            SpotifyConfig = new SpotifyConfiguration()
            {
                ExquisiteEdmId = configuration["spotify_playlist_exquisiteEdmId"],
                FavoritesId = configuration["spotify_playlist_favoritesId"],
                UserId = configuration["spotify_userId"]
            };

            SpotifyService = new SpotifyService();
        }

        [HttpGet("Favorites")]
        public async Task<IActionResult> Favorites()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                User user = new User
                {
                    UserId = SpotifyConfig.UserId,
                    PlaylistId = SpotifyConfig.FavoritesId,
                    Limit = 50
                };

                (string, string)[] parameters = {
                    ("limit", user.Limit.ToString())
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylist, parameters);
                Playlist favorites = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);

                Random random = new Random();
                var result = new
                {
                    Favorites = favorites.Tracks.Items.Select(t => t.Track.Id).OrderBy(order => random.Next())
                };

                return new OkObjectResult(result);
            }
            catch
            {
                return new StatusCodeResult(500);
            }
        }

        [HttpGet("Genres")]
        public async Task<IActionResult> Genres()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                User user = new User
                {
                    UserId = SpotifyConfig.UserId,
                    PlaylistId = SpotifyConfig.ExquisiteEdmId,
                    Limit = 50
                };

                (string query, string value)[] parameters = {
                    ("limit", user.Limit.ToString())
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylists, parameters);
                Playlist personalPlaylists = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);
                user.Offset = personalPlaylists.Items[0].Tracks.Total - 1;

                Dictionary<string, (long, string)> edmPlaylists = new Dictionary<string, (long, string)>();
                List<string> genres = new List<string>
                {
                    "Bass House",
                    "Big Room",
                    "Breakbeat",
                    "Dance",
                    "Drum & Bass",
                    "Dubstep",
                    "Electronica / Downtempo",
                    "Future Bass",
                    "Glitch Hop",
                    "Hard Electronic",
                    "Deep House",
                    "Electro House",
                    "Future House",
                    "House",
                    "Progressive House",
                    "Indie Dance / Nu Disco",
                    "Tech House",
                    "Trance",
                    "Trap"
                };

                foreach (var playlist in personalPlaylists.Items.OrderByDescending(t => t.Tracks.Total))
                {
                    string name = playlist.Name;
                    bool isValid = genres.Any(id => name.Contains(id));
                    if (isValid)
                    {
                        edmPlaylists.Add(name, (playlist.Tracks.Total, playlist.Uri));
                    }
                }

                var data = new
                {
                    Genres = edmPlaylists
                };

                return new OkObjectResult(data);
            }
            catch
            {
                return new StatusCodeResult(500);
            }
        }

        [HttpGet("TracksByYear")]
        public async Task<IActionResult> TracksByYear()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                User user = new User
                {
                    UserId = SpotifyConfig.UserId,
                    PlaylistId = SpotifyConfig.ExquisiteEdmId,
                    Limit = 0
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylist);
                Playlist playlist = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);

                var data = await SpotifyService.Client.Spotify.GetTracksByYear(user, playlist.Tracks.Total);

                return new OkObjectResult(data);
            }
            catch
            {
                return new StatusCodeResult(500);
            }
        }

        [HttpGet("IdDurationArtists")]
        public async Task<IActionResult> IdDurationArtists()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                User user = new User
                {
                    UserId = SpotifyConfig.UserId,
                    PlaylistId = SpotifyConfig.ExquisiteEdmId,
                    Limit = 0
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylist);
                Playlist playlist = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);

                var data = await SpotifyService.Client.Spotify.GetDetailsArtists<Playlist>(user, playlist.Tracks.Total);

                return new OkObjectResult(data);
            }
            catch
            {
                return new StatusCodeResult(500);
            }
        }
    }
}