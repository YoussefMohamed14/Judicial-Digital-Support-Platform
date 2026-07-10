using JDSP.Helpers;
using JDSP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Data {
    public static class SeedData {
        public static async Task InitializeAsync(IServiceProvider serviceProvider) {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roles =
            {
                Roles.Admin,
                Roles.CourtEmployee,
                Roles.Lawyer,
                Roles.Client
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await CreateUserIfNotExists(
                userManager,
                email: "superadmin@jdsp.local",
                password: "SuperAdmin@123",
                firstName: "Super",
                lastName: "Admin",
                nationalNumber: "10000000000000",
                role: Roles.Admin
            );

            await CreateUserIfNotExists(
                userManager,
                email: "admin@jdsp.com",
                password: "Admin@123",
                firstName: "System",
                lastName: "Admin",
                nationalNumber: "10000000000001",
                role: Roles.Admin
            );

            await CreateUserIfNotExists(
                userManager,
                email: "court@jdsp.com",
                password: "Admin@123",
                firstName: "Court",
                lastName: "Employee",
                nationalNumber: "10000000000002",
                role: Roles.CourtEmployee
            );

            await CreateUserIfNotExists(
                userManager,
                email: "lawyer@jdsp.com",
                password: "Admin@123",
                firstName: "Test",
                lastName: "Lawyer",
                nationalNumber: "10000000000003",
                role: Roles.Lawyer
            );

            await CreateUserIfNotExists(
                userManager,
                email: "client@jdsp.com",
                password: "Admin@123",
                firstName: "Test",
                lastName: "Client",
                nationalNumber: "10000000000004",
                role: Roles.Client
            );


            var testLawyer = await userManager.FindByEmailAsync("lawyer@jdsp.com");
            if (testLawyer != null && !await db.LawyerProfiles.AnyAsync(x => x.UserId == testLawyer.Id)) {
                db.LawyerProfiles.Add(new LawyerProfile {
                    UserId = testLawyer.Id,
                    Bio = "Test lawyer profile for local development and request-flow testing.",
                    Specialization = "General Practice",
                    YearsOfExperience = 5,
                    ConsultationPrice = 500,
                    ConsultationPriceUnit = UiText.PriceUnitHour,
                    IsAvailable = true,
                    CreatedAt = DateTime.Now
                });
                await db.SaveChangesAsync();
            }

            // Backfill existing lawyer accounts that do not have a LawyerProfile row yet.
            // Without this, the Find Lawyers page only shows lawyers who manually created
            // a professional profile and hides valid lawyer accounts.
            var lawyerUsers = await userManager.GetUsersInRoleAsync(Roles.Lawyer);

            foreach (var lawyer in lawyerUsers) {
                var hasVerificationRequest = await db.LawyerVerificationRequests.AnyAsync(x => x.LawyerId == lawyer.Id);
                if (!hasVerificationRequest &&
                    (string.IsNullOrWhiteSpace(lawyer.LawyerApprovalStatus) ||
                     lawyer.LawyerApprovalStatus == VerificationStatus.NotRequired)) {
                    lawyer.LawyerApprovalStatus = VerificationStatus.Approved;
                    lawyer.LawyerApprovalRejectionReason = null;
                    lawyer.LawyerApprovalReviewedAt = DateTime.Now;
                    await userManager.UpdateAsync(lawyer);
                }
            }

            var activeLawyerUsers = lawyerUsers
                .Where(x => x.AccountStatus == "Active" && x.LawyerApprovalStatus == VerificationStatus.Approved)
                .ToList();
            var activeLawyerIds = activeLawyerUsers.Select(x => x.Id).ToList();
            var existingLawyerProfileIds = await db.LawyerProfiles
                .Where(x => activeLawyerIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .ToListAsync();
            var existingProfileSet = existingLawyerProfileIds.ToHashSet();

            foreach (var lawyer in activeLawyerUsers.Where(x => !existingProfileSet.Contains(x.Id))) {
                db.LawyerProfiles.Add(new LawyerProfile {
                    UserId = lawyer.Id,
                    Bio = LawyerProfileRules.DefaultIncompleteBio,
                    Specialization = LawyerProfileRules.DefaultSpecialization,
                    YearsOfExperience = 0,
                    ConsultationPrice = 0,
                    ConsultationPriceUnit = UiText.PriceUnitHour,
                    IsAvailable = true,
                    CreatedAt = DateTime.Now
                });
            }


            var profilesWithoutPriceUnit = await db.LawyerProfiles
                .Where(x => x.ConsultationPriceUnit == null || x.ConsultationPriceUnit == "")
                .ToListAsync();

            foreach (var profile in profilesWithoutPriceUnit) {
                profile.ConsultationPriceUnit = UiText.PriceUnitHour;
            }

            await db.SaveChangesAsync();

            await BackfillAcceptedServiceRequestCasesAsync(db);
        }

        private static async Task BackfillAcceptedServiceRequestCasesAsync(ApplicationDbContext db) {
            var acceptedRequests = await db.LegalServiceRequests
                .AsNoTracking()
                .Where(x => x.Status == "Accepted" && x.LawyerId != null)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            foreach (var request in acceptedRequests) {
                var alreadyAssigned = await db.CaseLawyers.AnyAsync(x =>
                    x.LawyerId == request.LawyerId &&
                    x.Case != null &&
                    x.Case.CreatedBy_Id == request.ClientId &&
                    x.Case.CaseName == request.Subject &&
                    x.Case.Description == request.Brief);

                if (alreadyAssigned)
                    continue;

                var caseType = request.RequestType == "Public" ? "Public Request" : "Direct Request";
                var assignedCase = new Case {
                    CaseName = request.Subject.Trim(),
                    CaseType = caseType,
                    Description = request.Brief.Trim(),
                    CreatedBy_Id = request.ClientId,
                    CreatedAt = request.RespondedAt?.ToLocalTime() ?? DateTime.Now,
                    Status = "Open"
                };

                if (!string.IsNullOrWhiteSpace(request.FilePath)) {
                    assignedCase.Documents.Add(new Document {
                        FileName = string.IsNullOrWhiteSpace(request.OriginalFileName) ? "case-file" : request.OriginalFileName,
                        FilePath = request.FilePath,
                        UploadedAt = request.CreatedAt,
                        UploadedById = request.ClientId
                    });
                }

                db.Cases.Add(assignedCase);
                db.CaseLawyers.Add(new CaseLawyer {
                    Case = assignedCase,
                    LawyerId = request.LawyerId!,
                    Status = "Accepted",
                    AssignedAt = request.RespondedAt?.ToLocalTime() ?? DateTime.Now
                });
            }

            await db.SaveChangesAsync();
        }

        private static async Task CreateUserIfNotExists(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string firstName,
            string lastName,
            string nationalNumber,
            string role) {
            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser != null)
                return;

            var user = new ApplicationUser {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                NationalNumber = nationalNumber,
                AccountStatus = "Active",
                LawyerApprovalStatus = role == Roles.Lawyer ? VerificationStatus.Approved : VerificationStatus.NotRequired,
                IsProfileCompleted = role == Roles.Admin || role == Roles.CourtEmployee,
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded) {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}