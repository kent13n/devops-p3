namespace DataShare.Application.DTOs;

public record AuthResponse(UserDto User, string Token, DateTime ExpiresAt);
