using System.Security.Cryptography;
using System.Text;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using Microsoft.EntityFrameworkCore;
namespace API.Data;
public class Seed
{
    public static async Task SeedData(AppDbContext context, IConfiguration config)
    {
        if (await context.Users.AnyAsync(x => x.Role == Role.Admin)) return;

        var password = config["AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "AdminPassword is not configured. Set it in appsettings.Development.json, user-secrets or the AdminPassword environment variable.");
        var error = CredentialRules.ValidatePassword(password);
        if (error != null)
            throw new InvalidOperationException("AdminPassword is not valid: " + error);

        using var hmac = new HMACSHA512();
        var admin = new User
        {
            Username = "admin",
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)),
            PasswordSalt = hmac.Key,
            Role = Role.Admin
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
