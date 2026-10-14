using System;
using API.DTOs;

namespace API.Interfaces;

public interface ICounterRepository
{
    Task<IReadOnlyList<ActiveCounterDto>> GetActiveCountersAsync();

}
