namespace Domain.DTOs.Store;

public record CreateStoreRequest(Guid OwnerId, string Name);