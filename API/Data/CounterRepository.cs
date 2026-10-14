// Data/CounterRepository.cs
using API.DTOs;
using API.Entities.Enums;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class CounterRepository(AppDbContext context) : ICounterRepository
{
    public async Task<IReadOnlyList<ActiveCounterDto>> GetActiveCountersAsync()
    {   
        var today=DateTime.UtcNow.Date;
        return await context.Counters
            .Where(x => x.IsActive)
            .Select(x => new ActiveCounterDto
            {
                CounterId = x.Id,
                CounterName = x.Name,
                CurrentTicketNumber = context.Tickets
                    .Where(t => t.CounterId == x.Id && t.Status == TicketStatus.InProgress
                    &&t.CreatedAt>=today)
                    .Select(t => (int?)t.Number)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }


}