using API.DTOs;
using API.Entities.Enums;
using API.Extensions;
using API.Interfaces;
using API.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController(
    IEmployeeRepository employeeRepository,
    ICounterRepository counterRepository,
    IHubContext<QueueHub> hubContext) : BaseApiController
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


        [HttpPost("call-next")]
        public async Task<ActionResult<EmployeeTicketsDto>> CallNext()
        {
            var counterId = await employeeRepository.GetCounterIdAsync(User.GetUserId());
            if (counterId == null) return BadRequest("Employee is not assigned to a counter");

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

                var ticket = await employeeRepository.GetTicketDtoAsync(next.Id);
                if (ticket == null) return NotFound();
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

                var ticket = await employeeRepository.GetTicketDtoAsync(current.Id);
                if (ticket == null) return NotFound();
                return ticket;
            }

            return BadRequest("Problem finishing ticket");
        }

    }
}
