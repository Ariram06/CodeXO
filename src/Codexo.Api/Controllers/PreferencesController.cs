namespace Codexo.Api.Controllers;

using System.Security.Claims;
using Codexo.Core.Dtos.Preferences;
using Codexo.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/preferences")]
public class PreferencesController : ControllerBase
{
    private readonly IPreferenceService _preferenceService;
    private readonly IValidator<UpdateUserPreferenceRequestDto> _validator;

    public PreferencesController(
        IPreferenceService preferenceService,
        IValidator<UpdateUserPreferenceRequestDto> validator)
    {
        _preferenceService = preferenceService;
        _validator = validator;
    }

    /// <summary>
    /// Retrieves user study preferences (preferred language, auto-unlock level 1).
    /// If no preferences exist yet, default settings are generated.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        var pref = await _preferenceService.GetPreferencesAsync(userId, cancellationToken);
        return Ok(pref);
    }

    /// <summary>
    /// Upserts (creates or updates) the authenticated user's preferences.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateUserPreferenceRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be verified." });
        }

        var updated = await _preferenceService.UpsertPreferencesAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
