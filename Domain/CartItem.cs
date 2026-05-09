namespace Domain;

public class CartItem
{
    public required Guid Id { get; set; }
    public required String OwnerId { get; set; }
    public required String ProductId { get; set; }
    public required String StoreId { get; set; }
}