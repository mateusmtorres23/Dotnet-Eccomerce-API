using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using ecommerce_api;
using Domain.DTOs.Auth;
using Domain.DTOs.User;
using Infra;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtKey = builder.Configuration["JwtSettings:Key"]
    ?? throw new InvalidOperationException("Missing JWT key configuration");
var JwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var JwtAudience = builder.Configuration["JwtSettings:Audience"];

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = JwtIssuer,
            ValidAudience = JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StoreService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ProductService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "E-commerce API is running.");

var api = app.MapGroup("/api");

var authApi = api.MapGroup("/auth").AllowAnonymous();

authApi.MapPost("/register", async (AuthService authService, RegisterRequest request) =>
{
    var response = await authService.RegisterUser(request);
    return Results.Created("/api/auth/login", response);
});

authApi.MapPost("/login", async (AuthService authService, LoginRequest request) =>
{
    var response = await authService.LoginUser(request);
    return Results.Ok(response);
});

var secureApi = api.MapGroup("").RequireAuthorization();

var usersApi = secureApi.MapGroup("/users");

usersApi.MapGet("/", async (UserService userService, HttpContext context) =>
{
    var requesterId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var users = await userService.ListUsers(requesterId);
    return Results.Ok(users);
});

usersApi.MapGet("/{id:guid}", async (UserService userService, HttpContext context, Guid id) =>
{
    var requesterId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var userDetails = await userService.GetUserDetails(requesterId, id);
    return Results.Ok(userDetails);
});

usersApi.MapPost("/upgrade", async (UserService userService, UpgradeRoleRequest request) =>
{
    var response = await userService.UpgradeRole(request);
    return Results.Ok(response);
});

var storesApi = secureApi.MapGroup("/stores");

app.Run();