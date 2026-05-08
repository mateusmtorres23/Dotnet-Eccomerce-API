namespace Domain;

public class CartItem
{
    public Guid Id { get; set; }
    public String OwnerId { get; set; }
    public String ProductId { get; set; }
    public String StoreId { get; set; }
}