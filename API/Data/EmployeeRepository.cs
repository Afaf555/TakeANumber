using API.Extensions;
using System;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class EmployeeRepository (AppDbContext context): IEmployeeRepository
{
    public async Task<int?> GetCounterIdAsync(int userId)
    {
        return await context.Users
        .Where(x=> x.Id==userId)
        .Select(x=>x.CounterId)
        .SingleOrDefaultAsync();
    }

    public async Task<Ticket?> GetCurrentTicketAsync(int counterId)
{
    var today = LocalTime.TodayStartUtc();
    return await context.Tickets.FirstOrDefaultAsync(x =>
        x.CounterId == counterId && x.Status == TicketStatus.InProgress && x.CreatedAt >= today);
}

public async Task<Ticket?> GetNextTicketAsync(int counterId)
{
    var today = LocalTime.TodayStartUtc();
    return await context.Tickets
        .Where(x => x.Status == TicketStatus.InQueue
            && x.CreatedAt >= today
            && context.CounterServices.Any(
                c => c.CounterId == counterId && c.ServiceId == x.ServiceId))
        .OrderBy(x => x.Id)
        .FirstOrDefaultAsync();
}

    public async Task<EmployeeTicketsDto?> GetTicketDtoAsync(int ticketId)
    {
        return await context.Tickets
        .Where(x=>x.Id==ticketId)
        .Select(x=> new EmployeeTicketsDto
        {
            Id=x.Id,
            Number=x.Number,
            ServiceName=x.Service.Name,
            Status=x.Status.ToString(),
            CreatedAt=x.CreatedAt,
            StartedAt=x.StartedAt,
            ServiceCode = x.Service.Code,
        }).SingleOrDefaultAsync();
    }

    public async Task<bool> IsCounterActiveAsync(int counterId)
    {
        return await context.Counters.AnyAsync(x=>x.Id==counterId && x.IsActive);
    }

    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync()>0;
    }
}