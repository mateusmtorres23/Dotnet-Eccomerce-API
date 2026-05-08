namespace Domain;

public class Product
{
    public Guid Id { get; set; }
    public String Name { get; set; }
    public String Description { get; set; }
    public double Price { get; set; }
    public String StoreId { get; set; }
}