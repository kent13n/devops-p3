namespace DataShare.Application.Interfaces;

public interface IFilePasswordHasher
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}
