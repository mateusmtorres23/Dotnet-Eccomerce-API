namespace Domain.Models;

public class Product
{
    public required Guid Id { get; set; }
    public required String Name { get; set; }
    public required String Description { get; set; }
    public required decimal Price { get; set; }
    public required Guid StoreId { get; set; }
}