

using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = "Client")]
public class ClientController(ITicketRepository ticketRepository) : BaseApiController
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
        if (await ticketRepository.GetActiveTicketIdAsync(User.GetUserId()) != null)
            return BadRequest("You already have an active ticket");
        var lastNumber = await ticketRepository.GetLastNumberTodayAsync();
        var ticket = new Ticket
        {
            Number = lastNumber + 1,
            Status = TicketStatus.InQueue,
            CreatedAt = DateTime.UtcNow,
            ServiceId = dto.ServiceId,
            ClientId = User.GetUserId()
        };
        ticketRepository.AddTicket(ticket);
        if (await ticketRepository.SaveAllAsync())
            return await ticketRepository.GetTicketForClientAsync(ticket.Id, User.GetUserId());

        return BadRequest("Problem creating ticket");
    }

    [HttpGet("tickets/{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
        var ticket = await ticketRepository.GetTicketForClientAsync(id, User.GetUserId());
        if (ticket == null) return NotFound();
        return ticket;
    }
    [HttpGet("tickets/active")]
    public async Task<ActionResult<TicketDto>> GetActiveTicket()
    {
        var id = await ticketRepository.GetActiveTicketIdAsync(User.GetUserId());
        if (id == null) return NoContent();

        return await ticketRepository.GetTicketForClientAsync(id.Value, User.GetUserId());
    }

}

