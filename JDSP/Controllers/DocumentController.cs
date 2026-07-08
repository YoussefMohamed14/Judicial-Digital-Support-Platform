using JDSP.Data;
using JDSP.Models;
using JDSP.ViewModel.Document;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Document = JDSP.Models.Document;

namespace JDSP.Controllers
{
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocumentController(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }
        public IActionResult Index(int caseId)
        {
            var documents = _context.Documents
                .Where(d => d.CaseId == caseId)
                .OrderByDescending(d => d.UploadedAt)
                .ToList();

            return View(documents);
        }


        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(UploadDocumentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);

            }
            string currentUserId = GetCurrentUserId();

            string uniqueFileName = Guid.NewGuid().ToString() +
                        Path.GetExtension(model.File.FileName);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }



            var doucement = new Document 
            {
                FileName = model.File.FileName,
                FilePath = uniqueFileName,
                UploadedAt = DateTime.UtcNow,
                CaseId = model.CaseId,
                UploadedById = currentUserId
            };

            _context.Documents.Add(doucement);
            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Index), new { caseId = model.CaseId });
        }


        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(User)!;
        }


    }
}
