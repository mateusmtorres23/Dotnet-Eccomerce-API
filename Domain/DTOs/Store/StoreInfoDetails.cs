using Domain.DTOs.Product;

namespace Domain.DTOs.Store;

public record StoreInfoDetails(string Name, string OwnerEmail, List<ProductInfo> Products);