namespace Domain;

public class User
{
    public Guid Id { get; set; }
    public String Email { get; set; }
    public String Password { get; set; }
    public UserRole Role { get; set; }
    public List<CartItem> Cart { get; set; }
}