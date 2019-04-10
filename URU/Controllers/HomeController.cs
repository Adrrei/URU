using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using URU.Models;
using URU.ViewModels;

namespace URU.Controllers
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IStringLocalizer<HomeController> _stringLocalizer;

        public HomeController(IStringLocalizer<HomeController> stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "URU";

            return View(new HomeViewModel());
        }

        public IActionResult Error(int? statusCode = null)
        {
            ViewBag.Title = _stringLocalizer["TitleError"];

            if (statusCode.HasValue && statusCode.Value == StatusCodes.Status404NotFound)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("Error/" + statusCode.ToString());
            }

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View("Error/Error");
        }

        public IActionResult Masters()
        {
            ViewBag.Title = _stringLocalizer["TitleMasters"];

            return View(new MastersViewModel());
        }

        public IActionResult TicTacToe()
        {
            ViewBag.Title = _stringLocalizer["TitleTicTacToe"];

            return View(new GameViewModel());
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
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}