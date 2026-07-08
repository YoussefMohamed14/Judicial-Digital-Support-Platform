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
                    IsAvailable = true,
                    CreatedAt = DateTime.Now
                });
                await db.SaveChangesAsync();
            }
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
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded) {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}