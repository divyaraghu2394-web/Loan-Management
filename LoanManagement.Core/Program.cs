
using LoanManagement.DL.Implementation;
using LoanManagement.DL.Interfaces;
using LoanManagement.Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Load EMI settings from JSON
builder.Configuration.AddJsonFile(
    "EMISettings.json",
    optional: false,
    reloadOnChange: true);

builder.Services.AddControllersWithViews();
builder.Services.Configure<AzureDocumentIntelligenceOptions>(
    builder.Configuration.GetSection("AzureDocumentIntelligence"));
builder.Services.AddScoped<IDocumentValidationService, AzureDocumentValidationService>();

// Database connection
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection connection string is not configured.");

builder.Services.AddScoped<IDbConnectionFactory>(
    _ => new DbConnectionFactory(connectionString));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<IEMICollectionRepository, EMICollectionRepository>();

// Cookie Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "LoanManagement.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// EMI calculation service
builder.Services.AddScoped<EMICalculationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
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
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
