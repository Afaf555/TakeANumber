// Data/CounterRepository.cs
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
        var today = DateTime.UtcNow.Date;

        var counters = await context.Counters
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        var inProgress = await context.Tickets
            .Where(t => t.Status == TicketStatus.InProgress && t.CreatedAt >= today && t.CounterId != null)
            .Select(t => new { t.CounterId, t.Number, ServiceCode = t.Service.Code })
            .ToListAsync();

        return counters.Select(c =>
        {
            var t = inProgress.FirstOrDefault(x => x.CounterId == c.Id);
            return new ActiveCounterDto
            {
                CounterId = c.Id,
                CounterName = c.Name,
                CurrentTicketNumber = t?.Number,
                CurrentTicketCode = t == null ? null : TicketCode.Format(t.ServiceCode, t.Number)
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<ServiceQueueDto>> GetServiceQueuesAsync()
    {
        var today = DateTime.UtcNow.Date;

        var tickets = await context.Tickets
            .Where(t => t.CreatedAt >= today &&
                        (t.Status == TicketStatus.InQueue || t.Status == TicketStatus.InProgress))
            .Select(t => new
            {
                t.Id,
                t.ServiceId,
                t.Number,
                t.Status,
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
                .ToList()
        }).ToList();
    }
}