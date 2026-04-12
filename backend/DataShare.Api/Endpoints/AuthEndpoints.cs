using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DataShare.Application.DTOs;
using DataShare.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace DataShare.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register).AllowAnonymous();
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<IdentityUser<Guid>> userManager,
        ITokenService tokenService)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
        {
            var errors = validationResults.ToDictionary(
                v => v.MemberNames.FirstOrDefault() ?? "general",
                v => v.ErrorMessage ?? "Valeur invalide");
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "Données invalides", errors));
        }

        var user = new IdentityUser<Guid>
        {
            Email = request.Email,
            UserName = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
            {
                return Results.Conflict(new ErrorResponse("EMAIL_ALREADY_USED", "Cet email est déjà utilisé"));
            }

            var errors = result.Errors.ToDictionary(e => e.Code, e => e.Description);
            return Results.BadRequest(new ErrorResponse("REGISTRATION_FAILED", "Échec de l'inscription", errors));
        }

        var (token, expiresAt) = tokenService.GenerateToken(user.Id, user.Email!);
        var userDto = new UserDto(user.Id, user.Email!, user.SecurityStamp != null ? DateTime.UtcNow : DateTime.UtcNow);

        return Results.Created("/api/auth/me", new AuthResponse(userDto, token, expiresAt));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<IdentityUser<Guid>> userManager,
        ITokenService tokenService)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
        {
            var errors = validationResults.ToDictionary(
                v => v.MemberNames.FirstOrDefault() ?? "general",
                v => v.ErrorMessage ?? "Valeur invalide");
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "Données invalides", errors));
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Results.Json(
                new ErrorResponse("INVALID_CREDENTIALS", "Email ou mot de passe incorrect"),
                statusCode: 401);
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Results.Json(
                new ErrorResponse("INVALID_CREDENTIALS", "Email ou mot de passe incorrect"),
                statusCode: 401);
        }

        var (token, expiresAt) = tokenService.GenerateToken(user.Id, user.Email!);
        var userDto = new UserDto(user.Id, user.Email!, DateTime.UtcNow);

        return Results.Ok(new AuthResponse(userDto, token, expiresAt));
    }

    private static async Task<IResult> Me(
        ClaimsPrincipal claims,
        UserManager<IdentityUser<Guid>> userManager)
    {
        var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? claims.FindFirstValue("sub");

        if (userId is null || !Guid.TryParse(userId, out var id))
        {
            return Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token invalide"),
                statusCode: 401);
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Results.NotFound(new ErrorResponse("USER_NOT_FOUND", "Utilisateur introuvable"));
        }

        return Results.Ok(new UserDto(user.Id, user.Email!, DateTime.UtcNow));
    }
}
