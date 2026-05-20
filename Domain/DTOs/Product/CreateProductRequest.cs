namespace Domain.DTOs.Product;

public record CreateProductRequest(string Name, string Description, decimal Price, Guid StoreId);