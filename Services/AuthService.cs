using Infra;
using Domain.Models;
using Domain.DTOs.Auth;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class AuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    
    public AuthService(AppDbContext dbContext, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<RegisterResponse> RegisterUser(RegisterRequest request)
    {
        string passwordHash = _passwordHasher.Hash(request.Password);

        User newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = passwordHash,
            Role = UserRole.Customer
        };
        
        _dbContext.Users.Add(newUser);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") ==  true)
        {
            throw new DuplicateEmailException("Email already in use.");
        }
        
        return new RegisterResponse(newUser.Email);
    }

    public async Task<LoginResponse> LoginUser(LoginRequest request)
    {
        User user  = await _dbContext.Users.SingleOrDefaultAsync(x => x.Email == request.Email)
            ?? throw new Exception("Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.Password))
        {
            throw new Exception("Invalid email or password.");
        }

        string token = _tokenService.GenerateToken(user);
        
        return new LoginResponse(token);
    }
}