using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Exceptions;
using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Data;

namespace JourneyHub.Api.Services
{
    public class TripRatingService : ITripRatingService
    {
        private readonly AppDbContext _context;

        public TripRatingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TripRating> RateTripAsync(string userId, int tripId, PostTripRatingDto ratingDto)
        {
            if (ratingDto.Rating < 1 || ratingDto.Rating > 5)
            {
                throw new BadRequestException("Rating must be between 1 and 5.");
            }

            var tripRating = new TripRating
            {
                UserId = userId,
                TripId = tripId,
                Rating = ratingDto.Rating,
                Comment = ratingDto.Comment
            };

            _context.TripRatings.Add(tripRating);
            await _context.SaveChangesAsync();

            return tripRating;
        }
    }
}