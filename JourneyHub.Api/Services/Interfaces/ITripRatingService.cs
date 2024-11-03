using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;

namespace JourneyHub.Api.Services.Interfaces
{
    public interface ITripRatingService
    {
        Task<TripRating> RateTripAsync(string userId, int tripId, PostTripRatingDto ratingDto);
    }
}