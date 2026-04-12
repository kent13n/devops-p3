namespace DataShare.Application.Interfaces;

public interface ITokenService
{
    (string token, DateTime expiresAt) GenerateToken(Guid userId, string email);
}
