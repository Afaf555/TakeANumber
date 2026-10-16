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
    Task<TicketDto?> GetTicketAsync(int ticketId);
    Task<bool> SaveAllAsync();

}
