using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.Client + "," + Roles.Lawyer)]
    public class MessagesController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> users) {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var currentUser = await _users.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var contacts = new List<MessageContactViewModel>();

            if (await _users.IsInRoleAsync(currentUser, Roles.Lawyer)) {
                contacts.AddRange(await _db.LegalServiceRequests.AsNoTracking()
                    .Where(x => x.LawyerId == currentUser.Id && x.Status == "Accepted")
                    .Select(x => new MessageContactViewModel {
                        UserId = x.ClientId,
                        FullName = x.Client.FirstName + " " + x.Client.LastName,
                        PhotoPath = x.Client.PhotoPath,
                        Relationship = "Accepted direct request"
                    })
                    .ToListAsync());

                contacts.AddRange(await _db.CaseLawyers.AsNoTracking()
                    .Where(x => x.LawyerId == currentUser.Id && x.Case != null &&
                        (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                    .Select(x => new MessageContactViewModel {
                        UserId = x.Case!.CreatedBy_Id,
                        FullName = x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName,
                        PhotoPath = x.Case.Creator == null ? null : x.Case.Creator.PhotoPath,
                        Relationship = "Assigned case"
                    })
                    .ToListAsync());
            }
            else {
                contacts.AddRange(await _db.LegalServiceRequests.AsNoTracking()
                    .Where(x => x.ClientId == currentUser.Id && x.LawyerId != null && x.Status == "Accepted")
                    .Select(x => new MessageContactViewModel {
                        UserId = x.LawyerId!,
                        FullName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                        PhotoPath = x.Lawyer == null ? null : x.Lawyer.PhotoPath,
                        Relationship = "Accepted direct request"
                    })
                    .ToListAsync());

                contacts.AddRange(await _db.CaseLawyers.AsNoTracking()
                    .Where(x => x.Case != null && x.Case.CreatedBy_Id == currentUser.Id &&
                        (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                    .Select(x => new MessageContactViewModel {
                        UserId = x.LawyerId,
                        FullName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                        PhotoPath = x.Lawyer == null ? null : x.Lawyer.PhotoPath,
                        Relationship = "Assigned lawyer"
                    })
                    .ToListAsync());
            }

            var uniqueContacts = contacts
                .GroupBy(x => x.UserId)
                .Select(x => x.First())
                .OrderBy(x => x.FullName)
                .ToList();

            return View(uniqueContacts);
        }
    }
}
