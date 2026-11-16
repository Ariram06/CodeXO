using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Codexo.Api.Middleware;
using Codexo.Api.Validators;
using Codexo.Infrastructure;
using Codexo.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CodexoApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CodexoExtension";

// Startup check: Report key presence safely without exposing secret values (CODEXO BUILD RULES)
var isKeyPresent = !string.IsNullOrWhiteSpace(jwtKey) && jwtKey.Length >= 32;
Console.WriteLine($"[Startup Check] JWT signing key configured: {(isKeyPresent ? "yes" : "no")}");

// Startup check: Report AI provider and key presence safely without exposing secret values
var aiProvider = builder.Configuration["AI:Provider"] ?? "Fake";
var aiKey = builder.Configuration["AI:ApiKey"];
var isAiKeyConfigured = !string.IsNullOrWhiteSpace(aiKey);
Console.WriteLine($"[Startup Check] AI provider: {aiProvider}, API key configured: {(isAiKeyConfigured ? "yes" : "no")}");

// Use configured key, or fallback dummy key for schema initialization if key is not yet set
var signingKeyBytes = Encoding.UTF8.GetBytes(isKeyPresent ? jwtKey! : new string('0', 32));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure Configurable Per-User Rate Limiting
var permitLimitPerMinute = builder.Configuration.GetValue<int>("RateLimiting:PermitLimitPerMinute", 6);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please wait a minute before requesting another hint.",
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Instance = context.HttpContext.Request.Path
        };
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(problem), token);
    };

    options.AddPolicy("HintPolicy", httpContext =>
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            userId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Seed database on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.SeedAsync(dbContext, app.Environment.IsDevelopment());
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();

// Make Program accessible to WebApplicationFactory in IntegrationTests
public partial class Program { }
