using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using URU.Client.Data;
using URU.Models;
using URU.Utilities;
using URU.ViewModels;

namespace URU.Controllers
{
    [SecurityHeaders]
    public class SpotifyController : Controller
    {
        private readonly IStringLocalizer<SpotifyController> _stringLocalizer;

        public SpotifyController(IStringLocalizer<SpotifyController> stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;
        }

        public IActionResult Index()
        {
            ViewBag.Title = _stringLocalizer["TitleSpotify"];

            var spotifyConfiguration = new SpotifyConfiguration();
            var user = new User(spotifyConfiguration.UserId, spotifyConfiguration.ExquisiteEdmId);

            return View(new SpotifyViewModel(user));
        }
    }
}