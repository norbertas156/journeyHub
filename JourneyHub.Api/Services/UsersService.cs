using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Exceptions;
using JourneyHub.Common.Models.Dtos.Requests;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using JourneyHub.Common.Constants;

namespace JourneyHub.Api.Services
{
    public class UsersService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITripServices _tripServices;

        public UsersService(UserManager<IdentityUser> userManager, ITripServices tripServices)
        {
            _userManager = userManager;
            _tripServices = tripServices;
        }

        public async Task<IdentityUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<bool> DeleteCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            await _tripServices.DeleteAllTripsByUserIdAsync(userId);

            if (user == null)
                return false;

            var result = await _userManager.DeleteAsync(user);

            return result.Succeeded;
        }

        public async Task<IdentityUser> UpdateUserAsync(IdentityUser user, UserUpdateRequestDto userUpdateDto)
        {

            await UpdateEmailAsync(user, userUpdateDto.Email);
            await UpdateUsernameAsync(user, userUpdateDto.UserName);
            await UpdatePasswordAsync(user, userUpdateDto.NewPassword);

            return user;
        }

        private async Task UpdateEmailAsync(IdentityUser user, string newEmail)
        {
            const string emailPattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";

            if (!Regex.IsMatch(newEmail, emailPattern))
                throw new BadRequestException("Invalid email format.");

            user.Email = newEmail;
            await _userManager.UpdateAsync(user);
        }

        private async Task UpdateUsernameAsync(IdentityUser user, string newUsername)
        {
            await ValidateNewUsernameAsync(user, newUsername);

            user.UserName = newUsername;
            await UpdateUserAsync(user);
        }

        private async Task ValidateNewUsernameAsync(IdentityUser user, string newUsername)
        {
            var existingUserWithNewUsername = await _userManager.FindByNameAsync(newUsername);

            if (existingUserWithNewUsername != null && existingUserWithNewUsername.Id != user.Id)
            {
                throw new BadRequestException("Username already taken.");
            }
        }

        private async Task UpdateUserAsync(IdentityUser user)
        {
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "Failed to update user.";
                throw new BadRequestException(errorMessage);
            }
        }

        private async Task UpdatePasswordAsync(IdentityUser user, string newPassword)
        {
            string? token = await _userManager.GeneratePasswordResetTokenAsync(user);
            IdentityResult? result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.FirstOrDefault()?.Description);
        }

        public async Task<bool> VerifyUserPasswordAsync(string userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return await _userManager.CheckPasswordAsync(user, password);
        }
    }
}