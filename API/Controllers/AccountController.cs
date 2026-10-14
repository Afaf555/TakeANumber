using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(AppDbContext context, ITokenService tokenService) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
    {
        if (await UserExists(dto.Username))
            return BadRequest("Username is taken");

        using var hmac = new HMACSHA512();

        var user = new User
        {
            Username = dto.Username.ToLower(),
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
            PasswordSalt = hmac.Key,
            Role = Role.Client
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return ToDto(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto dto)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(x => x.Username == dto.Username.ToLower());

        if (user == null) return Unauthorized("Invalid username");

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));

        for (var i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i])
                return Unauthorized("Invalid password");
        }

        return ToDto(user);
    }

    private async Task<bool> UserExists(string username)
    {
        return await context.Users.AnyAsync(x => x.Username == username.ToLower());
    }

    private UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Role = user.Role.ToString(),
        Token = tokenService.CreateToken(user)
    };
}