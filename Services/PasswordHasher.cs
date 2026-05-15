using BCrypt.Net;

namespace Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedStrign);
}

public class PasswordHasher : IPasswordHasher
{
    private const int workFactor = 12;

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
    }

    public bool Verify(string password, string hashedString)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedString);
    }
    
}