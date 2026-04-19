using System.Security.Claims;
using DataShare.Application.DTOs;
using DataShare.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Api.Endpoints;

public static class TagEndpoints
{
    public static WebApplication MapTagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tags").WithTags("Tags").RequireAuthorization();

        group.MapGet("/", GetTags);
        group.MapDelete("/{id:guid}", DeleteTag);

        return app;
    }

    private static async Task<IResult> GetTags(
        ClaimsPrincipal claims,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? claims.FindFirstValue("sub");

        if (userId is null || !Guid.TryParse(userId, out var uid))
            return Results.Json(new ErrorResponse("UNAUTHORIZED", "Token invalide"), statusCode: 401);

        var tags = await db.Tags
            .AsNoTracking()
            .Where(t => t.OwnerId == uid)
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name))
            .ToListAsync(ct);

        return Results.Ok(tags);
    }

    private static async Task<IResult> DeleteTag(
        Guid id,
        ClaimsPrincipal claims,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? claims.FindFirstValue("sub");

        if (userId is null || !Guid.TryParse(userId, out var uid))
            return Results.Json(new ErrorResponse("UNAUTHORIZED", "Token invalide"), statusCode: 401);

        // Anti-IDOR : requête combinée ownership + existence
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == uid, ct);

        if (tag is null)
            return Results.NotFound(new ErrorResponse("TAG_NOT_FOUND", "Tag introuvable"));

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
