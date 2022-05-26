using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using URU.Utilities;

namespace URU.Controllers
{
    [SecurityHeaders]
    public class ErrorController : Controller
    {
        public ErrorController()
        { }

        public IActionResult Index(int? statusCode)
        {
            if (statusCode == null)
            {
                statusCode = StatusCodes.Status404NotFound;
            }

            return View(statusCode);
        }
    }
}