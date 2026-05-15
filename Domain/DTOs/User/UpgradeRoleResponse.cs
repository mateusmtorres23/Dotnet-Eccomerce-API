using Domain.Models;

namespace Domain.DTOs.User;

public record UpgradeRoleResponse(string Email, UserRole Role);