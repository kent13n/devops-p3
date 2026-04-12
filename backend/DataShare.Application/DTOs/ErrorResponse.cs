namespace DataShare.Application.DTOs;

public record ErrorResponse(string Code, string Message, object? Details = null);
