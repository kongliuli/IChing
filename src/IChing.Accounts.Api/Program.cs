using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace IChing.Accounts.Api;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var jwtKey = builder.Configuration["Auth:JwtKey"] ?? "iching-lab-dev-key-change-in-production";
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        builder.Services.AddSingleton(new AccountStore());
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey
                };
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "iching-accounts" }));

        app.MapPost("/api/register", (RegisterRequest req, AccountStore store) =>
        {
            if (string.IsNullOrWhiteSpace(req.Phone) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new { error = "phone and password required" });
            }

            if (!store.TryRegister(req.Phone.Trim(), req.Password, req.Nickname, out var user, out var error))
            {
                return Results.Conflict(new { error });
            }

            return Results.Ok(new { userId = user.Id, phone = user.Phone, nickname = user.Nickname });
        });

        app.MapPost("/api/login", (LoginRequest req, AccountStore store, IConfiguration config) =>
        {
            if (!store.TryLogin(req.Phone.Trim(), req.Password, out var user))
            {
                return Results.Unauthorized();
            }

            var token = IssueToken(user, signingKey, config);
            return Results.Ok(new
            {
                token,
                user = new { userId = user.Id, phone = user.Phone, nickname = user.Nickname, memberLevel = user.MemberLevel }
            });
        });

        app.MapGet("/api/credits", (ClaimsPrincipal principal, AccountStore store) =>
        {
            if (!TryUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = store.Get(userId);
            return user is null
                ? Results.NotFound()
                : Results.Ok(new { interpretCredits = user.InterpretCredits, memberLevel = user.MemberLevel });
        });

        app.MapPost("/api/credits/consume", (ConsumeCreditsRequest req, ClaimsPrincipal principal, AccountStore store) =>
        {
            if (!TryUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var amount = Math.Max(1, req.Amount);
            if (!store.TryConsumeCredits(userId, amount, req.ReadingId, out var remaining))
            {
                return Results.BadRequest(new { error = "insufficient credits" });
            }

            return Results.Ok(new { consumed = amount, interpretCredits = remaining });
        });

        app.MapPost("/api/orders/mock-pay", (MockPayRequest req, ClaimsPrincipal principal, AccountStore store) =>
        {
            if (!TryUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var order = store.CreateMockOrder(userId, req.ProductType, req.Amount);
            return Results.Ok(new
            {
                orderNo = order.OrderNo,
                status = "paid",
                payType = "mock",
                grantedCredits = order.GrantedCredits,
                memberLevel = store.Get(userId)?.MemberLevel ?? 0
            });
        });

        app.Run();
    }

    private static bool TryUserId(ClaimsPrincipal principal, out long userId)
    {
        userId = 0;
        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim is not null && long.TryParse(claim, out userId);
    }

    private static string IssueToken(UserAccount user, SymmetricSecurityKey key, IConfiguration config)
    {
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.Phone)
            ],
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class AccountStore
{
    private long _nextUserId = 1;
    private long _nextOrderId = 1;
    private readonly ConcurrentDictionary<long, UserAccount> _users = new();
    private readonly ConcurrentDictionary<string, long> _phoneIndex = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTime> _readingCache = new();

    public bool TryRegister(string phone, string password, string? nickname, out UserAccount user, out string? error)
    {
        user = null!;
        if (_phoneIndex.ContainsKey(phone))
        {
            error = "phone already registered";
            return false;
        }

        var id = Interlocked.Increment(ref _nextUserId);
        user = new UserAccount(id, phone, HashPassword(password), nickname ?? phone, 0, 3);
        _users[id] = user;
        _phoneIndex[phone] = id;
        error = null;
        return true;
    }

    public bool TryLogin(string phone, string password, out UserAccount? user)
    {
        user = null;
        if (!_phoneIndex.TryGetValue(phone, out var id) || !_users.TryGetValue(id, out var found))
        {
            return false;
        }

        if (!VerifyPassword(password, found.PasswordHash))
        {
            return false;
        }

        user = found;
        return true;
    }

    public UserAccount? Get(long userId) => _users.GetValueOrDefault(userId);

    public bool TryConsumeCredits(long userId, int amount, string? readingId, out int remaining)
    {
        remaining = 0;
        if (!_users.TryGetValue(userId, out var user))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(readingId)
            && _readingCache.TryGetValue(readingId, out var cached)
            && cached > DateTime.UtcNow)
        {
            remaining = user.InterpretCredits;
            return true;
        }

        if (user.InterpretCredits < amount)
        {
            return false;
        }

        user = user with { InterpretCredits = user.InterpretCredits - amount };
        _users[userId] = user;
        remaining = user.InterpretCredits;

        if (!string.IsNullOrWhiteSpace(readingId))
        {
            _readingCache[readingId] = DateTime.UtcNow.AddHours(24);
        }

        return true;
    }

    public MockOrder CreateMockOrder(long userId, string productType, decimal amount)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            throw new InvalidOperationException("user not found");
        }

        var orderNo = $"MOCK{Interlocked.Increment(ref _nextOrderId):D8}";
        var granted = productType.Equals("membership", StringComparison.OrdinalIgnoreCase) ? 30 : 10;
        var memberLevel = productType.Equals("membership", StringComparison.OrdinalIgnoreCase) ? 1 : user.MemberLevel;

        user = user with
        {
            InterpretCredits = user.InterpretCredits + granted,
            MemberLevel = Math.Max(user.MemberLevel, memberLevel)
        };
        _users[userId] = user;

        return new MockOrder(orderNo, granted);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

public record UserAccount(long Id, string Phone, string PasswordHash, string Nickname, int MemberLevel, int InterpretCredits);

public record MockOrder(string OrderNo, int GrantedCredits);

public record RegisterRequest(string Phone, string Password, string? Nickname);

public record LoginRequest(string Phone, string Password);

public record ConsumeCreditsRequest(int Amount, string? ReadingId);

public record MockPayRequest(string ProductType, decimal Amount);
