using Infra;
using Domain.Models;
using Domain.DTOs.Store;
using Domain.DTOs.Product;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class StoreService
{
    private readonly AppDbContext _dbContext;
    
    public StoreService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<List<StoreInfo>> ListAllStores(Guid userId)
    {
        User user =  await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found.");

        if (user.Role != UserRole.Admin)
        {
            throw new UnauthorizedUserException("This user is not authorized to view this information");
        }

        return await _dbContext.Stores
            .Select(s => new StoreInfo(s.Id, s.Name))
            .ToListAsync();
    }
    
    public async Task<List<StoreInfo>> ListOwnStores(Guid userId)
    {
        User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found.");

        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to view this information");
        }

        return await _dbContext.Stores
            .Where(s => s.OwnerId == userId)
            .Select(s => new StoreInfo(s.Id, s.Name))
            .ToListAsync();
    }

    public async Task<StoreInfoDetails> GetStoreInfoDetails(Guid storeId,  Guid userId)
    {
        User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found.");
        
        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to view this information");
        }

        Store store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Id == storeId)
            ?? throw new ArgumentException("Store with this ID not found.");

        if (store.OwnerId != userId)
        {
            throw new UnauthorizedUserException("This user is not authorized to view this information");
        }

        List<ProductInfo> products = await _dbContext.Products
            .Where(p => p.StoreId == storeId)
            .Select(p => new ProductInfo(p.Id, p.Name, p.Price))
            .ToListAsync();
            
        return new StoreInfoDetails(store.Name, user.Email, products);
    }

    public async Task<CreateStoreResponse> CreateStore(CreateStoreRequest request)
    {
        User owner = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.OwnerId)
            ?? throw new ArgumentException("This user  doesn't exist");

        if (owner.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("Only users with Seller role can create stores.");
        }

        if (_dbContext.Stores.Any(s => s.Name == request.Name))
        {
            throw new InvalidOperationException("A store with this name already exists.");
        }

        Store newStore = new Store
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            OwnerId = request.OwnerId
        };
        
        _dbContext.Stores.Add(newStore);
        await _dbContext.SaveChangesAsync();

        return new CreateStoreResponse(request.Name, owner.Email);
    }

    public async Task DeleteStore(Guid storeId)
    {
        Store store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Id == storeId)
            ?? throw new ArgumentException("Store with this ID not found.");
        
        _dbContext.Stores.Remove(store);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<UpdateStoreResponse> UpdateStore(Guid userId, UpdateStoreRequest request)
    {
        User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found.");

        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action");
        }

        Store store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Id == request.StoreId)
                      ?? throw new ArgumentException("Store with this ID not found.");

        
        if (_dbContext.Stores.Any(s => s.Name == request.Name && s.OwnerId == userId))
        {
            throw new InvalidOperationException("User already own a store with this name.");
        }
        
        store.Name = request.Name;
        await _dbContext.SaveChangesAsync();
        
        return new UpdateStoreResponse(store.Name);
    }
}