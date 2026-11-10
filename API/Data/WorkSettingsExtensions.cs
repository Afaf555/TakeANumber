using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public static class WorkSettingsExtensions
{
      public static async Task<WorkSettings> GetWorkSettingsAsync(this AppDbContext context)
    {
        var text = await context.Settings.AsNoTracking()
            .Where(x => x.Key == SettingKeys.Work)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();
        return WorkSettings.Parse(text);
    }
}