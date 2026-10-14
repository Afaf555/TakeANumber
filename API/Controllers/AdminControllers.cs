using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext context) : BaseApiController
{

    [HttpGet("services")]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        return await context.Services
            .Select(x => new ServiceDto { Id = x.Id, Name = x.Name })
            .ToListAsync();
    }

    [HttpPost("services")]
    public async Task<ActionResult<ServiceDto>> CreateService(CreateServiceDto dto)
    {
        var service = new Service { Name = dto.Name };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        return new ServiceDto { Id = service.Id, Name = service.Name };
    }

    [HttpDelete("services/{id}")]
    public async Task<ActionResult> DeleteService(int id)
    {
        var service = await context.Services.FindAsync(id);
        if (service == null) return NotFound();

        if (await context.Tickets.AnyAsync(x => x.ServiceId == id))
            return BadRequest("Service has tickets and cannot be deleted");

        var links = context.CounterServices.Where(x => x.ServiceId == id);
        context.CounterServices.RemoveRange(links);
        context.Services.Remove(service);
        await context.SaveChangesAsync();

        return NoContent();
    }


    [HttpGet("counters")]
    public async Task<ActionResult<List<CounterDto>>> GetCounters()
    {
        return await context.Counters
            .Select(x => new CounterDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                ServiceIds = x.CounterServices.Select(cs => cs.ServiceId).ToList()
            })
            .ToListAsync();
    }

    [HttpPost("counters")]
    public async Task<ActionResult<CounterDto>> CreateCounter(CreateCounterDto dto)
    {
        var counter = new Counter { Name = dto.Name, IsActive = true };
        context.Counters.Add(counter);
        await context.SaveChangesAsync();

        return new CounterDto { Id = counter.Id, Name = counter.Name, IsActive = counter.IsActive };
    }

    [HttpPut("counters/{id}/services")]
    public async Task<ActionResult<CounterDto>> AssignServices(int id, AssignServicesDto dto)
    {
        var counter = await context.Counters
            .Include(x => x.CounterServices)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (counter == null) return NotFound();

        var distinctIds = dto.ServiceIds.Distinct().ToList();
        var existingCount = await context.Services.CountAsync(x => distinctIds.Contains(x.Id));
        if (existingCount != distinctIds.Count)
            return BadRequest("One or more services do not exist");

        context.CounterServices.RemoveRange(counter.CounterServices);

        foreach (var serviceId in distinctIds)
        {
            context.CounterServices.Add(new CounterService
            {
                CounterId = counter.Id,
                ServiceId = serviceId
            });
        }

        await context.SaveChangesAsync();

        return new CounterDto
        {
            Id = counter.Id,
            Name = counter.Name,
            IsActive = counter.IsActive,
            ServiceIds = distinctIds
        };
    }

    [HttpPut("counters/{id}/active")]
    public async Task<ActionResult> SetActive(int id, [FromQuery] bool isActive)
    {
        var counter = await context.Counters.FindAsync(id);
        if (counter == null) return NotFound();

        counter.IsActive = isActive;
        await context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPost("employees")]
    public async Task<ActionResult> CreateEmployee(CreateEmployeeDto dto)
    {
        if (await context.Users.AnyAsync(x => x.Username == dto.Username.ToLower()))
            return BadRequest("Username is taken");

        if (dto.CounterId != null)
        {
            if (!await context.Counters.AnyAsync(x => x.Id == dto.CounterId))
                return BadRequest("Counter does not exist");

            if (await context.Users.AnyAsync(x => x.CounterId == dto.CounterId))
                return BadRequest("Counter already has an employee");
        }

        using var hmac = new HMACSHA512();

        var employee = new User
        {
            Username = dto.Username.ToLower(),
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
            PasswordSalt = hmac.Key,
            Role = Role.Employee,
            CounterId = dto.CounterId
        };

        context.Users.Add(employee);
        await context.SaveChangesAsync();

        return Ok(new { employee.Id, employee.Username, employee.CounterId });
    }
    [HttpGet("employees")]
    public async Task<ActionResult<List<EmployeeDto>>> GetEmployees()
    {
        return await context.Users
            .Where(x => x.Role == Role.Employee)
            .Select(x => new EmployeeDto
            {
                Id = x.Id,
                Username = x.Username,
                CounterId = x.CounterId
            })
            .ToListAsync();
    }

    [HttpPut("employees/{id}/counter")]
    public async Task<ActionResult> AssignCounter(int id, AssignCounterDto dto)
    {
        var employee = await context.Users
            .SingleOrDefaultAsync(x => x.Id == id && x.Role == Role.Employee);
        if (employee == null) return NotFound();

        if (dto.CounterId != null)
        {
            if (!await context.Counters.AnyAsync(x => x.Id == dto.CounterId))
                return BadRequest("Counter does not exist");

            if (await context.Users.AnyAsync(x => x.CounterId == dto.CounterId && x.Id != id))
                return BadRequest("Counter already has an employee");
        }

        employee.CounterId = dto.CounterId;
        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpDelete("counters/{id}")]
    public async Task<ActionResult> DeleteCounter(int id)
    {
        var today = DateTime.UtcNow.Date;
        var counter = await context.Counters.FindAsync(id);
        if (counter == null) return NotFound();

        if (await context.Tickets.AnyAsync(x =>
                x.CounterId == id && x.Status == TicketStatus.InProgress &&
                x.CreatedAt >= today))
            return BadRequest("Counter is serving a ticket right now");

        if (await context.Users.AnyAsync(x => x.CounterId == id))
            return BadRequest("Counter has employees assigned and cannot be deleted");

        await context.Tickets
                .Where(x => x.CounterId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.CounterId, (int?)null));
        var links = context.CounterServices.Where(x => x.CounterId == id);
        context.CounterServices.RemoveRange(links);
        context.Counters.Remove(counter);
        await context.SaveChangesAsync();
        return NoContent();

    }

    [HttpDelete("employees/{id}")]
    public async Task<ActionResult> DeleteEmployee(int id)
    {
        var employee = await context.Users.SingleOrDefaultAsync(x => x.Id == id && x.Role == Role.Employee);
        if (employee == null) return NotFound();

        context.Users.Remove(employee);
        await context.SaveChangesAsync();
        return NoContent();
    }
    [HttpPut("counters/{id}/name")]
    public async Task<ActionResult<CounterDto>> RenameCounter(int id, RenameCounterDto renameCounterDto)
    {
        var counter = await context.Counters
        .Include(x => x.CounterServices)
        .SingleOrDefaultAsync(x => x.Id == id);
        if (counter == null) return NotFound();
        var name = renameCounterDto.Name.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Name is required");
        counter.Name = name;
        await context.SaveChangesAsync();
        return new CounterDto
        {
            Id = counter.Id,
            Name = counter.Name,
            IsActive = counter.IsActive,
            ServiceIds = counter.CounterServices.Select(cs => cs.ServiceId).ToList()

        };
    }
    [HttpDelete("employees/{id}/counter")]
    public async Task<ActionResult> UnassignCounter(int id)
    {
        var employee = await context.Users
            .SingleOrDefaultAsync(x => x.Id == id && x.Role == Role.Employee);
        if (employee == null) return NotFound();

        employee.CounterId = null;
        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpGet("tickets")]
public async Task<ActionResult<List<AdminTicketDto>>> GetTickets()
{
    var today = DateTime.UtcNow.Date;

    return await context.Tickets
        .Where(x => x.CreatedAt >= today)
        .OrderBy(x => x.Id)
        .Select(x => new AdminTicketDto
        {
            Id = x.Id,
            Number = x.Number,
            ServiceName = x.Service.Name,
            Status = x.Status.ToString(),
            CreatedAt = x.CreatedAt,
            CounterName = x.Counter != null ? x.Counter.Name : null,
            ClientUsername = x.Client != null ? x.Client.Username : null
        })
        .ToListAsync();
}
}