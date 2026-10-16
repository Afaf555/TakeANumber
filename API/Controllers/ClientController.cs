

using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers;

[AllowAnonymous]
public class ClientController(ITicketRepository ticketRepository,
    ICounterRepository counterRepository,
    IHubContext<QueueHub> hubContext, ActivityLogger logger) : BaseApiController
{
    [HttpGet("services")]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetServices()
    {
        return Ok(await ticketRepository.GetServicesAsync());
    }
    [HttpPost("tickets")]
    public async Task<ActionResult<TicketDto>> CreateTicket(CreateTicketsDto dto)
    {
        if (!await ticketRepository.ServiceExistsAsync(dto.ServiceId))
        {
            return BadRequest("Service does not exist");
        }

        var ticket = new Ticket
        {
            Number = await ticketRepository.GetLastNumberTodayAsync() + 1,
            Status = TicketStatus.InQueue,
            CreatedAt = DateTime.UtcNow,
            ServiceId = dto.ServiceId,

        };
        ticketRepository.AddTicket(ticket);
        if (!await ticketRepository.SaveAllAsync())
            return BadRequest("Problem creating ticket");

        var created = await ticketRepository.GetTicketAsync(ticket.Id);
        if (created == null) return BadRequest("Problem creating ticket");

        await logger.LogAsync(LogActions.TicketCreated, "Ticket", ticket.Id, created.Code);

        await hubContext.Clients.All.SendAsync("QueuesUpdated",
            await counterRepository.GetServiceQueuesAsync());

        return created;
    }

    [HttpGet("tickets/{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
        var ticket = await ticketRepository.GetTicketAsync(id);
        return ticket == null ? NotFound() : ticket;
    }


}

