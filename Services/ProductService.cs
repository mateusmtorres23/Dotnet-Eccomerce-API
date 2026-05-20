using Domain.DTOs.Product;
using Domain.Models;
using Domain.Exceptions;
using Infra;
using Microsoft.EntityFrameworkCore;


namespace Services;

public class ProductService
{
    private readonly AppDbContext _dbContext;
    
    public ProductService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ProductInfo>> ReadStoreProduct(Guid storeId)
    {
        return await _dbContext.Products
            .Where(p => p.StoreId == storeId)
            .Select(p => new ProductInfo(p.Id, p.Name, p.Price))
            .ToListAsync();
    }

    public async Task<ProductInfoDetails> ReadProductDetails(Guid productId)
    {
        return await _dbContext.Products
            .Where(p => p.Id == productId)
            .Select(p => new ProductInfoDetails(p.Name, p.Price, p.Description))
            .SingleOrDefaultAsync() ?? throw new ArgumentException("Product not found.");
    }

    public async Task<CreateProductResponse> CreateProduct(CreateProductRequest request, Guid userId)
    {
        User user =  await _dbContext.Users.FindAsync(userId)
            ?? throw new ArgumentException("User not found.");

        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        
        Store store = await _dbContext.Stores.FindAsync(request.StoreId)
            ?? throw new ArgumentException("Store not found.");

        if (user.Role == UserRole.Seller && userId != store.OwnerId)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }

        Product newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StoreId = request.StoreId
        };
        
        _dbContext.Products.Add(newProduct);
        await _dbContext.SaveChangesAsync();

        return new CreateProductResponse(newProduct.Name, newProduct.Price, newProduct.Description);
    }
    
    public async Task<UpdateProductResponse> UpdateProduct(UpdateProductRequest request, Guid userId, Guid productId)
    {
        User user =  await _dbContext.Users.FindAsync(userId)
                     ?? throw new ArgumentException("User not found.");

        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        Product product = await _dbContext.Products.FindAsync(productId)
                          ?? throw new ArgumentException("Product not found.");
        
        Store store = await _dbContext.Stores.FindAsync(product.StoreId)
                      ?? throw new ArgumentException("Store not found.");

        if (user.Role == UserRole.Seller && userId != store.OwnerId)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        
        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        
        return new UpdateProductResponse(product.Name, product.Price, product.Description);
    }

    public async Task DeleteProduct(Guid productId, Guid userId)
    {
        User user = await _dbContext.Users.FindAsync(userId)
            ?? throw new ArgumentException("User not found.");

        if (user.Role == UserRole.Customer)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }

        Product product = await _dbContext.Products.FindAsync(productId)
                          ?? throw new ArgumentException("Product not found.");

        Store store = await _dbContext.Stores.FindAsync(product.StoreId)
            ?? throw new ArgumentException("Store not found.");

        if (user.Role == UserRole.Seller && userId != store.OwnerId)
        {
            throw new UnauthorizedUserException("This user is not authorized to perform this action.");
        }
        
        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();
    }
}