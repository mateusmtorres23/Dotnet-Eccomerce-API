namespace Domain;

public class User
{
    public required Guid Id { get; set; }
    public required String Email { get; set; }
    public required String Password { get; set; }
    public required UserRole Role { get; set; }
}