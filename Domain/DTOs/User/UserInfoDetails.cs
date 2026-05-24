using Domain.Models;
using Domain.DTOs.Store;

namespace Domain.DTOs.User;

public record UserInfoDetails(Guid Id, string Email, UserRole Role, List<StoreInfo> Stores);