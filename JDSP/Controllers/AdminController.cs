using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller {
        private readonly UserManager<ApplicationUser> _users;

        public AdminController(UserManager<ApplicationUser> users) {
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> CourtEmployees() {
            var employees = await _users.GetUsersInRoleAsync(Roles.CourtEmployee);
            var model = employees
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .Select(x => new CourtEmployeeAccountListViewModel {
                    Id = x.Id,
                    FullName = $"{x.FirstName} {x.LastName}",
                    Email = x.Email ?? string.Empty,
                    PhoneNumber = x.PhoneNumber ?? string.Empty,
                    NationalNumber = x.NationalNumber,
                    AccountStatus = x.AccountStatus,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CourtEmployeeDetails(string id) {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var employee = await _users.FindByIdAsync(id);
            if (employee == null || !await _users.IsInRoleAsync(employee, Roles.CourtEmployee)) return NotFound();

            return View(new CourtEmployeeAccountDetailsViewModel {
                Id = employee.Id,
                FullName = $"{employee.FirstName} {employee.MiddleName} {employee.LastName}".Replace("  ", " ").Trim(),
                Email = employee.Email ?? string.Empty,
                PhoneNumber = employee.PhoneNumber ?? string.Empty,
                NationalNumber = employee.NationalNumber,
                AccountStatus = employee.AccountStatus,
                CreatedAt = employee.CreatedAt,
                MustChangePassword = employee.MustChangePassword,
                PreferredLanguage = employee.PreferredLanguage
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCourtEmployee(string id) {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var employee = await _users.FindByIdAsync(id);
            if (employee == null || !await _users.IsInRoleAsync(employee, Roles.CourtEmployee)) return NotFound();

            employee.AccountStatus = "Disabled";
            employee.LockoutEnabled = true;
            employee.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

            var result = await _users.UpdateAsync(employee);
            if (!result.Succeeded) {
                TempData["Error"] = Text("Could not remove this court employee account.", "تعذر إزالة حساب موظف المحكمة.");
                return RedirectToAction(nameof(CourtEmployees));
            }

            TempData["Success"] = Text("Court employee account removed from active access.", "تمت إزالة صلاحية دخول موظف المحكمة.");
            return RedirectToAction(nameof(CourtEmployees));
        }

        [HttpGet]
        public IActionResult CreateCourtEmployee() => View(new CreateCourtEmployeeViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourtEmployee(CreateCourtEmployeeViewModel model) {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim();
            var national = model.NationalNumber.Trim();

            if (await _users.FindByEmailAsync(email) != null)
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");

            if (_users.Users.Any(x => x.NationalNumber == national))
                ModelState.AddModelError(nameof(model.NationalNumber), "This national number is already registered.");

            if (!ModelState.IsValid) return View(model);

            var employee = new ApplicationUser {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = model.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(),
                LastName = model.LastName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                NationalNumber = national,
                AccountStatus = "Active",
                LawyerApprovalStatus = VerificationStatus.NotRequired,
                PreferredLanguage = "en",
                IsProfileCompleted = true,
                MustChangePassword = true,
                CreatedAt = DateTime.Now
            };

            var result = await _users.CreateAsync(employee, model.TemporaryPassword);
            if (!result.Succeeded) {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _users.AddToRoleAsync(employee, Roles.CourtEmployee);
            TempData["Success"] = "Court employee account created.";
            return RedirectToAction(nameof(CourtEmployees));
        }

        private string Text(string en, string ar) => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;
    }
}
