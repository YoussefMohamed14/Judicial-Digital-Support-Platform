using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews().AddViewLocalization();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options => {
    var cultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
    options.DefaultRequestCulture = new RequestCulture("en"); options.SupportedCultures = cultures; options.SupportedUICultures = cultures;
    options.RequestCultureProviders = new IRequestCultureProvider[] {
        new CookieRequestCultureProvider(),
        new CustomRequestCultureProvider(async context => {
            if (context.User.Identity?.IsAuthenticated != true) return null;
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.GetUserAsync(context.User);
            var language = user?.PreferredLanguage == "ar" ? "ar" : "en";
            return new ProviderCultureResult(language, language);
        })
    };
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true; options.Password.RequiredLength = 6; options.Password.RequireUppercase = true; options.Password.RequireLowercase = true; options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true; options.SignIn.RequireConfirmedEmail = false;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options => { options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/AccessDenied"; });
var app = builder.Build();
await SeedData.InitializeAsync(app.Services);
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error/500"); app.UseHsts(); }
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseRequestLocalization(); app.UseAuthorization();

app.Use(async (context, next) => {
    if (context.User.Identity?.IsAuthenticated == true) {
        var path = context.Request.Path;
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var user = await userManager.GetUserAsync(context.User);

        var isAllowedPasswordPath =
            path.StartsWithSegments("/Account/ChangeTemporaryPassword") ||
            path.StartsWithSegments("/Account/Logout") ||
            path.StartsWithSegments("/Account/ChangeAccount") ||
            path.StartsWithSegments("/Account/AccessDenied") ||
            path.StartsWithSegments("/Account/Error") ||
            path.StartsWithSegments("/Error");

        if (user?.MustChangePassword == true && !isAllowedPasswordPath) {
            context.Response.Redirect("/Account/ChangeTemporaryPassword");
            return;
        }

        if (context.User.IsInRole(Roles.Lawyer)) {
            var isAllowedApprovalPath =
                path.StartsWithSegments("/Account/LawyerPendingApproval") ||
                path.StartsWithSegments("/Account/Logout") ||
                path.StartsWithSegments("/Account/ChangeAccount") ||
                path.StartsWithSegments("/Account/AccessDenied") ||
                path.StartsWithSegments("/Account/Error") ||
            path.StartsWithSegments("/Error");

            if (user != null && user.LawyerApprovalStatus != VerificationStatus.Approved) {
                if (!isAllowedApprovalPath) {
                    context.Response.Redirect("/Account/LawyerPendingApproval");
                    return;
                }

                await next();
                return;
            }

            var isAllowedProfilePath =
                path.StartsWithSegments("/Profile") ||
                path.StartsWithSegments("/Settings") ||
                path.StartsWithSegments("/Account/CompleteProfile") ||
                path.StartsWithSegments("/Account/Logout") ||
                path.StartsWithSegments("/Account/ChangeAccount") ||
                path.StartsWithSegments("/Account/AccessDenied") ||
                path.StartsWithSegments("/Account/Error") ||
            path.StartsWithSegments("/Error");

            if (!isAllowedProfilePath && user?.IsProfileCompleted == true) {
                var profile = await db.LawyerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (!LawyerProfileRules.IsProfessionalProfileComplete(profile)) {
                    context.Response.Redirect("/Profile?professionalRequired=true");
                    return;
                }
            }
        }
    }

    await next();
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
