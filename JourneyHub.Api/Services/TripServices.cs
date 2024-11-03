using AutoMapper;
using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Constants;
using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Models.Dtos.Responses;
using JourneyHub.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using JourneyHub.Common.Exceptions;

namespace JourneyHub.Api.Services
{
    public class TripServices : ITripServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public TripServices(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Trip> CreateTripAsync(PostTripRequestDto tripDto, string userId)
        {
            Trip trip = _mapper.Map<Trip>(tripDto);
            trip.UserId = userId;
            trip.Area = await getAreaByCoordinatesAsync(tripDto.MapPoints[0]);

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return trip;
        }


        public async Task<(IEnumerable<GetTripsResponseDto>, int)> GetTripsByUserIdAsync(string userId, int pageNumber,
            int pageSize)
        {
            var query = _context.Trips.Where(t => t.UserId == userId);

            var totalTrips = await query.CountAsync();
            var trips = await query.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .Select(trip => new GetTripsResponseDto
                {
                    Id = trip.Id,
                    RouteName = trip.RouteName,
                    RouteDescription = trip.RouteDescription,
                    Area = trip.Area,
                    Distance = trip.Distance,
                    Duration = trip.Duration,
                    Images = trip.Images,
                })
                .ToListAsync();

            return (trips, totalTrips);
        }

        public async Task<(IEnumerable<GetTripsResponseDto>, int)> GetTripsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Trips.Where(trip => !trip.IsPrivate);

            var totalTrips = await query.CountAsync();
            var trips = await query.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .Select(trip => new GetTripsResponseDto
                {
                    Id = trip.Id,
                    RouteName = trip.RouteName,
                    RouteDescription = trip.RouteDescription,
                    Area = trip.Area,
                    Distance = trip.Distance,
                    Duration = trip.Duration,
                    Images = trip.Images,
                })
                .ToListAsync();
            return (trips, totalTrips);
        }


        public async Task<Trip> GetTripByIdAsync(int id)
        {
            return await _context.Trips.FindAsync(id);
        }

        public async Task<bool> DeleteTripAsync(int id, string userId)
        {
            var trip = await _context.Trips.FindAsync(id);

            if (trip == null)
            {
                return false;
            }

            if (trip.UserId != userId)
            {
                throw new UnauthorizedException(ErrorMessages.Unauthorized_Trip_Deletion, 403);
            }

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task DeleteAllTripsByUserIdAsync(string userId)
        {
            var userTrips = _context.Trips.Where(t => t.UserId == userId).ToList();
            _context.Trips.RemoveRange(userTrips);
            await _context.SaveChangesAsync();
        }

        public async Task<AreaInfo> getAreaByCoordinatesAsync(MapPoint MapPoint)
        {
            string _address = "https://nominatim.openstreetmap.org/reverse?lat=" + MapPoint.Lat.ToString() + "&lon=" +
                              MapPoint.Lng.ToString() + "&format=json";

            var client = new HttpClient();

            var productValue = new ProductInfoHeaderValue("ScraperBot", "1.0");
            var commentValue = new ProductInfoHeaderValue("(+http://www.example.com/ScraperBot.html)");

            client.DefaultRequestHeaders.UserAgent.Add(productValue);
            client.DefaultRequestHeaders.UserAgent.Add(commentValue);

            client.BaseAddress = new Uri(_address);
            HttpResponseMessage response = await client.GetAsync(new Uri(_address));
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            string patterncountry = @"""\bcountry\b"":""([^""]+)""";
            string patterncity = @"""\bcity\b"":""([^""]+)""";

            Match matchCountry = Regex.Match(result, patterncountry);
            string countryName = matchCountry.Groups[1].Value;

            Match matchCity = Regex.Match(result, patterncity);
            string cityName = matchCity.Groups[1].Value;


            return new AreaInfo { Country = countryName, City = cityName };
        }
    }
}