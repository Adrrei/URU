using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("favorites")]
        public async Task<IActionResult> Favorites()
        {
            await SpotifyService.SetAuthorizationHeader();

            var user = new User(SpotifyConfig.UserId, SpotifyConfig.FavoritesId)
            {
                Limit = 50,
            };

            (string query, string value)[] parameters = [("limit", user.Limit.ToString())];

            try
            {
                Favorites favoriteIds = await SpotifyService.Client.Spotify.GetFavoriteSongs(user, parameters);

                return new OkObjectResult(favoriteIds);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("genres")]
        public async Task<IActionResult> Genres()
        {
            await SpotifyService.SetAuthorizationHeader();

            var user = new User(SpotifyConfig.UserId, SpotifyConfig.ExquisiteEdmId)
            {
                Limit = 50,
            };

            (string query, string value)[] parameters = [("limit", user.Limit.ToString())];

            try
            {
                Genres genres = await SpotifyService.Client.Spotify.GetGenres(user, parameters);

                return new OkObjectResult(genres);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("tracksByYear")]
        public async Task<IActionResult> TracksByYear()
        {
            await SpotifyService.SetAuthorizationHeader();

            var user = new User(SpotifyConfig.UserId, SpotifyConfig.ExquisiteEdmId)
            {
                Limit = 50,
            };

            (string query, string value)[] parameters = null!;

            try
            {
                Playlist playlist = await SpotifyService.Client.Spotify.GetPlaylist(user, parameters);
                TracksByYear tracksByYear = await SpotifyService.Client.Spotify.GetTracksByYear(user, playlist.Tracks!.Total);

                return new OkObjectResult(tracksByYear);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("idDurationArtists")]
        public async Task<IActionResult> IdDurationArtists()
        {
            await SpotifyService.SetAuthorizationHeader();

            var user = new User(SpotifyConfig.UserId, SpotifyConfig.ExquisiteEdmId)
            {
                Limit = 50,
            };

            (string query, string value)[] parameters = null!;

            try
            {
                Playlist playlist = await SpotifyService.Client.Spotify.GetPlaylist(user, parameters);
                Artists artists = await SpotifyService.Client.Spotify.GetDetailsArtists<Playlist>(user, playlist.Tracks!.Total);

                return new OkObjectResult(artists);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}