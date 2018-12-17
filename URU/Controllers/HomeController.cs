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
        public IActionResult Spotify()
        {
            ViewBag.Title = _stringLocalizer["HomeController_Spotify"];

            const string MY_USER = "11157411586";
            const string EXQUISITE_EDM = "7ssZYYankNsiAfeyPATtXe";
            User user = new User
            {
                UserId = MY_USER,
                PlaylistId = EXQUISITE_EDM,
                Offset = 0,
                Limit = 1
            };

            Spotify spotify = new Spotify(_configuration);
            Playlist exquisiteEdm = spotify.GetSpotify<Playlist>(user, Method.GetPlaylist);

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
        
        public JsonResult GetSpotifyFavorites()
        {
            try
            {
                const string MY_USER = "11157411586";
                const string EXQUISITE_EDM = "48HcflR8QplI2zgAutNDnT";
                User user = new User
                {
                    UserId = MY_USER,
                    PlaylistId = EXQUISITE_EDM,
                    Offset = 0,
                    Limit = 1
                };

                Spotify spotify = new Spotify(_configuration);

                Playlist favorites = spotify.GetSpotify<Playlist>(user, Method.GetPlaylist);
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
                const string MY_USER = "11157411586";
                const string EXQUISITE_EDM = "7ssZYYankNsiAfeyPATtXe";
                User user = new User
                {
                    UserId = MY_USER,
                    PlaylistId = EXQUISITE_EDM,
                    Offset = 0,
                    Limit = 1
                };

                Spotify spotify = new Spotify(_configuration);
                Playlist personalPlaylists = spotify.GetSpotify<Playlist>(user, Method.GetPlaylists);
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

                Playlist exquisiteEdm = await spotify.GetPlaylists<Playlist>(user);
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
                const string MY_USER = "11157411586";
                const string EXQUISITE_EDM = "7ssZYYankNsiAfeyPATtXe";
                User user = new User
                {
                    UserId = MY_USER,
                    PlaylistId = EXQUISITE_EDM,
                    Offset = 0
                };

                Spotify spotify = new Spotify(_configuration);
                Playlist playlist = spotify.GetSpotify<Playlist>(user, Method.GetPlaylist);

                long milliseconds = await spotify.GetSpotifyPlaytime<Playlist>(user, playlist.Tracks.Total);
                int hoursOfPlaytime = (int)TimeSpan.FromMilliseconds(milliseconds).TotalHours;

                var result = new { Hours = hoursOfPlaytime };
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