using System;
using API.DTOs;
using API.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Interfaces;

public interface IEmployeeRepository
{
    Task<int?> GetCounterIdAsync(int userId);
    Task<bool> IsCounterActiveAsync(int counterId);
    Task<Ticket?> GetCurrentTicketAsync(int counterId);
    Task LockCounterAsync(int counterId);
    Task<Ticket?> TakeNextTicketAsync(int counterId);
    Task<EmployeeTicketsDto?> GetTicketDtoAsync(int ticketId);
    Task<bool> SaveAllAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}