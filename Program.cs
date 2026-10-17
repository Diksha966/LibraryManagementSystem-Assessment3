using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.Services.Branches;
using LibraryManagementSystem.Services.Exports;
using LibraryManagementSystem.Services.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddHostedService<OverdueNotificationBackgroundService>();

// Person 1: multi-branch inventory and inter-branch transfers
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ITransferService, TransferService>();

// Person 3: kiosk, reservations, CSV import and manager reports.
// Session holds the signed-in kiosk patron and the pending CSV upload.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<PatronService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<CsvItemImportService>();
builder.Services.AddScoped<ReportService>();

// Register the Entity Framework Core database context.
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
 builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryDbContext>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = { "Admin", "Reception", "Manager" };

    // Create the roles if they do not already exist.
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Create staff accounts and assign their roles.
    var staffAccounts = new[]
    {
        new
        {
            Email = "admin@library.com",
            Password = "Admin123!",
            Role = "Admin"
        },
        new
        {
            Email = "reception@library.com",
            Password = "Reception123!",
            Role = "Reception"
        },
        new
        {
            Email = "manager@library.com",
            Password = "Manager123!",
            Role = "Manager"
        }
    };

    foreach (var staff in staffAccounts)
    {
        var user = await userManager.FindByEmailAsync(staff.Email);

        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = staff.Email,
                Email = staff.Email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                user,
                staff.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    createResult.Errors.Select(e => e.Description));

                throw new Exception(
                    $"Could not create {staff.Email}: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, staff.Role))
        {
            await userManager.AddToRoleAsync(user, staff.Role);
        }
    }
}

app.Run();
