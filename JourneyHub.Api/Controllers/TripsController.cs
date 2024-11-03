using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Models.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public class PagedResponse<T> : GenericResponse<T>
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public PagedResponse(T data, int currentPage, int pageSize, int totalCount) : base(data)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}

namespace JourneyHub.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripServices _tripService;

        public TripsController(ITripServices tripService)
        {
            _tripService = tripService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTripAsync([FromBody] PostTripRequestDto tripDto)
        {
            string userId = GetUserId();
            Trip trip = await _tripService.CreateTripAsync(tripDto, userId);
            return Ok(new GenericResponse<Trip>(trip));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTripsAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var (trips, totalCount) = await _tripService.GetTripsPagedAsync(pageNumber, pageSize);
            var response = new PagedResponse<IEnumerable<GetTripsResponseDto>>(trips, pageNumber, pageSize, totalCount);
            return Ok(response);
        }

        [HttpGet("user-trips")]
        [Authorize]
        public async Task<IActionResult> GetUserTripsAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            string userId = GetUserId();
            var (trips, totalCount) = await _tripService.GetTripsByUserIdAsync(userId, pageNumber, pageSize);
            var response = new PagedResponse<IEnumerable<GetTripsResponseDto>>(trips, pageNumber, pageSize, totalCount);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTripByIdAsync(int id)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            return trip != null ? Ok(new GenericResponse<Trip>(trip)) : NotFoundResponse<Trip>();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTripAsync(int id)
        {
            string userId = GetUserId();
            var result = await _tripService.DeleteTripAsync(id, userId);
            return result ? Ok() : NotFoundResponse<string>();
        }

        // Helper Methods
        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private IActionResult NotFoundResponse<T>()
        {
            return NotFound();
        }
    }
}
