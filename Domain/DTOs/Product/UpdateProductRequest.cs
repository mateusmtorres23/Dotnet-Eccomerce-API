namespace Domain.DTOs.Product;

public record UpdateProductRequest(string Name, string Description, decimal Price);