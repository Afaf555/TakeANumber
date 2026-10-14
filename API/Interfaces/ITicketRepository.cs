using System;
using API.DTOs;
using API.Entities;

namespace API.Interfaces;

public interface ITicketRepository
{
    Task<IReadOnlyList<ServiceDto>> GetServicesAsync();
    Task<bool> ServiceExistsAsync(int serviceId);
    Task<int> GetLastNumberTodayAsync();
    void AddTicket(Ticket ticket);
    Task<TicketDto?> GetTicketForClientAsync(int ticketId, int clientId);
    Task<bool> SaveAllAsync();
    Task<int?> GetActiveTicketIdAsync(int clientId);

}
