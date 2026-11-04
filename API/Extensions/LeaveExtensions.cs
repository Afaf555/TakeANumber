using API.Data;
using API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions;

public static class LeaveExtensions
{
   
    public static Task<bool> IsOnLeaveTodayAsync(this AppDbContext context, int userId)
    {
        var today = LocalTime.Today();
        return context.Leaves.AsNoTracking().AnyAsync(l => l.UserId == userId
            && l.Status == LeaveStatus.Approved
            && l.StartDate <= today && l.EndDate >= today);
    }
}
