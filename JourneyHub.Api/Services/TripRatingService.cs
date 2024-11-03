using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Exceptions;
using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Data;
using Microsoft.EntityFrameworkCore;

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
            ValidateRating(ratingDto.Rating);

            var existingRating = await GetExistingRatingAsync(userId, tripId);
            if (existingRating != null)
                throw new BadRequestException("User has already rated this trip.");

            var tripRating = CreateTripRating(userId, tripId, ratingDto);

            await SaveRatingAsync(tripRating);

            return tripRating;
        }

        private void ValidateRating(int rating)
        {
            if (rating < 1 || rating > 5)
            {
                throw new BadRequestException("Rating must be between 1 and 5.");
            }
        }

        private async Task<TripRating?> GetExistingRatingAsync(string userId, int tripId)
        {
            return await _context.TripRatings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.TripId == tripId);
        }

        private TripRating CreateTripRating(string userId, int tripId, PostTripRatingDto ratingDto)
        {
            return new TripRating
            {
                UserId = userId,
                TripId = tripId,
                Rating = ratingDto.Rating,
                Comment = ratingDto.Comment
            };
        }

        private async Task SaveRatingAsync(TripRating tripRating)
        {
            _context.TripRatings.Add(tripRating);
            await _context.SaveChangesAsync();
        }
    }
}
