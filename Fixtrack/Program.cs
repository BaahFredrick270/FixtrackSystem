using Fixtrack.Services;
using Fixtrack.Data;
using Fixtrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- DATABASE ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------- APP SERVICES ----------
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IAppEmailSender, EmailSender>();

// Shows a helpful "you have pending migrations" page instead of a raw error.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------- IDENTITY (login + roles) ----------
// AddDefaultIdentity already includes the token providers used for password reset.
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
     .AddRoles<IdentityRole>()
     .AddEntityFrameworkStores<ApplicationDbContext>();

// Send signed-out visitors to OUR login page, and signed-in users who lack the
// right role to OUR "Access denied" page (instead of the built-in Identity ones).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ---------- MVC ----------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();      // the Identity login screens are Razor Pages

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider);
        logger.LogInformation("Seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Seeding failed - the app will start anyway. " +
            "Roles and starter accounts may be missing.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ORDER MATTERS: authentication (who are you?) must run before
// authorization (are you allowed?).
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();                   // maps /Identity/Account/Login etc.

app.Run();