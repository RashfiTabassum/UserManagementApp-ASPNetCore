using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.IO;
//using UserManagementApp.Data; // replace with your namespace
//using UserManagementApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add MVC support
builder.Services.AddControllersWithViews();

// Configure SQL Server + DbContext
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")));
// Database: SQLite in production, SQL Server locally
// ============================
var sqlitePath = "/app/Data/UserManagementApp.db";

// Ensure directory exists in container
//var dataDir = "/app/Data";
//if (!Directory.Exists(dataDir))
//{
//    Directory.CreateDirectory(dataDir);
//}

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SQLiteConnection")));
}


// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<EmailService>();


var app = builder.Build();

// Automatically create/migrate the database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (builder.Environment.IsDevelopment())
    {
        // Use migrations for SQL Server locally
        db.Database.Migrate();
    }
    else
    {
        // SQLite in Render container: create DB if not exists
        db.Database.EnsureCreated();
    }
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // important
app.UseMiddleware<UserStatusMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
