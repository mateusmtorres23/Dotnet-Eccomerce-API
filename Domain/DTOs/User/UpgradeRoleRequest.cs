using Domain.Models;

namespace Domain.DTOs.User;

public record UpgradeRoleRequest(Guid userId, UserRole Role);