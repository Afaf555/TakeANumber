using System.Globalization;
using System.Text.Json;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class CalendarController(AppDbContext context, ActivityLogger logger,
    IHttpClientFactory httpFactory) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<NonWorkingDayDto>>> Get([FromQuery] int? year)
    {
        var query = context.NonWorkingDays.AsQueryable();
        if (year != null) query = query.Where(x => x.Date.Year == year);

        return await query
            .OrderBy(x => x.Date)
            .Select(x => new NonWorkingDayDto { Id = x.Id, Date = x.Date, Name = x.Name })
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<NonWorkingDayDto>> Add(CreateNonWorkingDayDto dto)
    {
        var date = dto.Date.Date;
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        if (await context.NonWorkingDays.AnyAsync(x => x.Date == date))
            return BadRequest("That day is already in the calendar");

        var day = new NonWorkingDay { Date = date, Name = dto.Name.Trim() };
        context.NonWorkingDays.Add(day);
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.NonWorkingDayAdded, "NonWorkingDay", day.Id,
            $"{date:yyyy-MM-dd} {day.Name}");

        return new NonWorkingDayDto { Id = day.Id, Date = day.Date, Name = day.Name };
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Remove(int id)
    {
        var day = await context.NonWorkingDays.FindAsync(id);
        if (day == null) return NotFound();

        context.NonWorkingDays.Remove(day);
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.NonWorkingDayRemoved, "NonWorkingDay", id,
            $"{day.Date:yyyy-MM-dd} {day.Name}");
        return NoContent();
    }

    [HttpPost("import/{year}")]
    public async Task<ActionResult<int>> Import(int year)
    {
        HttpResponseMessage res;
        try
        {
            res = await httpFactory.CreateClient()
                .GetAsync($"https://date.nager.at/api/v3/PublicHolidays/{year}/MK");
        }
        catch
        {
            return BadRequest("Could not reach the holiday service");
        }

        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            return BadRequest("No holiday data for North Macedonia for this year");
        if (!res.IsSuccessStatusCode)
            return BadRequest("Holiday service returned " + (int)res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return BadRequest("Holiday service returned no data");

        using var doc = JsonDocument.Parse(body);

        var existing = (await context.NonWorkingDays
            .Where(x => x.Date.Year == year).Select(x => x.Date).ToListAsync()).ToHashSet();

        var added = 0;
        foreach (var h in doc.RootElement.EnumerateArray())
        {
            if (h.TryGetProperty("global", out var isGlobal) && isGlobal.ValueKind == JsonValueKind.False)
                continue;
            if (h.TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array &&
                !types.EnumerateArray().Any(t => t.GetString() == "Public"))
                continue;

            var dateText = h.GetProperty("date").GetString();
            if (dateText == null || !DateTime.TryParseExact(dateText, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;

            if (existing.Contains(date)) continue;

            context.NonWorkingDays.Add(new NonWorkingDay
            {
                Date = date,
                Name = h.GetProperty("name").GetString() ?? "Holiday"
            });
            existing.Add(date);
            added++;
        }

        await context.SaveChangesAsync();
        await logger.LogAsync(LogActions.NonWorkingDayAdded, "NonWorkingDay", null,
            $"Imported {added} holidays for {year}");
        return added;
    }
}