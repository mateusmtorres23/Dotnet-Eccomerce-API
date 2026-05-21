using Domain.DTOs.Cart;
using Domain.Models;
using Infra;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class CartService
{
    private readonly AppDbContext _dbContext;
    
    public CartService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AddProductCartResponse> AddProductCart(AddProductCartRequest request, Guid userId)
    {
        Product product = await _dbContext.Products.FindAsync(request.ProductId)
            ?? throw new ArgumentException("Product not found");
        
        CartItem newCartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            UserId = userId
        };
        
        _dbContext.CartItems.Add(newCartItem);
        await _dbContext.SaveChangesAsync();

        return new AddProductCartResponse(product.Name, product.Price);
    }
    
    public async Task RemoveProductCart(RemoveProductCartRequest request,  Guid userId)
    {
        CartItem cartItem = await _dbContext.CartItems.FirstOrDefaultAsync(ci => 
                                ci.ProductId == request.ProductId && ci.UserId == userId)
            ?? throw new ArgumentException("Cart item not found");
        
        _dbContext.CartItems.Remove(cartItem);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task CheckoutCart(Guid userId)
    {
            List<CartItem> cartItems = await _dbContext.CartItems.Where(ci => ci.UserId == userId).ToListAsync();
            
            if (!cartItems.Any())
            {
                throw new InvalidOperationException("Cart is empty");
            }
            
            _dbContext.CartItems.RemoveRange(cartItems);
            await _dbContext.SaveChangesAsync();
    }
}