using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using URU.Models;
using URU.ViewModels;
using static URU.Models.Spotify;

namespace URU.Controllers
{
    [SecurityHeaders]
    public class SpotifyController : Controller
    {
        private Spotify _spotify;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<SpotifyController> _stringLocalizer;

        public SpotifyController(IConfiguration configuration, IStringLocalizer<SpotifyController> stringLocalizer)
        {
            _configuration = configuration;
            _stringLocalizer = stringLocalizer;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = _stringLocalizer["TitleSpotify"];

            var sectionSpotify = _configuration.GetSection("Spotify");
            User user = new User
            {
                UserId = sectionSpotify["UserId"],
                PlaylistId = sectionSpotify["ExquisiteEdmId"],
            };

            Spotify spotify = new Spotify(_configuration);

            string spotifyUrl = spotify.GetEndpoint(user, Method.GetPlaylist);
            Playlist exquisiteEdm = await spotify.GetSpotify<Playlist>(spotifyUrl);

            Playlist playlist = new Playlist()
            {
                Name = exquisiteEdm.Name,
                Uri = exquisiteEdm.Uri,
                Total = exquisiteEdm.Tracks.Total
            };

            SpotifyViewModel spotifyViewModel = new SpotifyViewModel
            {
                User = user,
                ExquisiteEdm = playlist
            };

            return View(spotifyViewModel);
        }

        private void EnsureSpotifyExist()
        {
            if (_spotify == null)
            {
                _spotify = new Spotify(_configuration);
            }
        }

        public async Task<JsonResult> GetFavorites()
        {
            try
            {
                var sectionSpotify = _configuration.GetSection("Spotify");
                User user = new User
                {
                    UserId = sectionSpotify["UserId"],
                    PlaylistId = sectionSpotify["FavoritesId"],
                    Limit = 50
                };

                EnsureSpotifyExist();

                (string, string)[] parameters = {
                    ("limit", user.Limit.ToString())
                };

                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylist, parameters);
                Playlist favorites = await _spotify.GetSpotify<Playlist>(spotifyUrl);

                var result = new
                {
                    Favorites = favorites.Tracks.Items.Select(t => t.Track.Id)
                };

                return Json(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<JsonResult> GetGenres()
        {
            try
            {
                var sectionSpotify = _configuration.GetSection("Spotify");
                User user = new User
                {
                    UserId = sectionSpotify["UserId"],
                    PlaylistId = sectionSpotify["ExquisiteEdmId"],
                    Limit = 50
                };

                EnsureSpotifyExist();

                (string query, string value)[] parameters = {
                    ("limit", user.Limit.ToString())
                };

                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylists, parameters);
                Playlist personalPlaylists = await _spotify.GetSpotify<Playlist>(spotifyUrl);
                user.Offset = personalPlaylists.Items[0].Tracks.Total - 1;

                Dictionary<string, (long, string)> edmPlaylists = new Dictionary<string, (long, string)>();
                List<string> genres = new List<string>
                {
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

                return Json(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetTracksByYear()
        {
            try
            {
                var sectionSpotify = _configuration.GetSection("Spotify");
                User user = new User
                {
                    UserId = sectionSpotify["UserId"],
                    PlaylistId = sectionSpotify["ExquisiteEdmId"],
                    Limit = 0
                };

                EnsureSpotifyExist();
                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylist);
                Playlist playlist = await _spotify.GetSpotify<Playlist>(spotifyUrl);

                var data = await _spotify.GetTracksByYear(user, playlist.Tracks.Total);

                return Json(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetIdDurationArtists()
        {
            try
            {
                var sectionSpotify = _configuration.GetSection("Spotify");
                User user = new User
                {
                    UserId = sectionSpotify["UserId"],
                    PlaylistId = sectionSpotify["ExquisiteEdmId"],
                    Limit = 0
                };

                EnsureSpotifyExist();

                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylist);
                Playlist playlist = await _spotify.GetSpotify<Playlist>(spotifyUrl);

                var data = await _spotify.GetIdDurationArtists<Playlist>(user, playlist.Tracks.Total);

                return Json(data);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}