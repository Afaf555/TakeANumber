using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController(
    AppDbContext context,
    IEmployeeRepository employeeRepository,
    ICounterRepository counterRepository,
    IHubContext<QueueHub> hubContext, ActivityLogger logger) : BaseApiController
    {
        [HttpGet("current")]
        public async Task<ActionResult<EmployeeTicketsDto>> GetCurrentTicket()
        {
            var counterId = await employeeRepository.GetCounterIdAsync(User.GetUserId());
            if (counterId == null) return BadRequest("Employee is not assigned to a counter");

            var current = await employeeRepository.GetCurrentTicketAsync(counterId.Value);
            if (current == null) return NoContent();

            return await employeeRepository.GetTicketDtoAsync(current.Id);
        }

        [HttpGet("status")]
        public async Task<ActionResult> GetStatus()
        {
            var userId = User.GetUserId();
            var onBreak = await context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.IsOnBreak)
                .SingleAsync();
            return Ok(new { isOnBreak = onBreak });
        }

        [HttpPost("call-next")]
        public async Task<ActionResult<EmployeeTicketsDto>> CallNext()
        {
            var userId = User.GetUserId();
            var counterId = await employeeRepository.GetCounterIdAsync(userId);
            if (counterId == null) return BadRequest("Employee is not assigned to a counter");

            if (await context.Users.AnyAsync(u => u.Id == userId && u.IsOnBreak))
                return BadRequest("You are on break");

            var active = await employeeRepository.IsCounterActiveAsync(counterId.Value);
            if (!active) return BadRequest("Counter is not active");

            if (await employeeRepository.GetCurrentTicketAsync(counterId.Value) != null)
            {
                return BadRequest("Finish the current ticket first");
            }

            var next = await employeeRepository.GetNextTicketAsync(counterId.Value);
            if (next == null) return NotFound("No tickets in queue");

            next.Status = TicketStatus.InProgress;
            next.StartedAt = DateTime.UtcNow;
            next.CounterId = counterId.Value;

            if (await employeeRepository.SaveAllAsync())
            {
                var counters = await counterRepository.GetActiveCountersAsync();
                await hubContext.Clients.All.SendAsync("CountersUpdated", counters);
                await hubContext.Clients.All.SendAsync("QueuesUpdated", await counterRepository.GetServiceQueuesAsync());

                var ticket = await employeeRepository.GetTicketDtoAsync(next.Id);
                if (ticket == null) return NotFound();

                await logger.LogAsync(LogActions.TicketCalled, "Ticket", next.Id,
                    $"{ticket.Code}, counter {counterId}");
                return ticket;
            }

            return BadRequest("Problem calling next ticket");
        }

        [HttpPost("finish")]
        public async Task<ActionResult<EmployeeTicketsDto>> FinishCurrent()
        {
            var counterId = await employeeRepository.GetCounterIdAsync(User.GetUserId());
            if (counterId == null) return BadRequest("Employee is not assigned to a counter");

            var current = await employeeRepository.GetCurrentTicketAsync(counterId.Value);
            if (current == null) return BadRequest("No ticket in progress");

            current.Status = TicketStatus.Finished;
            current.FinishedAt = DateTime.UtcNow;

            if (await employeeRepository.SaveAllAsync())
            {

                var counters = await counterRepository.GetActiveCountersAsync();
                await hubContext.Clients.All.SendAsync("CountersUpdated", counters);
                await hubContext.Clients.All.SendAsync("QueuesUpdated", await counterRepository.GetServiceQueuesAsync());
                var ticket = await employeeRepository.GetTicketDtoAsync(current.Id);
                if (ticket == null) return NotFound();

                await logger.LogAsync(LogActions.TicketFinished, "Ticket", current.Id, ticket.Code);
                return ticket;
            }

            return BadRequest("Problem finishing ticket");
        }

        [HttpPost("no-show")]
        public async Task<ActionResult<EmployeeTicketsDto>> MarkNoShow()
        {
            var counterId = await employeeRepository.GetCounterIdAsync(User.GetUserId());
            if (counterId == null) return BadRequest("Employee is not assigned to a counter");
            var current = await employeeRepository.GetCurrentTicketAsync(counterId.Value);
            if (current == null) return BadRequest("No ticket in progress");

            current.Status = TicketStatus.NoShow;
            current.FinishedAt = DateTime.UtcNow;

            if (await employeeRepository.SaveAllAsync())
            {
                var counters = await counterRepository.GetActiveCountersAsync();
                await hubContext.Clients.All.SendAsync("CountersUpdated", counters);
                await hubContext.Clients.All.SendAsync("QueuesUpdated", await counterRepository.GetServiceQueuesAsync());

                var ticket = await employeeRepository.GetTicketDtoAsync(current.Id);
                if (ticket == null) return NotFound();

                await logger.LogAsync(LogActions.TicketNoShow, "Ticket", current.Id, ticket.Code);
                return ticket;
            }
            return BadRequest("Problem marking ticket no-show");
        }

        [HttpPost("break/start")]
        public async Task<ActionResult> StartBreak()
        {
            var userId = User.GetUserId();
            var counterId = await employeeRepository.GetCounterIdAsync(userId);
            if (counterId == null) return BadRequest("Employee is not assigned to a counter");

            if (await employeeRepository.GetCurrentTicketAsync(counterId.Value) != null)
                return BadRequest("Finish the current ticket first");

            var user = await context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            if (user.IsOnBreak) return BadRequest("Already on break");

            var session = await OpenSessionAsync(userId);
            if (session == null) return BadRequest("No open session");

            user.IsOnBreak = true;
            context.EmployeeSessions.Add(new EmployeeSession
            {
                UserId = userId,
                LoginAt = session.LoginAt,
                CounterId = session.CounterId,
                CounterName = session.CounterName,
                Services = session.Services,
                BreakStartedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await logger.LogAsync(LogActions.BreakStarted, "Counter", counterId.Value, $"counter {counterId}");
            await hubContext.Clients.All.SendAsync("CountersUpdated", await counterRepository.GetActiveCountersAsync());
            return NoContent();
        }

        [HttpPost("break/end")]
        public async Task<ActionResult> EndBreak()
        {
            var userId = User.GetUserId();
            var counterId = await employeeRepository.GetCounterIdAsync(userId);
            if (counterId == null) return BadRequest("Employee is not assigned to a counter");

            var user = await context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            if (!user.IsOnBreak) return BadRequest("Not on break");

            var now = DateTime.UtcNow;
            var minutes = 0.0;

            var brk = await context.EmployeeSessions
                .Where(s => s.UserId == userId && s.BreakStartedAt != null && s.BreakEndedAt == null)
                .OrderByDescending(s => s.BreakStartedAt)
                .FirstOrDefaultAsync();
            if (brk != null)
            {
                brk.BreakEndedAt = now;
                minutes = (now - brk.BreakStartedAt!.Value).TotalMinutes;
            }

            user.IsOnBreak = false;
            await context.SaveChangesAsync();

            await logger.LogAsync(LogActions.BreakEnded, "Counter", counterId.Value,
                $"counter {counterId}, {minutes:0.#} min");
            await hubContext.Clients.All.SendAsync("CountersUpdated", await counterRepository.GetActiveCountersAsync());
            return NoContent();
        }

        private Task<EmployeeSession?> OpenSessionAsync(int userId) =>
            context.EmployeeSessions
                .Where(s => s.UserId == userId && s.LogoutAt == null && s.BreakStartedAt == null)
                .OrderByDescending(s => s.LoginAt)
                .FirstOrDefaultAsync();
    }
}