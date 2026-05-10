namespace Domain;

public class Store
{
    public required Guid Id { get; set; }
    public required String Name { get; set; }
    public required Guid OwnerId { get; set; }
}