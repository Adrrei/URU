using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using URU.Models;
using URU.ViewModels;
using static URU.Models.Spotify;

namespace URU.Controllers
{
    public class HomeController : Controller
    {
        private Spotify _spotify;
        private readonly IConfiguration _configuration;
        private readonly IRepository _repository;
        private readonly IStringLocalizer<HomeController> _stringLocalizer;

        public HomeController(IConfiguration configuration, IRepository repository, IStringLocalizer<HomeController> stringLocalizer)
        {
            _configuration = configuration;
            _repository = repository;
            _stringLocalizer = stringLocalizer;
        }

        [HttpGet]
        public IActionResult Contact()
        {
            ViewBag.Title = _stringLocalizer["HomeController_Contact"];
            ContactViewModel contactViewModel = new ContactViewModel
            { };

            return View(contactViewModel);
        }

        [HttpPost]
        public IActionResult Contact(ContactViewModel contactViewModel)
        {
            ViewBag.Title = _stringLocalizer["HomeController_Contact"];

            bool isEmailValid = contactViewModel.Email != null;
            try
            {
                if (isEmailValid)
                {
                    new MailAddress(contactViewModel.Email);
                }
            }
            catch (FormatException)
            {
                isEmailValid = false;
            }

            string message = _stringLocalizer["HomeController_ContactError"];
            if (!ModelState.IsValid || !isEmailValid)
            {
                TempData["Error"] = message;
                return View(contactViewModel);
            }

            Contact contact = new Contact()
            {
                DateTime = DateTime.Now,
                Email = contactViewModel.Email,
                Message = contactViewModel.Message,
                Name = contactViewModel.Name,
                PhoneNumber = contactViewModel.Phone,
                Subject = contactViewModel.Subject
            };

            _repository.AddContact(contact);

            message = _stringLocalizer["HomeController_ContactSuccess"];
            TempData["Success"] = message;

            return RedirectToAction("Contact");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Error(int? statusCode = null)
        {
            ViewBag.Title = _stringLocalizer["HomeController_Error"];
            if (statusCode.HasValue && statusCode.Value == 404)
                return View("Error/" + statusCode.ToString());

            return View("Error/Error");
        }

        [HttpGet]
        public IActionResult Index(string id)
        {
            if (string.Equals(id, "projects") || string.Equals(id, "about-me"))
            {
                var url = Url.Action(nameof(Index));
                url = url.Substring(0, url.LastIndexOf('/')) + "#" + url.Substring(url.LastIndexOf('/') + 1);
                return Redirect(url);
            }

            ViewBag.Title = _stringLocalizer["HomeController_Home"];
            HomeViewModel homeViewModel = new HomeViewModel
            { };

            return View(homeViewModel);
        }

        [HttpGet]
        public IActionResult Masters()
        {
            ViewBag.Title = _stringLocalizer["HomeController_Masters"];
            MastersViewModel mastersViewModel = new MastersViewModel
            { };

            return View(mastersViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Spotify()
        {
            ViewBag.Title = _stringLocalizer["HomeController_Spotify"];

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

        private void CreateSpotifyClient()
        {
            _spotify = new Spotify(_configuration);
        }

        private void EnsureSpotifyCreated()
        {
            if (_spotify == null)
            {
                CreateSpotifyClient();
            }
        }

        public async Task<JsonResult> GetSpotifyFavorites()
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

                EnsureSpotifyCreated();

                (string, string)[] parameters = {
                    ("limit", user.Limit.ToString())
                };

                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylist, parameters);
                Playlist favorites = await _spotify.GetSpotify<Playlist>(spotifyUrl);
                IEnumerable<Item> favoriteItems = IEnumerableHelper.Randomize(favorites.Tracks.Items);
                favoriteItems = favoriteItems.Take(5);

                var result = new { Favorites = favoriteItems };

                return Json(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<JsonResult> GetSpotifyPlaylists()
        {
            try
            {
                var sectionSpotify = _configuration.GetSection("Spotify");
                User user = new User
                {
                    UserId = sectionSpotify["UserId"],
                    PlaylistId = sectionSpotify["ExquisiteEdmId"],
                };

                EnsureSpotifyCreated();

                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylists);
                Playlist personalPlaylists = await _spotify.GetSpotify<Playlist>(spotifyUrl);
                user.Offset = personalPlaylists.Items[0].Tracks.Total - 1;

                Dictionary<string, long> edmPlaylists = new Dictionary<string, long>();
                List<string> keepPlaylists = new List<string>
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
                    bool isValid = keepPlaylists.Any(id => name.Contains(id));
                    if (isValid)
                    {
                        edmPlaylists.Add(name, playlist.Tracks.Total);
                    }
                }

                (string, string)[] parameters = {
                    ("offset", user.Offset.ToString()),
                    ("limit", user.Limit.ToString())
                };
                spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylistTracks, parameters);
                Playlist exquisiteEdm = await _spotify.GetSpotify<Playlist>(spotifyUrl);

                var result = new { ExquisiteEdm = exquisiteEdm, Playlists = edmPlaylists };

                return Json(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<JsonResult> GetSpotifyPlaytime()
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

                EnsureSpotifyCreated();

                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylist);
                Playlist playlist = await _spotify.GetSpotify<Playlist>(spotifyUrl);

                long milliseconds = await _spotify.GetSpotifyPlaytime<Playlist>(user, playlist.Tracks.Total);
                int hoursOfPlaytime = (int)TimeSpan.FromMilliseconds(milliseconds).TotalHours;

                var result = new { Hours = hoursOfPlaytime };
                return Json(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<JsonResult> GetSpotifyTopArtists()
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

                EnsureSpotifyCreated();

                string spotifyUrl = _spotify.GetEndpoint(user, Method.GetPlaylist);
                Playlist playlist = await _spotify.GetSpotify<Playlist>(spotifyUrl);

                Dictionary<string, int> artists = await _spotify.GetSpotifyTopArtists<Playlist>(user, playlist.Tracks.Total);
                var topTenArtists = artists.OrderByDescending(a => a.Value).Take(10);

                var result = new { Artists = topTenArtists };
                return Json(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Route("Home/SetLanguage")]
        public IActionResult SetLanguage(string returnUrl)
        {
            bool initialView = false;
            string cultureCookie = Request.Cookies[".AspNetCore.Culture"];

            if (null == cultureCookie)
            {
                cultureCookie = "en-US";
                initialView = true;
            }
            string locale = cultureCookie.Contains("nb-NO") ? "en-US" : "nb-NO";
            locale = initialView ? locale.Contains("nb-NO") ? "en-US" : "nb-NO" : locale;

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(locale)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) }
            );

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }

    public static class IEnumerableHelper
    {
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            Random random = new Random();
            return source.OrderBy((item) => random.Next());
        }
    }
}