using System.Globalization;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController(AppDbContext context, ActivityLogger logger) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<WorkSettingsDto>> Get() =>
        ToDto(await context.GetWorkSettingsAsync());

    [HttpPut]
    public async Task<ActionResult<WorkSettingsDto>> Update(WorkSettingsDto dto)
    {
        if (!TryParse(dto.OpenTime, out var open) || !TryParse(dto.CloseTime, out var close))
            return BadRequest("Time must be in HH:mm format");
        if (open >= close) return BadRequest("Opening time must be before closing time");
        if (dto.LateGraceMinutes is < 0 or > 120 || dto.EarlyGraceMinutes is < 0 or > 120)
            return BadRequest("Grace must be between 0 and 120 minutes");
        if (dto.StandardDayHours is <= 0 or > 24)
            return BadRequest("Standard day must be between 0 and 24 hours");
        if (dto.QueueAlertWaiting is < 0 or > 500)
            return BadRequest("Waiting people limit must be between 0 and 500 (0 = off)");
        if (dto.QueueAlertMinutes is < 0 or > 240)
            return BadRequest("Waiting time limit must be between 0 and 240 minutes (0 = off)");
        if (dto.AnnualLeaveDays is < 0 or > 60 || dto.AnnualSickDays is < 0 or > 60)
            return BadRequest("Leave days must be between 0 and 60");
        var settings = new WorkSettings
        {
            OpenTime = open,
            CloseTime = close,
            LateGraceMinutes = dto.LateGraceMinutes,
            EarlyGraceMinutes = dto.EarlyGraceMinutes,
            StandardDayHours = dto.StandardDayHours,
            QueueAlertWaiting = dto.QueueAlertWaiting,
            QueueAlertMinutes = dto.QueueAlertMinutes,
            AnnualLeaveDays = dto.AnnualLeaveDays,
            AnnualSickDays = dto.AnnualSickDays
        };

        var text = settings.Serialize();
        var row = await context.Settings.FirstOrDefaultAsync(x => x.Key == SettingKeys.Work);
        if (row == null) context.Settings.Add(new Setting { Key = SettingKeys.Work, Value = text });
        else row.Value = text;


        context.Settings.RemoveRange(await context.Settings.Where(x => x.Key != SettingKeys.Work).ToListAsync());
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.WorkSettingsUpdated, "Settings", null, text);
        return ToDto(settings);
    }

    private static bool TryParse(string value, out TimeSpan time) =>
        TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out time);

    private static WorkSettingsDto ToDto(WorkSettings s) => new()
    {
        OpenTime = s.OpenTime.ToString(@"hh\:mm"),
        CloseTime = s.CloseTime.ToString(@"hh\:mm"),
        LateGraceMinutes = s.LateGraceMinutes,
        EarlyGraceMinutes = s.EarlyGraceMinutes,
        StandardDayHours = s.StandardDayHours,
        QueueAlertWaiting = s.QueueAlertWaiting,
        QueueAlertMinutes = s.QueueAlertMinutes,
        AnnualLeaveDays = s.AnnualLeaveDays,
        AnnualSickDays = s.AnnualSickDays
    };
}