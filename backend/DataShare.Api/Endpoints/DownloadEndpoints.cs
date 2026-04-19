using DataShare.Application.DTOs;
using DataShare.Application.Services;

namespace DataShare.Api.Endpoints;

public static class DownloadEndpoints
{
    public static WebApplication MapDownloadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/download").WithTags("Download").AllowAnonymous();

        group.MapGet("/{token}", GetMetadata);
        group.MapPost("/{token}", Download).RequireRateLimiting("download");

        return app;
    }

    private static async Task<IResult> GetMetadata(
        string token,
        FileDownloadService downloadService,
        CancellationToken ct)
    {
        var (metadata, error) = await downloadService.GetMetadataAsync(token, ct);

        return error switch
        {
            DownloadError.NotFound => Results.NotFound(
                new ErrorResponse("TOKEN_NOT_FOUND", "Lien de téléchargement inconnu")),
            DownloadError.Expired => Results.Json(
                new ErrorResponse("FILE_EXPIRED", "Ce lien de téléchargement a expiré"), statusCode: 410),
            _ => Results.Ok(metadata)
        };
    }

    private static async Task<IResult> Download(
        string token,
        DownloadRequest? request,
        FileDownloadService downloadService,
        CancellationToken ct)
    {
        var result = await downloadService.DownloadAsync(token, request?.Password, ct);

        return result switch
        {
            DownloadResult.Failure { Error: DownloadError.NotFound } => Results.NotFound(
                new ErrorResponse("TOKEN_NOT_FOUND", "Lien de téléchargement inconnu")),
            DownloadResult.Failure { Error: DownloadError.Expired } => Results.Json(
                new ErrorResponse("FILE_EXPIRED", "Ce lien de téléchargement a expiré"), statusCode: 410),
            DownloadResult.Failure => Results.Json(
                new ErrorResponse("INVALID_PASSWORD", "Mot de passe absent ou incorrect"), statusCode: 401),
            DownloadResult.Success s => Results.Stream(s.Stream, contentType: s.ContentType, fileDownloadName: s.FileName),
            _ => Results.Json(new ErrorResponse("INTERNAL_ERROR", "Erreur interne"), statusCode: 500)
        };
    }
}
