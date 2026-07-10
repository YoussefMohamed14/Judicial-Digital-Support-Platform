using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace JDSP.Controllers {
    [AllowAnonymous]
    public class ErrorController : Controller {
        [Route("Error/{code?}")]
        public IActionResult Index(int? code = null) {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            ViewBag.Code = code ?? 500;
            ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            ViewBag.Path = feature?.Path ?? HttpContext.Request.Path.Value;
            return View();
        }
    }
}
