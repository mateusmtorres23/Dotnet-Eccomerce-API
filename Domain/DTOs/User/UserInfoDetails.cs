using Domain.Models;
using Domain.DTOs.Store;

namespace Domain.DTOs.User;

public record UserInfoDetails(Guid id, string Email, UserRole Role, List<StoreInfo> Stores);