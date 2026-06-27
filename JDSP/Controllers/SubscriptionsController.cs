using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JDSP.Data;
using JDSP.ViewModel;

namespace JDSP.Controllers
{
    [Authorize]
    public class SubscriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Confirmation(int subscriptionId)
        {
            var sub = _context.CaseLawyerSubscriptions
                .Where(s => s.CaseLawyerSubscriptionId == subscriptionId)
                .Select(s => new SubscriptionConfirmationViewModel
                {
                    SubscriptionId = s.CaseLawyerSubscriptionId,
                    CaseName = s.Caselawyer!.Case!.CaseName,
                    
                    LawyerName = s.Caselawyer.Lawyer!.FirstName 
                    + " " + s.Caselawyer.Lawyer!.MiddleName 
                    + " " + s.Caselawyer.Lawyer!.LastName,
                    
                    Price = s.Price,
                    BillingCycle = s.BillingCycle,
                    Status = s.Status,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate
                }).FirstOrDefault();

            if (sub == null)
                return NotFound();

            return View(sub);
        }
    }
}

