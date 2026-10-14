using System;
using API.DTOs;
using API.Entities;

namespace API.Interfaces;

public interface IEmployeeRepository
{
    Task<int?> GetCounterIdAsync(int userId);
    Task<bool> IsCounterActiveAsync(int counterId);
    Task<Ticket?> GetCurrentTicketAsync(int counterId);
    Task<Ticket?> GetNextTicketAsync(int counterId);
    Task<EmployeeTicketsDto?> GetTicketDtoAsync(int ticketId);
    Task<bool> SaveAllAsync();
}
