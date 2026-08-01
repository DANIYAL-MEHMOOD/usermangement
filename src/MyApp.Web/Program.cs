using MyApp.Web.Filters;
using MyApp.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddRazorPages(options =>
{
    // Signed-in-only screens
    options.Conventions.AddFolderApplicationModelConvention("/Users", m => m.Filters.Add(new AuthGuardFilter()));
    options.Conventions.AddFolderApplicationModelConvention("/Roles", m => m.Filters.Add(new AuthGuardFilter()));
    options.Conventions.AddFolderApplicationModelConvention("/Permissions", m => m.Filters.Add(new AuthGuardFilter()));
    options.Conventions.AddFolderApplicationModelConvention("/Menus", m => m.Filters.Add(new AuthGuardFilter()));
    options.Conventions.AddPageApplicationModelConvention("/Dashboard", m => m.Filters.Add(new AuthGuardFilter()));
    options.Conventions.AddPageApplicationModelConvention("/ChangePassword", m => m.Filters.Add(new AuthGuardFilter()));
    options.Conventions.AddFolderApplicationModelConvention("/Settings", m => m.Filters.Add(new AuthGuardFilter()));

    // Admin-only screens (matches [Authorize(Roles = "Administrator")] on the API side)
    options.Conventions.AddFolderApplicationModelConvention("/Roles", m => m.Filters.Add(new AdminOnlyFilter()));
    options.Conventions.AddFolderApplicationModelConvention("/Permissions", m => m.Filters.Add(new AdminOnlyFilter()));
    options.Conventions.AddFolderApplicationModelConvention("/Menus", m => m.Filters.Add(new AdminOnlyFilter()));
});
builder.Services.AddHttpContextAccessor();

// CSRF protection: Razor Pages auto-validates antiforgery on every POST handler;
// pointing it at a header (rather than only a form field) is what lets the
// fetch()-based AJAX calls in wwwroot/js/site.js satisfy that check — see the
// X-CSRF-TOKEN meta tag emitted in _Layout.cshtml.
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout requirement
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// The Web project never touches SQL Server directly — every data operation
// goes through this typed HttpClient to MyApp.Api.
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
        ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorPages();

app.Run();
