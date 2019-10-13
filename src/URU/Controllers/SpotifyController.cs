using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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

            string userId = "1157411586";
            string exquisiteEdmId = "7ssZYYankNsiAfeyPATtXe";

            var user = new User(userId, exquisiteEdmId);

            return View(new SpotifyViewModel(user));
        }
    }
}