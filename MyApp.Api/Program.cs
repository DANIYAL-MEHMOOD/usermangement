using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication;
using MyApp.Api.Configurations;
using MyApp.Api.Extensions;
using MyApp.Api.Middlewares;
using MyApp.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Services
// ---------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("SessionToken", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = SessionTokenDefaults.HeaderName,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "SessionToken",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter the opaque session token returned by POST /api/auth/login."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "SessionToken"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));
builder.Services.AddApplicationServices(builder.Configuration);

// Session-token authentication (opaque token in X-Api-Token header, stored
// hashed in SQL Server). No JWT anywhere in the solution.
builder.Services.AddAuthentication(SessionTokenDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, SessionTokenAuthenticationHandler>(
        SessionTokenDefaults.AuthenticationScheme, _ => { });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Brute-force / DoS protection
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// ---------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("WebFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
