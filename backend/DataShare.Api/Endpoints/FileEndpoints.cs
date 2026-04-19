using System.Security.Claims;
using DataShare.Application.DTOs;
using DataShare.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataShare.Api.Endpoints;

public static class FileEndpoints
{
    public static WebApplication MapFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/files").WithTags("Files");

        group.MapPost("/", Upload).AllowAnonymous().DisableAntiforgery()
             .RequireRateLimiting("upload");
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Upload(
        IFormFile file,
        [FromForm] int? expiresInDays,
        [FromForm] string? password,
        [FromForm] string? tags,
        HttpContext http,
        FileUploadService uploadService,
        ILogger<FileUploadService> logger,
        CancellationToken ct)
    {
        Guid? ownerId = null;
        var authHeader = http.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? http.User.FindFirstValue("sub");

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var uid))
            {
                return Results.Json(
                    new ErrorResponse("INVALID_TOKEN", "Token JWT invalide ou expiré"),
                    statusCode: 401);
            }
            ownerId = uid;
        }

        if (string.IsNullOrWhiteSpace(password)) password = null;
        if (string.IsNullOrWhiteSpace(tags)) tags = null;

        var tagArray = ownerId.HasValue && tags != null
            ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null;

        try
        {
            var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
            using var stream = file.OpenReadStream();

            var (result, error, detail) = await uploadService.UploadAsync(
                stream, file.FileName, file.ContentType, file.Length,
                ownerId, expiresInDays, password, tagArray, baseUrl, ct);

            if (error is not null)
            {
                return error switch
                {
                    UploadError.BlockedExtension => Results.Json(
                        new ErrorResponse("BLOCKED_EXTENSION", $"Type de fichier interdit : {detail}"),
                        statusCode: 415),
                    UploadError.FileTooLarge => Results.Json(
                        new ErrorResponse("FILE_TOO_LARGE", "Le fichier dépasse la taille maximale de 1 Go"),
                        statusCode: 413),
                    UploadError.InvalidExpiration => Results.BadRequest(
                        new ErrorResponse("INVALID_EXPIRATION", "La durée d'expiration doit être entre 1 et 7 jours")),
                    UploadError.PasswordTooShort => Results.BadRequest(
                        new ErrorResponse("PASSWORD_TOO_SHORT", "Le mot de passe doit contenir au moins 6 caractères")),
                    _ => Results.Json(
                        new ErrorResponse("INTERNAL_ERROR", "Erreur interne"), statusCode: 500)
                };
            }

            return Results.Created($"/api/download/{result!.DownloadToken}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inattendue lors de l'upload");
            return Results.Json(
                new ErrorResponse("INTERNAL_ERROR", "Erreur interne lors du téléversement"),
                statusCode: 500);
        }
    }

    private static async Task<IResult> Delete(
        Guid id,
        ClaimsPrincipal claims,
        FileDeleteService deleteService,
        CancellationToken ct)
    {
        var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? claims.FindFirstValue("sub");

        if (userId is null || !Guid.TryParse(userId, out var uid))
            return Results.Json(new ErrorResponse("UNAUTHORIZED", "Token invalide"), statusCode: 401);

        var deleted = await deleteService.DeleteAsync(uid, id, ct);

        return deleted
            ? Results.NoContent()
            : Results.NotFound(new ErrorResponse("FILE_NOT_FOUND", "Fichier introuvable"));
    }
}
