namespace Domain.DTOs.Store;

public record UpdateStoreRequest(Guid StoreId, string Name);