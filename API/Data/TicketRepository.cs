using API.Extensions;
using System;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Data;

public class TicketRepository(AppDbContext context) : ITicketRepository
{
    public void AddTicket(Ticket ticket)
    {
        context.Tickets.Add(ticket);
    }


    public async Task<int> TakeNextNumberAsync()
    {
        var today = LocalTime.Today();

        var numbers = await context.Database.SqlQuery<int>($"""
            DECLARE @taken TABLE (Value int);
            UPDATE TicketCounters WITH (UPDLOCK, HOLDLOCK)
            SET LastNumber = LastNumber + 1
            OUTPUT inserted.LastNumber INTO @taken
            WHERE [Date] = {today};

            -- first ticket of the day: create the row
            -- (continues after tickets made before this table existed)
            IF NOT EXISTS (SELECT 1 FROM @taken)
                INSERT INTO TicketCounters ([Date], LastNumber)
                OUTPUT inserted.LastNumber INTO @taken
                SELECT {today}, ISNULL(MAX(Number), 0) + 1
                FROM Tickets
                WHERE TicketDate = {today};

            SELECT Value FROM @taken;
            """).ToListAsync();

        return numbers[0];
    }

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync()
    {
        return await context.Services.Where(x => x.IsActive).Select(x => new ServiceDto
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
        return await context.Services.AnyAsync(x => x.Id == serviceId && x.IsActive);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await context.Database.BeginTransactionAsync();
    }

}