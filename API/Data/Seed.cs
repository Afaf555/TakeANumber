using System.Security.Cryptography;
using System.Text;
using API.Entities;
using API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
namespace API.Data;
public class Seed
{
    public static async Task SeedData(AppDbContext context)
    {
        if (await context.Users.AnyAsync(x => x.Role == Role.Admin)) return;
        using var hmac = new HMACSHA512();
        var admin = new User
        {
            Username = "admin",
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("admin1234")),
            PasswordSalt = hmac.Key,
            Role = Role.Admin
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}