using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly SpotifyConfiguration SpotifyConfig;

        public readonly SpotifyService SpotifyService;

        public SpotifyApiController()
        {
            SpotifyConfig = new SpotifyConfiguration();
            SpotifyService = new SpotifyService();
        }

        [HttpGet("Favorites")]
        public async Task<IActionResult> Favorites()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                var user = new User(SpotifyConfig.UserId, SpotifyConfig.FavoritesId)
                {
                    Limit = 50
                };

                (string, string)[] parameters = {
                    ("limit", user.Limit.ToString())
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylist, parameters);
                var favorites = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);

                var random = new Random();

                if (favorites.Tracks == null)
                    throw new NullReferenceException();

                var favoriteTrackIds = favorites.Tracks.Items
                    .Select(t => t?.Track?.Id ?? "").OrderBy(order => random.Next())
                    .ToArray();

                var favoriteIds = new Favorites(favoriteTrackIds);

                return new OkObjectResult(favoriteIds);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("Genres")]
        public async Task<IActionResult> Genres()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                var user = new User(SpotifyConfig.UserId, SpotifyConfig.ExquisiteEdmId)
                {
                    Limit = 50
                };

                (string query, string value)[] parameters = {
                    ("limit", user.Limit.ToString())
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylists, parameters);
                var personalPlaylists = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);
                var orderedPlaylists = personalPlaylists.Items.OrderByDescending(t => t?.Tracks?.Total);

                user.Offset = personalPlaylists?.Items?[0].Tracks?.Total - 1 ?? 1L;

                var edmPlaylists = new Dictionary<string, (long, string)>();
                var listedGenres = new ListedGenres().Genres;

                foreach (var playlist in orderedPlaylists)
                {
                    if (string.IsNullOrWhiteSpace(playlist.Name) || string.IsNullOrWhiteSpace(playlist.Uri))
                        continue;

                    string name = playlist.Name;
                    bool isValid = listedGenres.Any(id => name.Contains(id));
                    if (isValid)
                    {
                        edmPlaylists.Add(name, (playlist.Tracks.Total, playlist.Uri));
                    }
                }

                var genres = new Genres(edmPlaylists);

                return new OkObjectResult(genres);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("TracksByYear")]
        public async Task<IActionResult> TracksByYear()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                var user = new User(SpotifyConfig.UserId, SpotifyConfig.ExquisiteEdmId)
                {
                    Limit = 50
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylist);
                Playlist playlist = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);

                if (playlist.Tracks == null)
                    throw new NullReferenceException();

                TracksByYear tracksByYear = await SpotifyService.Client.Spotify.GetTracksByYear(user, playlist.Tracks.Total);

                return new OkObjectResult(tracksByYear);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("IdDurationArtists")]
        public async Task<IActionResult> IdDurationArtists()
        {
            await SpotifyService.SetAuthorizationHeader();

            try
            {
                var user = new User(SpotifyConfig.UserId, SpotifyConfig.ExquisiteEdmId)
                {
                    Limit = 50
                };

                string spotifyUrl = SpotifyService.Client.Spotify.ConstructEndpoint(user, Method.GetPlaylist);
                var playlist = await SpotifyService.Client.Spotify.GetObject<Playlist>(spotifyUrl);

                if (playlist.Tracks == null)
                    throw new NullReferenceException();

                var artists = await SpotifyService.Client.Spotify.GetDetailsArtists<Playlist>(user, playlist.Tracks.Total);

                return new OkObjectResult(artists);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}