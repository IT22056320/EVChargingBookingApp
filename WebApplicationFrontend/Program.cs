/*
 * File: Program.cs
 * Description: Application startup configuration for EV Charging Web Frontend
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using WebApplicationFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add HTTP client for API communication
builder.Services.AddHttpClient<ApiService>();
builder.Services.AddScoped<ApiService>();

// Add session support for user authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true; // Security
    options.Cookie.IsEssential = true; // GDPR compliance
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add memory cache for performance
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable session before authorization
app.UseSession();
app.UseAuthorization();

// Default route should go to login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
