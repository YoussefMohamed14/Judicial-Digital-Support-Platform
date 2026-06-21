using JDSP.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JDSP.Controllers {
    [Authorize]
    public class DashboardController : Controller {
        [Authorize(Roles = Roles.Admin)]
        public IActionResult AdminDashboard() {
            return View();
        }

        [Authorize(Roles = Roles.CourtEmployee)]
        public IActionResult CourtEmployeeDashboard() {
            return View();
        }

        [Authorize(Roles = Roles.Lawyer)]
        public IActionResult LawyerDashboard() {
            return View();
        }

        [Authorize(Roles = Roles.Client)]
        public IActionResult ClientDashboard() {
            return View();
        }
    }
}