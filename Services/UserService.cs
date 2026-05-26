using Infra;
using Domain.Models;
using Domain.DTOs.Store;
using Domain.DTOs.User;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class UserService
{
    private readonly AppDbContext _dbContext;
    
    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<UserInfo>> ListUsers(Guid userId)
    {
        User user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException($"User not found");

        if (user.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("This user is not authorized to view this information");
        }

        return await _dbContext.Users
            .Select(u => new UserInfo(u.Id, u.Email))
            .ToListAsync();
    }
    
    public async Task<UserInfoDetails> GetUserDetails(Guid userId, Guid viewUserId)
    {
        User user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId)
                    ?? throw new ArgumentException($"User not found");

        if (user.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("This user is not authorized to view this information");
        }
        
        var viewUser = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == viewUserId)
                       ?? throw new ArgumentException($"User not found");

        List<StoreInfo> userStores = await _dbContext.Stores
            .Where(s => s.OwnerId == viewUserId)
            .Select(s => new StoreInfo(s.Id, s.Name))
            .ToListAsync();

        return new UserInfoDetails(viewUser.Id, user.Email, user.Role, userStores);
    }

    public async Task<UpgradeRoleResponse> UpgradeRole (UpgradeRoleRequest request)
    {
        User user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == request.userId)
            ?? throw new ArgumentException($"User not found");
        
        user.Role = request.Role;
        
        await _dbContext.SaveChangesAsync();

        return new UpgradeRoleResponse(user.Email, user.Role);
    }
}