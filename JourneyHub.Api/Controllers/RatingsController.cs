using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Models.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JourneyHub.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/trips/{tripId}/ratings")]
    public class RatingsController : ControllerBase
    {
        private readonly ITripRatingService _tripRatingService;

        public RatingsController(ITripRatingService tripRatingService)
        {
            _tripRatingService = tripRatingService;
        }

        [HttpPost]
        public async Task<IActionResult> RateTrip(int tripId, [FromBody] PostTripRatingDto ratingDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tripRating = await _tripRatingService.RateTripAsync(userId, tripId, ratingDto);
            return Ok(new GenericResponse<TripRating>(tripRating));
        }
    }
}