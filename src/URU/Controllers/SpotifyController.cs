using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<Startup>()
                .Build();

            User user = new User
            {
                UserId = configuration["spotify_userId"],
                PlaylistId = configuration["spotify_playlist_exquisiteEdmId"]
            };

            SpotifyViewModel spotifyViewModel = new SpotifyViewModel
            {
                User = user
            };

            return View(spotifyViewModel);
        }
    }
}