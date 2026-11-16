namespace Codexo.Api.Controllers;

using Codexo.Infrastructure.AI;
using Codexo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[AllowAnonymous]
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IOptions<AISettings> _aiSettings;

    public HealthController(AppDbContext dbContext, IOptions<AISettings> aiSettings)
    {
        _dbContext = dbContext;
        _aiSettings = aiSettings;
    }

    [HttpGet]
    public async Task<IActionResult> CheckHealth(CancellationToken cancellationToken)
    {
        var dbConnected = false;
        try
        {
            dbConnected = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            dbConnected = false;
        }

        var ai = _aiSettings.Value;
        var isAiConfigured = ai.Provider == "Fake" || !string.IsNullOrWhiteSpace(ai.ApiKey);

        var isHealthy = dbConnected && isAiConfigured;

        var result = new
        {
            status = isHealthy ? "Healthy" : "Unhealthy",
            timestamp = DateTimeOffset.UtcNow,
            database = dbConnected ? "Connected" : "Disconnected",
            ai = new
            {
                provider = ai.Provider,
                model = ai.Model,
                configured = isAiConfigured
            }
        };

        return isHealthy ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
