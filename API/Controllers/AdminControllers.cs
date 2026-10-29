using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext context, ActivityLogger logger) : BaseApiController
{

    [HttpGet("services")]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        var today = DateTime.UtcNow.Date;
        return await context.Services
            .Select(x => new ServiceDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                HasActiveTickets = context.Tickets.Any(t => t.ServiceId == x.Id && t.CreatedAt >= today &&
                (t.Status == TicketStatus.InQueue || t.Status == TicketStatus.InProgress))
            })
            .ToListAsync();
    }

    [HttpPost("services")]
    public async Task<ActionResult<ServiceDto>> CreateService(CreateServiceDto dto)
    {
        var code = dto.Code.Trim().ToUpper();
        if (await context.Services.AnyAsync(x => x.Code == code))
            return BadRequest("Code is already used");
        var service = new Service { Name = dto.Name, Code = dto.Code };
        context.Services.Add(service);
        await context.SaveChangesAsync();
        await logger.LogAsync(LogActions.ServiceCreated, "Service", service.Id, $"{code} - {service.Name}");
        return new ServiceDto { Id = service.Id, Name = service.Name, Code = service.Code };
    }


    [HttpDelete("services/{id}")]
    public async Task<ActionResult> DeleteService(int id)
    {
        var service = await context.Services.FindAsync(id);
        if (service == null) return NotFound();
        var today = DateTime.UtcNow.Date;
        if (await context.Tickets.AnyAsync(x => x.ServiceId == id && x.CreatedAt >= today &&
        (x.Status == TicketStatus.InQueue || x.Status == TicketStatus.InProgress)))
            return BadRequest("Service has active tickets");

        var serviceLabel = $"{service.Code} - {service.Name}";

        var tickets = await context.Tickets.Where(x => x.ServiceId == id).ToListAsync();
        context.Tickets.RemoveRange(tickets);
        var links = context.CounterServices.Where(x => x.ServiceId == id);
        var affectedCounterIds = await context.CounterServices
     .Where(x => x.ServiceId == id)
     .Select(x => x.CounterId)
     .ToListAsync();
        context.CounterServices.RemoveRange(links);
        context.Services.Remove(service);
        await context.SaveChangesAsync();
        var affectedEmployeeIds = await context.Users
            .Where(x => x.CounterId != null && affectedCounterIds.Contains(x.CounterId.Value))
            .Select(x => x.Id)
            .ToListAsync();
        foreach (var userId in affectedEmployeeIds)
            await RefreshOpenSessionAsync(userId);
        await logger.LogAsync(LogActions.ServiceDeleted, "Service", id,
                $"{serviceLabel}: {tickets.Count} tickets deleted");
        return NoContent();
    }


    [HttpGet("counters")]
    public async Task<ActionResult<List<CounterDto>>> GetCounters()
    {
        var today = DateTime.UtcNow.Date;
        return await context.Counters
            .Select(x => new CounterDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                ServiceIds = x.CounterServices.Select(cs => cs.ServiceId).ToList(),
                HasActiveTickets = context.Tickets.Any(t => t.CreatedAt >= today &&
    ((t.Status == TicketStatus.InProgress && t.CounterId == x.Id) ||
     (t.Status == TicketStatus.InQueue &&
      x.CounterServices.Any(cs => cs.ServiceId == t.ServiceId))))
            })
            .ToListAsync();
    }

    [HttpPost("counters")]
    public async Task<ActionResult<CounterDto>> CreateCounter(CreateCounterDto dto)
    {
        var counter = new Counter { Name = dto.Name, IsActive = true };
        context.Counters.Add(counter);
        await context.SaveChangesAsync();
        await logger.LogAsync(LogActions.CounterCreated, "Counter", counter.Id, counter.Name);
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

        var oldIds = counter.CounterServices.Select(cs => cs.ServiceId).ToList();   // ← ново

        context.CounterServices.RemoveRange(counter.CounterServices);

        foreach (var serviceId in distinctIds)
        {
            context.CounterServices.Add(new CounterService { CounterId = counter.Id, ServiceId = serviceId });
        }

        await context.SaveChangesAsync();


        if (!oldIds.OrderBy(x => x).SequenceEqual(distinctIds.OrderBy(x => x)))
        {
            await logger.LogAsync(LogActions.CounterServicesChanged, "Counter", counter.Id,
                $"{counter.Name}: [{await ServiceNames(oldIds)}] -> [{await ServiceNames(distinctIds)}]");

            var employeeIds = await context.Users
                .Where(x => x.CounterId == counter.Id)
                .Select(x => x.Id)
                .ToListAsync();
            foreach (var userId in employeeIds)
                await RefreshOpenSessionAsync(userId);
        }
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
        var wasActive = counter.IsActive;
        counter.IsActive = isActive;
        await context.SaveChangesAsync();
        if (wasActive != isActive)
            await logger.LogAsync(LogActions.CounterActiveChanged, "Counter", id,
                $"{counter.Name}: {(isActive ? "inactive -> active" : "active -> inactive")}");

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
        await logger.LogAsync(LogActions.EmployeeCreated, "Employee", employee.Id,
            $"{employee.Username}, counter: {await CounterName(dto.CounterId)}");
        return Ok(new { employee.Id, employee.Username, employee.CounterId });
    }
    [HttpGet("employees")]
    public async Task<ActionResult<List<EmployeeDto>>> GetEmployees()
    {
        var today = DateTime.UtcNow.Date;
        return await context.Users
            .Where(x => x.Role == Role.Employee)
            .Select(x => new EmployeeDto
            {
                Id = x.Id,
                Username = x.Username,
                CounterId = x.CounterId,
                HasActiveTickets = x.CounterId != null && context.Tickets.Any(t => t.CreatedAt >= today &&
                    ((t.Status == TicketStatus.InProgress && t.CounterId == x.CounterId) ||
                     (t.Status == TicketStatus.InQueue &&
                      context.CounterServices.Any(cs => cs.CounterId == x.CounterId && cs.ServiceId == t.ServiceId))))
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

        var oldCounterId = employee.CounterId;
        employee.CounterId = dto.CounterId;
        await context.SaveChangesAsync();
        await RefreshOpenSessionAsync(employee.Id);
        await logger.LogAsync(LogActions.EmployeeCounterChanged, "Employee", employee.Id,
            $"{employee.Username}: {await CounterName(oldCounterId)} -> {await CounterName(dto.CounterId)}");

        return NoContent();
    }
    [HttpDelete("counters/{id}")]
    public async Task<ActionResult> DeleteCounter(int id)
    {
        var today = DateTime.UtcNow.Date;
        var counter = await context.Counters.FindAsync(id);
        if (counter == null) return NotFound();
        var counterName = counter.Name;

        if (await CounterHasTicketsAsync(id))
            return BadRequest("Counter has active tickets");

        if (await context.Users.AnyAsync(x => x.CounterId == id))
            return BadRequest("Counter has employees assigned and cannot be deleted");

        await context.Tickets
                .Where(x => x.CounterId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.CounterId, (int?)null));
        var links = context.CounterServices.Where(x => x.CounterId == id);
        context.CounterServices.RemoveRange(links);
        context.Counters.Remove(counter);
        await context.SaveChangesAsync();
        await logger.LogAsync(LogActions.CounterDeleted, "Counter", id, counterName);

        return NoContent();

    }

    [HttpDelete("employees/{id}")]
    public async Task<ActionResult> DeleteEmployee(int id)
    {
        var employee = await context.Users.SingleOrDefaultAsync(x => x.Id == id && x.Role == Role.Employee);
        if (employee == null) return NotFound();
        if (employee.CounterId != null && await CounterHasTicketsAsync(employee.CounterId.Value))
            return BadRequest("The employee's counter has active tickets");

        var employeeName = employee.Username;
        var sessions = await context.EmployeeSessions
    .Where(s => s.UserId == id)
    .ToListAsync();

        context.EmployeeSessions.RemoveRange(sessions);
        context.Users.Remove(employee);
        await context.SaveChangesAsync();
        await logger.LogAsync(LogActions.EmployeeDeleted, "Employee", id, $"{employeeName}: {sessions.Count} login sessions deleted");
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
        var oldName = counter.Name;
        counter.Name = name;
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.CounterRenamed, "Counter", counter.Id,
            $"{oldName} -> {name}");
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

        var oldCounterId = employee.CounterId;
        employee.CounterId = null;
        await context.SaveChangesAsync();
        await RefreshOpenSessionAsync(employee.Id);
        await logger.LogAsync(LogActions.EmployeeCounterChanged, "Employee", employee.Id,
            $"{employee.Username}: {await CounterName(oldCounterId)} -> none");

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
                ClientUsername = x.Client != null ? x.Client.Username : null,
                ServiceCode = x.Service.Code,
            })
            .ToListAsync();
    }
    private async Task RefreshOpenSessionAsync(int userId)
    {
        var session = await context.EmployeeSessions
                            .Where(s => s.UserId == userId && s.LogoutAt == null && s.BreakStartedAt == null)
                            .OrderByDescending(s => s.LoginAt)
                            .FirstOrDefaultAsync();

        if (session == null) return;
        var counterId = await context.Users
        .Where(u => u.Id == userId)
        .Select(u => u.CounterId)
        .SingleOrDefaultAsync();

        string? counterName = null;
        string? services = null;
        if (counterId != null)
        {
            counterName = await context.Counters
            .Where(c => c.Id == counterId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

            var names = await context.CounterServices
                    .Where(cs => cs.CounterId == counterId)
                    .Select(cs => cs.Service.Code + " - " + cs.Service.Name)
                    .ToListAsync();

            services = names.Count > 0 ? string.Join(", ", names) : null;
            if (services != null && services.Length > 500) services = services[..500];
        }
        session.CounterId = counterId;
        session.CounterName = counterName;
        session.Services = services;
        await context.SaveChangesAsync();
    }
    private async Task<string> CounterName(int? id)
    {
        return id == null ? "none"
               : await context.Counters.Where(c => c.Id == id).Select(c => c.Name).SingleOrDefaultAsync() ?? "none";
    }
    private async Task<string> ServiceNames(IEnumerable<int> ids)
    {
        var list = ids.ToList();
        if (list.Count == 0) return "none";

        var names = await context.Services
            .Where(s => list.Contains(s.Id))
            .Select(s => s.Name)
            .ToListAsync();

        return string.Join(", ", names);
    }
    private async Task<bool> CounterHasTicketsAsync(int counterId)
    {
        var today = DateTime.UtcNow.Date;
        return await context.Tickets.AnyAsync(t => t.CreatedAt >= today &&
            ((t.Status == TicketStatus.InProgress && t.CounterId == counterId) ||
             (t.Status == TicketStatus.InQueue &&
              context.CounterServices.Any(cs => cs.CounterId == counterId && cs.ServiceId == t.ServiceId))));
    }
}
