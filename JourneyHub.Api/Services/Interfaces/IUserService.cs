using JourneyHub.Common.Models.Dtos.Requests;
using Microsoft.AspNetCore.Identity;

namespace JourneyHub.Api.Services.Interfaces
{
    public interface IUserService
    {
        Task<IdentityUser> GetUserByIdAsync(string userId);
        Task<IdentityUser> UpdateUserAsync(IdentityUser user, UserUpdateRequestDto userUpdateDto);
        Task<bool> DeleteCurrentUserAsync(string userId);
        Task<bool> VerifyUserPasswordAsync(string userId, string password);
    }
}