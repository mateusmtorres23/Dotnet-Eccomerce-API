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
            throw new UnauthorizedUserException("This user is not authorized to view this information.");
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
            throw new UnauthorizedUserException("This user is not authorized to view this information.");
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
            throw new UnauthorizedUserException("This user is not authorized to view this information.");
        }

        Store store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Id == storeId)
            ?? throw new ArgumentException("Store not found.");

        if (user.Role == UserRole.Seller && store.OwnerId != userId)
        {
            throw new UnauthorizedUserException("This user is not authorized to view this information.");
        }
        
        var ownerEmail =  await _dbContext.Users.Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync()
            ?? throw new ArgumentException("User not found.");

        List<ProductInfo> products = await _dbContext.Products
            .Where(p => p.StoreId == storeId)
            .Select(p => new ProductInfo(p.Id, p.Name, p.Price))
            .ToListAsync();
            
        return new StoreInfoDetails(store.Name, ownerEmail, products);
    }

    public async Task<CreateStoreResponse> CreateStore(CreateStoreRequest request, Guid userId)
    {
        User owner = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found");

        if (owner.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to view this information.");
        }

        if (await _dbContext.Stores.AnyAsync(s => s.Name == request.Name))
        {
            throw new ArgumentException("A store with this name already exists.");
        }

        Store newStore = new Store
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            OwnerId = userId
        };
        
        _dbContext.Stores.Add(newStore);
        await _dbContext.SaveChangesAsync();

        return new CreateStoreResponse(request.Name, owner.Email);
    }

    public async Task DeleteStore(Guid storeId, Guid userId)
    {
        User user  = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
                     ?? throw new ArgumentException("User not found.");

        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        
        Store store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Id == storeId)
            ?? throw new ArgumentException("Store  not found.");

        if (store.OwnerId != userId)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        
        if (user.Role == UserRole.Seller && store.OwnerId != userId)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        
        _dbContext.Stores.Remove(store);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<UpdateStoreResponse> UpdateStore(Guid userId, UpdateStoreRequest request)
    {
        User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found.");

        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }

        Store store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Id == request.StoreId)
                      ?? throw new ArgumentException("Store with this ID not found.");

        if (user.Role == UserRole.Seller && store.OwnerId != userId)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        
        if (await _dbContext.Stores.AnyAsync(s => s.Name == request.Name && s.OwnerId == userId && s.Id != store.Id))
        {
            throw new InvalidOperationException("User already owns a store with this name.");
        }
        
        store.Name = request.Name;
        await _dbContext.SaveChangesAsync();
        
        return new UpdateStoreResponse(store.Name);
    }
}