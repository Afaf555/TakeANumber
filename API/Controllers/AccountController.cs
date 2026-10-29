using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(AppDbContext context, ITokenService tokenService, ActivityLogger logger, IHubContext<QueueHub> hubContext, ICounterRepository counterRepository) : BaseApiController
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

        if (user.Role == Role.Employee)
        {
            var wasOnBreak = user.IsOnBreak;
            user.IsOnBreak = false;
            var staleBreaks = await context.EmployeeSessions
    .Where(s => s.UserId == user.Id && s.BreakStartedAt != null && s.BreakEndedAt == null)
    .ToListAsync();
            foreach (var b in staleBreaks) b.BreakEndedAt = DateTime.UtcNow;


            string? counterName = null;
            string? services = null;

            if (user.CounterId != null)
            {
                counterName = await context.Counters
                    .Where(c => c.Id == user.CounterId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                var serviceIds = await context.CounterServices
                    .Where(cs => cs.CounterId == user.CounterId)
                    .Select(cs => cs.ServiceId)
                    .ToListAsync();

                var names = await context.Services
                    .Where(s => serviceIds.Contains(s.Id))
                    .Select(s => s.Code + " - " + s.Name)
                    .ToListAsync();

                services = names.Count > 0 ? string.Join(", ", names) : null;
            }
            var now=DateTime.UtcNow;
            var openSessions=await context.EmployeeSessions
            .Where(s=>s.UserId==user.Id&&s.BreakStartedAt==null&&s.LogoutAt==null).ToListAsync();
            foreach (var s in openSessions)
            {
                var endOfDay=s.LoginAt.Date.AddDays(1);
                s.LogoutAt=now<endOfDay?now:endOfDay;
            }
            context.EmployeeSessions.Add(new EmployeeSession
            {
                UserId = user.Id,
                CounterId = user.CounterId,
                CounterName = counterName,
                Services = services
            });
            await context.SaveChangesAsync();

            if (wasOnBreak)
                await hubContext.Clients.All.SendAsync("CountersUpdated",
                    await counterRepository.GetActiveCountersAsync());

            await logger.LogAsync(LogActions.EmployeeLogin, "Employee", user.Id,
                $"counter: {counterName ?? "none"}; services: {services ?? "none"}",
                actorId: user.Id, actorName: user.Username);
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

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetUserId();
        var session = await context.EmployeeSessions
            .Where(s => s.UserId == userId && s.LogoutAt == null && s.BreakStartedAt == null)
            .OrderByDescending(s => s.LoginAt)
            .FirstOrDefaultAsync();

        if (session != null)
        {
            session.LogoutAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var user = await context.Users.FindAsync(userId);
            await logger.LogAsync(LogActions.EmployeeLogout, "Employee", userId,
                $"counter: {session.CounterName ?? "none"}",
                actorId: userId, actorName: user!.Username);
        }
        return NoContent();
    }
}