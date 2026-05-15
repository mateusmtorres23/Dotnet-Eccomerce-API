namespace Domain.Models;

public class CartItem
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid ProductId { get; set; }
}