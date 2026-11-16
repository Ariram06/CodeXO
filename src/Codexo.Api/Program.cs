using System.Text;
using Codexo.Api.Validators;
using Codexo.Infrastructure;
using Codexo.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("HintPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 30;
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

// Seed database on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.SeedAsync(dbContext, app.Environment.IsDevelopment());
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible to WebApplicationFactory in IntegrationTests
public partial class Program { }
