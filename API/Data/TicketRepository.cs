using API.Extensions;
using System;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class TicketRepository(AppDbContext context) : ITicketRepository
{
    public void AddTicket(Ticket ticket)
    {
        context.Tickets.Add(ticket);
    }

    public async Task<int> GetLastNumberTodayAsync()
    {
        var today = LocalTime.TodayStartUtc();

        return await context.Tickets
            .Where(x => x.CreatedAt >= today)
            .MaxAsync(x => (int?)x.Number) ?? 0;
    }
    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync()
    {
        return await context.Services.Select(x => new ServiceDto
        {
            Id = x.Id,
            Name = x.Name,
            Code = x.Code
        }).ToListAsync();
    }

    public async Task<TicketDto?> GetTicketAsync(int ticketId)
    {
        var today = LocalTime.TodayStartUtc();

        return await context.Tickets
            .Where(x => x.Id == ticketId && x.CreatedAt >= today)
            .Select(x => new TicketDto
            {
                Id = x.Id,
                Number = x.Number,
                ServiceId = x.ServiceId,
                ServiceName = x.Service.Name,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt,
                CounterName = x.Counter != null ? x.Counter.Name : null,
                PeopleAhead = x.Status == TicketStatus.InQueue
                    ? context.Tickets.Count(t =>
                        t.ServiceId == x.ServiceId &&
                        t.Status == TicketStatus.InQueue &&
                        t.CreatedAt >= today &&
                        t.Id < x.Id)
                    : 0,
                ServiceCode = x.Service.Code,
            })
            .SingleOrDefaultAsync();
    }

    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ServiceExistsAsync(int serviceId)
    {
        return await context.Services.AnyAsync(x => x.Id == serviceId);
    }
}