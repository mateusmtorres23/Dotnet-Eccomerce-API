namespace Domain;

public class Store
{
    public Guid Id { get; set; }
    public String Name { get; set; }
    public String OwnerId { get; set; }
    public List<Product> Products { get; set; }
}