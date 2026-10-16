using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [AllowAnonymous]
    public class DisplayController(ICounterRepository counterRepository) : BaseApiController
    {
        [HttpGet("counters")]
        public async Task<ActionResult<IReadOnlyList<ActiveCounterDto>>> GetCounters()
        {
            return Ok(await counterRepository.GetActiveCountersAsync());
        }
        [HttpGet("queues")]
        public async Task<ActionResult<IReadOnlyList<ServiceQueueDto>>> GetQueues()
        {
            return Ok(await counterRepository.GetServiceQueuesAsync());
        }
    }

}