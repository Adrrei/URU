using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using URU.Utilities;
using URU.ViewModels;

namespace URU.Controllers
{
    [SecurityHeaders]
    public class MastersController : Controller
    {
        private readonly IStringLocalizer<MastersController> _stringLocalizer;

        public MastersController(IStringLocalizer<MastersController> stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;
        }

        public IActionResult Index()
        {
            ViewBag.Title = _stringLocalizer["TitleMasters"];

            return View(new MastersViewModel());
        }
    }
}