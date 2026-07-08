using JDSP.Data;
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
    options.RequestCultureProviders = new IRequestCultureProvider[] { new CookieRequestCultureProvider() };
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true; options.Password.RequiredLength = 6; options.Password.RequireUppercase = true; options.Password.RequireLowercase = true; options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true; options.SignIn.RequireConfirmedEmail = false;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options => { options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/AccessDenied"; });
var app = builder.Build();
await SeedData.InitializeAsync(app.Services);
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Account/Error"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseRequestLocalization(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
