namespace Codexo.Infrastructure;

using Codexo.Core.AI;
using Codexo.Core.Auth;
using Codexo.Core.Interfaces;
using Codexo.Core.Prompts;
using Codexo.Infrastructure.AI;
using Codexo.Infrastructure.Auth;
using Codexo.Infrastructure.Data;
using Codexo.Infrastructure.Identity;
using Codexo.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=CodexoDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        // Register ASP.NET Core Identity with AppDbContext store
        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>();

        // Register TokenService
        services.AddScoped<ITokenService, TokenService>();

        // Register SessionService & HintService
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IHintService, HintService>();

        // Register HybridCache
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        // Register AI Configuration & Prompt Builder
        services.Configure<AISettings>(configuration.GetSection(AISettings.SectionName));
        services.AddSingleton<HintPromptBuilder>();
        services.AddHttpClient<OpenAiCompatibleAiClient>();
        services.AddTransient<FakeAiClient>();

        services.AddScoped<IAiClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AISettings>>().Value;
            if (string.Equals(options.Provider, "OpenAiCompatible", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<OpenAiCompatibleAiClient>();
            }

            return sp.GetRequiredService<FakeAiClient>();
        });

        return services;
    }
}
