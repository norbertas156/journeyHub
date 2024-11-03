using JourneyHub.Common.Models.Domain;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Models.Dtos.Responses;
using System.Threading.Tasks;

namespace JourneyHub.Api.Services.Interfaces
{
    public interface ITripServices
    {
        Task<Trip> CreateTripAsync(PostTripRequestDto tripDto, string userId);
        Task<Trip> GetTripByIdAsync(int id);
        Task<bool> DeleteTripAsync(int id, string userId);
        Task<AreaInfo> getAreaByCoordinatesAsync(MapPoint mapPoint);
        Task<(IEnumerable<GetTripsResponseDto>, int)> GetTripsPagedAsync(int pageNumber, int pageSize);
        Task DeleteAllTripsByUserIdAsync(string userId);
        Task<(IEnumerable<GetTripsResponseDto>, int)> GetTripsByUserIdAsync(string userId, int pageNumber, int pageSize);
    }
}