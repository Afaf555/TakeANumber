using System;
using API.DTOs;
using API.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Interfaces;

public interface ITicketRepository
{
    Task<IReadOnlyList<ServiceDto>> GetServicesAsync();
    Task<bool> ServiceExistsAsync(int serviceId);
    Task<int> TakeNextNumberAsync();
    void AddTicket(Ticket ticket);
    Task<TicketDto?> GetTicketAsync(int ticketId);
    Task<bool> SaveAllAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();

}