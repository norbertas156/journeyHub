using AutoMapper;
using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Constants;
using JourneyHub.Common.Exceptions;
using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Models.Dtos.Responses;
using JourneyHub.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace JourneyHub.Api.Services
{
    public class TripServices : ITripServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;

        private const string PatternCountry = @"""\bcountry\b"":""([^""]+)""";
        private const string PatternCity = @"""\bcity\b"":""([^""]+)""";

        public TripServices(AppDbContext context, IMapper mapper, HttpClient httpClient)
        {
            _context = context;
            _mapper = mapper;
            _httpClient = ConfigureHttpClient(httpClient);
        }

        public async Task<Trip> CreateTripAsync(PostTripRequestDto tripDto, string userId)
        {
            if (tripDto.MapPoints == null || !tripDto.MapPoints.Any())
                throw new BadRequestException("MapPoints cannot be empty.");

            var trip = _mapper.Map<Trip>(tripDto);
            trip.UserId = userId;
            trip.Area = await GetAreaByCoordinatesAsync(tripDto.MapPoints[0]);

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return trip;
        }

        public async Task<(IEnumerable<GetTripsResponseDto>, int)> GetTripsByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            var tripsQuery = _context.Trips.Where(t => t.UserId == userId);
            var totalTrips = await tripsQuery.CountAsync();
            var trips = await PaginateTripsAsync(tripsQuery, pageNumber, pageSize);

            return (trips, totalTrips);
        }

        public async Task<(IEnumerable<GetTripsResponseDto>, int)> GetTripsPagedAsync(int pageNumber, int pageSize)
        {
            var tripsQuery = _context.Trips.Where(trip => !trip.IsPrivate);
            var totalTrips = await tripsQuery.CountAsync();
            var trips = await PaginateTripsAsync(tripsQuery, pageNumber, pageSize);

            return (trips, totalTrips);
        }

        public async Task<Trip> GetTripByIdAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);

            return trip;
        }

        public async Task<bool> DeleteTripAsync(int id, string userId)
        {
            var trip = await GetTripByIdAsync(id);
            if (trip.UserId != userId)
                throw new UnauthorizedException(ErrorMessages.Unauthorized_Trip_Deletion);

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAllTripsByUserIdAsync(string userId)
        {
            var userTrips = await _context.Trips.Where(t => t.UserId == userId).ToListAsync();
            _context.Trips.RemoveRange(userTrips);
            await _context.SaveChangesAsync();
        }

        private HttpClient ConfigureHttpClient(HttpClient client)
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ScraperBot", "1.0"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+http://www.example.com/ScraperBot.html)"));
            return client;
        }

        private async Task<IEnumerable<GetTripsResponseDto>> PaginateTripsAsync(IQueryable<Trip> query, int pageNumber, int pageSize)
        {
            var trips = await query.Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .AsNoTracking()
                                   .ToListAsync();
            return _mapper.Map<IEnumerable<GetTripsResponseDto>>(trips);
        }

        public async Task<AreaInfo> GetAreaByCoordinatesAsync(MapPoint mapPoint)
        {
            var url = BuildNominatimUrl(mapPoint);
            var responseContent = await SendHttpRequestAsync(url);
            return ParseAreaInfo(responseContent);
        }

        private string BuildNominatimUrl(MapPoint mapPoint)
        {
            return $"https://nominatim.openstreetmap.org/reverse?lat={mapPoint.Lat}&lon={mapPoint.Lng}&format=json";
        }

        private async Task<string> SendHttpRequestAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private AreaInfo ParseAreaInfo(string responseContent)
        {
            var countryName = ExtractMatch(responseContent, PatternCountry);
            var cityName = ExtractMatch(responseContent, PatternCity);

            return new AreaInfo
            {
                Country = countryName,
                City = cityName
            };
        }

        private string ExtractMatch(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
