
using API.DTOs;
using API.Entities.Enums;
using API.Extensions;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class CounterRepository(AppDbContext context) : ICounterRepository
{
    public async Task<IReadOnlyList<ActiveCounterDto>> GetActiveCountersAsync()
    {
        var today = LocalTime.TodayStartUtc();

        var counters = await context.Counters
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        var inProgress = await context.Tickets
            .Where(t => t.Status == TicketStatus.InProgress && t.CreatedAt >= today && t.CounterId != null)
            .Select(t => new { t.CounterId, t.Number, ServiceCode = t.Service.Code })
            .ToListAsync();
        var onBreakCounterIds = await context.Users
            .Where(u => u.IsOnBreak && u.CounterId != null)
            .Select(u => u.CounterId!.Value)
            .ToListAsync();
        // services of every counter, and today's waiting tickets (oldest first)
        var counterServices = await context.CounterServices
            .Select(cs => new { cs.CounterId, cs.ServiceId })
            .ToListAsync();
        var waiting = await context.Tickets
            .Where(t => t.Status == TicketStatus.InQueue && t.CreatedAt >= today)
            .OrderBy(t => t.Id)
            .Select(t => new { t.ServiceId, t.Number, ServiceCode = t.Service.Code })
            .ToListAsync();
        return counters.Select(c =>
        {
            var t = inProgress.FirstOrDefault(x => x.CounterId == c.Id);
            var serviceIds = counterServices.Where(cs => cs.CounterId == c.Id).Select(cs => cs.ServiceId).ToHashSet();
            var mine = waiting.Where(w => serviceIds.Contains(w.ServiceId)).ToList();
            return new ActiveCounterDto
            {
                CounterId = c.Id,
                CounterName = c.Name,
                CurrentTicketNumber = t?.Number,
                CurrentTicketCode = t == null ? null : TicketCode.Format(t.ServiceCode, t.Number),
                IsOnBreak = onBreakCounterIds.Contains(c.Id),
                NextCodes = mine.Take(10).Select(w => TicketCode.Format(w.ServiceCode, w.Number)).ToList(),
                Waiting = mine.Count,
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<ServiceQueueDto>> GetServiceQueuesAsync()
    {
        var today = LocalTime.TodayStartUtc();

        var tickets = await context.Tickets
            .Where(t => t.CreatedAt >= today &&
                        (t.Status == TicketStatus.InQueue || t.Status == TicketStatus.InProgress))
            .Select(t => new
            {
                t.Id,
                t.ServiceId,
                t.Number,
                t.Status,
                t.CreatedAt,
                CounterName = t.Counter != null ? t.Counter.Name : ""
            })
            .ToListAsync();

        var services = await context.Services.Select(s => new { s.Id, s.Name, s.Code }).ToListAsync();

        return services.Select(s => new ServiceQueueDto
        {
            ServiceId = s.Id,
            ServiceName = s.Name,
            Serving = tickets
                .Where(t => t.ServiceId == s.Id && t.Status == TicketStatus.InProgress)
                .Select(t => new ServingDto { Code = TicketCode.Format(s.Code, t.Number), CounterName = t.CounterName })
                .ToList(),
            NextCodes = tickets
                .Where(t => t.ServiceId == s.Id && t.Status == TicketStatus.InQueue)
                .OrderBy(t => t.Id)
                .Take(10)
                .Select(t => TicketCode.Format(s.Code, t.Number))
                .ToList(),
            Waiting = tickets.Count(t => t.ServiceId == s.Id && t.Status == TicketStatus.InQueue),
            OldestWaitingSince = tickets
                .Where(t => t.ServiceId == s.Id && t.Status == TicketStatus.InQueue)
                .Select(t => (DateTime?)t.CreatedAt)
                .Min()
        }).ToList();
    }
}