using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Data;
using InternetBanking.Models;
using InternetBanking.Filters;
using InternetBanking.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 3;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RoleChangeAuthorizationFilter>();
});

// Register NotificationService
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register StatementService
builder.Services.AddScoped<IStatementService, StatementService>();

// Add background service to check for ready cards
builder.Services.AddHostedService<CardReadyCheckService>();

var app = builder.Build();

// Seed roles and default admin
SeedRoles(app).Wait();
SeedDefaultAdmin(app).Wait();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

async Task SeedRoles(IHost app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    // Create Admin role
    var adminRoleExists = await roleManager.RoleExistsAsync("Admin");
    if (!adminRoleExists)
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    // Create User role
    var userRoleExists = await roleManager.RoleExistsAsync("User");
    if (!userRoleExists)
    {
        await roleManager.CreateAsync(new IdentityRole("User"));
    }
}

async Task SeedDefaultAdmin(IHost app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Check if default admin exists
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        Console.WriteLine("Creating default admin user...");
        
        // Create default admin user
        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@securebank.com",
            FirstName = "System",
            LastName = "Administrator",
            Address = "System Address",
            DateOfBirth = new DateTime(1990, 1, 1),
            PhoneNumber = "000-000-0000"
        };
        
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
        {
            Console.WriteLine("Admin user created successfully");
            await userManager.AddToRoleAsync(admin, "Admin");
            Console.WriteLine("Admin role assigned successfully");
        }
        else
        {
            Console.WriteLine("Failed to create admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
    }
    else
    {
        Console.WriteLine("Admin user already exists - resetting password");
        
        // Reset the admin password to ensure it's correct
        var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
        var resetResult = await userManager.ResetPasswordAsync(adminUser, token, "Admin@123");
        
        if (resetResult.Succeeded)
        {
            Console.WriteLine("Admin password reset successfully");
        }
        else
        {
            Console.WriteLine("Failed to reset admin password:");
            foreach (var error in resetResult.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
        
        // Ensure admin role is assigned
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("Admin role assigned successfully");
        }
    }
    
    // Create or update AdminCredentials record
    var adminCredentials = await context.AdminCredentials.FirstOrDefaultAsync();
    if (adminCredentials == null)
    {
        Console.WriteLine("Creating admin credentials record...");
        
        // Hash the default password
        var hasher = new PasswordHasher<ApplicationUser>();
        var passwordHash = hasher.HashPassword(null, "Admin@123");
        
        adminCredentials = new AdminCredentials
        {
            Username = "admin",
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDefault = true
        };
        
        context.AdminCredentials.Add(adminCredentials);
        await context.SaveChangesAsync();
        Console.WriteLine("Admin credentials record created successfully");
    }
    else
    {
        Console.WriteLine("Admin credentials record already exists");
    }
}
