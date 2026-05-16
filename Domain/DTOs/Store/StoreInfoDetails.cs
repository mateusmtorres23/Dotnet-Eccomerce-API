using Domain.DTOs.Product;

namespace Domain.DTOs.Store;

public record StoreInfoDetailed(string Name, string OwnerEmail, List<ProductInfo> Products);