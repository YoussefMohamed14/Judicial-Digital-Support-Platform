using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.ServiceRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    [Authorize]
    public class ServiceRequestsController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IWebHostEnvironment _environment;

        public ServiceRequestsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users,
            IWebHostEnvironment environment) {
            _db = db;
            _users = users;
            _environment = environment;
        }


        [HttpGet]
        public async Task<IActionResult> Download(int id) {
            var currentUser = await _users.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var request = await _db.LegalServiceRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.LegalServiceRequestId == id);

            if (request == null || string.IsNullOrWhiteSpace(request.FilePath)) return NotFound();

            var isClientOwner = await _users.IsInRoleAsync(currentUser, Roles.Client) && request.ClientId == currentUser.Id;
            var isDirectLawyer = await _users.IsInRoleAsync(currentUser, Roles.Lawyer) && request.LawyerId == currentUser.Id;
            var isPublicLawyer = await _users.IsInRoleAsync(currentUser, Roles.Lawyer) && request.RequestType == "Public";
            if (!isClientOwner && !isDirectLawyer && !isPublicLawyer) return Forbid();

            var path = RequestFileHelper.GetPhysicalPath(request.FilePath, _environment);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path, "application/octet-stream", request.OriginalFileName ?? "case-file");
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> SendToLawyer(int lawyerProfileId) {
            var lawyer = await _db.LawyerProfiles
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.LawyerProfileId == lawyerProfileId);

            if (lawyer == null) return NotFound();

            return View(new CreateServiceRequestViewModel {
                LawyerProfileId = lawyer.LawyerProfileId,
                LawyerId = lawyer.UserId,
                LawyerName = $"{lawyer.User.FirstName} {lawyer.User.LastName}"
            });
        }

        [Authorize(Roles = Roles.Client), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToLawyer(CreateServiceRequestViewModel model) {
            var client = await _users.GetUserAsync(User);
            if (client == null) return Challenge();

            var lawyer = string.IsNullOrWhiteSpace(model.LawyerId)
                ? null
                : await _users.FindByIdAsync(model.LawyerId);

            if (lawyer == null || !await _users.IsInRoleAsync(lawyer, Roles.Lawyer)) {
                ModelState.AddModelError(string.Empty, "The selected lawyer could not be found.");
            }
            else if (!await _db.LawyerProfiles.AnyAsync(x => x.UserId == lawyer.Id && x.IsAvailable)) {
                ModelState.AddModelError(string.Empty, "This lawyer is not currently available for new requests.");
            }

            if (!ModelState.IsValid) {
                model.LawyerName = lawyer == null ? model.LawyerName : $"{lawyer.FirstName} {lawyer.LastName}";
                return View(model);
            }

            var upload = await RequestFileHelper.SaveAsync(model.CaseFile, _environment);
            if (!upload.Success) {
                ModelState.AddModelError(nameof(model.CaseFile), upload.Error ?? "The file could not be uploaded.");
                model.LawyerName = $"{lawyer!.FirstName} {lawyer.LastName}";
                return View(model);
            }

            _db.LegalServiceRequests.Add(new LegalServiceRequest {
                ClientId = client.Id,
                LawyerId = lawyer!.Id,
                Subject = model.Subject.Trim(),
                Brief = model.Brief.Trim(),
                RequestType = "Direct",
                Status = "Pending",
                FilePath = upload.Path,
                OriginalFileName = upload.OriginalName,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your request was sent to the lawyer.";
            return RedirectToAction(nameof(MyRequests));
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public IActionResult CreatePublic() => View(new CreateServiceRequestViewModel());

        [Authorize(Roles = Roles.Client), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePublic(CreateServiceRequestViewModel model) {
            ModelState.Remove(nameof(model.LawyerId));
            ModelState.Remove(nameof(model.LawyerName));
            ModelState.Remove(nameof(model.LawyerProfileId));

            var client = await _users.GetUserAsync(User);
            if (client == null) return Challenge();
            if (!ModelState.IsValid) return View(model);

            var upload = await RequestFileHelper.SaveAsync(model.CaseFile, _environment);
            if (!upload.Success) {
                ModelState.AddModelError(nameof(model.CaseFile), upload.Error ?? "The file could not be uploaded.");
                return View(model);
            }

            _db.LegalServiceRequests.Add(new LegalServiceRequest {
                ClientId = client.Id,
                Subject = model.Subject.Trim(),
                Brief = model.Brief.Trim(),
                RequestType = "Public",
                Status = "Pending",
                FilePath = upload.Path,
                OriginalFileName = upload.OriginalName,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your public request is now visible to lawyers.";
            return RedirectToAction(nameof(MyRequests));
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> MyRequests(string? type) {
            var clientId = _users.GetUserId(User);
            if (clientId == null) return Challenge();

            var query = _db.LegalServiceRequests
                .AsNoTracking()
                .Where(x => x.ClientId == clientId);

            if (type == "Direct" || type == "Public")
                query = query.Where(x => x.RequestType == type);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ServiceRequestListItemViewModel {
                    RequestId = x.LegalServiceRequestId,
                    Subject = x.Subject,
                    Brief = x.Brief,
                    RequestType = x.RequestType,
                    Status = x.Status,
                    LawyerName = x.Lawyer == null ? null : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                    ClientName = x.Client.FirstName + " " + x.Client.LastName,
                    FilePath = x.FilePath,
                    OriginalFileName = x.OriginalFileName,
                    CreatedAt = x.CreatedAt,
                    ProposalCount = x.Proposals.Count
                })
                .ToListAsync();

            ViewBag.TypeFilter = type;
            return View(items);
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> Details(int id) {
            var clientId = _users.GetUserId(User);
            if (clientId == null) return Challenge();

            var request = await _db.LegalServiceRequests
                .AsNoTracking()
                .Where(x => x.LegalServiceRequestId == id && x.ClientId == clientId)
                .Select(x => new ServiceRequestListItemViewModel {
                    RequestId = x.LegalServiceRequestId,
                    Subject = x.Subject,
                    Brief = x.Brief,
                    RequestType = x.RequestType,
                    Status = x.Status,
                    LawyerName = x.Lawyer == null ? null : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                    ClientName = x.Client.FirstName + " " + x.Client.LastName,
                    FilePath = x.FilePath,
                    OriginalFileName = x.OriginalFileName,
                    CreatedAt = x.CreatedAt,
                    ProposalCount = x.Proposals.Count
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound();

            var proposals = await _db.PublicRequestProposals
                .AsNoTracking()
                .Where(x => x.LegalServiceRequestId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ClientProposalListItemViewModel {
                    ProposalId = x.PublicRequestProposalId,
                    LawyerName = x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                    Message = x.Message,
                    ProposedPrice = x.ProposedPrice,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return View(new ClientRequestDetailsViewModel { Request = request, Proposals = proposals });
        }

        [Authorize(Roles = Roles.Client), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptProposal(int proposalId) {
            var clientId = _users.GetUserId(User);
            if (clientId == null) return Challenge();

            var proposal = await _db.PublicRequestProposals
                .Include(x => x.Request)
                .FirstOrDefaultAsync(x =>
                    x.PublicRequestProposalId == proposalId &&
                    x.Request.ClientId == clientId &&
                    x.Request.RequestType == "Public");

            if (proposal == null) return NotFound();

            if (proposal.Request.Status != "Pending" || proposal.Status != "Pending") {
                TempData["Error"] = "This proposal is no longer available.";
                return RedirectToAction(nameof(Details), new { id = proposal.LegalServiceRequestId });
            }

            proposal.Status = "Accepted";
            proposal.Request.Status = "Accepted";
            proposal.Request.LawyerId = proposal.LawyerId;
            proposal.Request.RespondedAt = DateTime.UtcNow;

            var otherProposals = await _db.PublicRequestProposals
                .Where(x =>
                    x.LegalServiceRequestId == proposal.LegalServiceRequestId &&
                    x.PublicRequestProposalId != proposal.PublicRequestProposalId &&
                    x.Status == "Pending")
                .ToListAsync();

            foreach (var other in otherProposals)
                other.Status = "Rejected";

            await _db.SaveChangesAsync();
            TempData["Success"] = "The lawyer was selected. You can now find them in Messages.";
            return RedirectToAction(nameof(Details), new { id = proposal.LegalServiceRequestId });
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> Incoming() {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var items = await _db.LegalServiceRequests
                .AsNoTracking()
                .Where(x => x.RequestType == "Direct" && x.LawyerId == lawyerId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ServiceRequestListItemViewModel {
                    RequestId = x.LegalServiceRequestId,
                    Subject = x.Subject,
                    Brief = x.Brief,
                    RequestType = x.RequestType,
                    Status = x.Status,
                    ClientName = x.Client.FirstName + " " + x.Client.LastName,
                    FilePath = x.FilePath,
                    OriginalFileName = x.OriginalFileName,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return View(items);
        }

        [Authorize(Roles = Roles.Lawyer), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondDirect(int id, string decision) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();
            if (decision != "Accepted" && decision != "Rejected") return BadRequest();

            var request = await _db.LegalServiceRequests
                .FirstOrDefaultAsync(x => x.LegalServiceRequestId == id && x.LawyerId == lawyerId && x.RequestType == "Direct");

            if (request == null) return NotFound();
            if (request.Status != "Pending") {
                TempData["Error"] = "This request has already been answered.";
                return RedirectToAction(nameof(Incoming));
            }

            request.Status = decision;
            request.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"The request was {decision.ToLowerInvariant()}.";
            return RedirectToAction(nameof(Incoming));
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> PublicRequests() {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var items = await _db.LegalServiceRequests
                .AsNoTracking()
                .Where(x => x.RequestType == "Public" && x.Status == "Pending")
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ServiceRequestListItemViewModel {
                    RequestId = x.LegalServiceRequestId,
                    Subject = x.Subject,
                    Brief = x.Brief,
                    RequestType = x.RequestType,
                    Status = x.Status,
                    ClientName = x.Client.FirstName + " " + x.Client.LastName,
                    FilePath = x.FilePath,
                    OriginalFileName = x.OriginalFileName,
                    CreatedAt = x.CreatedAt,
                    ProposalCount = x.Proposals.Count,
                    HasProposed = x.Proposals.Any(p => p.LawyerId == lawyerId)
                })
                .ToListAsync();

            return View(items);
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> Propose(int id) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var request = await _db.LegalServiceRequests
                .AsNoTracking()
                .Where(x => x.LegalServiceRequestId == id && x.RequestType == "Public" && x.Status == "Pending")
                .Select(x => new PublicRequestProposalViewModel {
                    RequestId = x.LegalServiceRequestId,
                    Subject = x.Subject,
                    ClientName = x.Client.FirstName + " " + x.Client.LastName
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound();
            if (await _db.PublicRequestProposals.AnyAsync(x => x.LegalServiceRequestId == id && x.LawyerId == lawyerId)) {
                TempData["Error"] = "You already sent a proposal for this request.";
                return RedirectToAction(nameof(PublicRequests));
            }

            return View(request);
        }

        [Authorize(Roles = Roles.Lawyer), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Propose(PublicRequestProposalViewModel model) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var request = await _db.LegalServiceRequests
                .Include(x => x.Client)
                .FirstOrDefaultAsync(x => x.LegalServiceRequestId == model.RequestId && x.RequestType == "Public" && x.Status == "Pending");

            if (request == null) return NotFound();
            model.Subject = request.Subject;
            model.ClientName = $"{request.Client.FirstName} {request.Client.LastName}";

            if (await _db.PublicRequestProposals.AnyAsync(x => x.LegalServiceRequestId == model.RequestId && x.LawyerId == lawyerId))
                ModelState.AddModelError(string.Empty, "You already sent a proposal for this request.");

            if (!ModelState.IsValid) return View(model);

            _db.PublicRequestProposals.Add(new PublicRequestProposal {
                LegalServiceRequestId = request.LegalServiceRequestId,
                LawyerId = lawyerId,
                Message = model.Message.Trim(),
                ProposedPrice = model.ProposedPrice,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your proposal was sent to the client.";
            return RedirectToAction(nameof(PublicRequests));
        }
    }
}
