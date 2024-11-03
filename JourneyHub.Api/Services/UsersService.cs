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
            if (user == null)
            {
                return false;
            }

            await _tripServices.DeleteAllTripsByUserIdAsync(userId);

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<IdentityUser> UpdateUserAsync(IdentityUser user, UserUpdateRequestDto userUpdateDto)
        {
            string emailPattern = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
            var mustUpdateCredentials = false;
            
            if (!string.IsNullOrEmpty(userUpdateDto.Email))
            {
                if (!Regex.IsMatch(userUpdateDto.Email, emailPattern))
                    throw new BadRequestException("Invalid email format.");

                user.Email = userUpdateDto.Email;
                mustUpdateCredentials = true;
            }

            if (!string.IsNullOrEmpty(userUpdateDto.UserName))
            {
                var existingUserWithNewUsername = await _userManager.FindByNameAsync(userUpdateDto.UserName);
                if (existingUserWithNewUsername != null && existingUserWithNewUsername.Id != user.Id)
                    throw new BadRequestException("Username already taken.");

                user.UserName = userUpdateDto.UserName;
                mustUpdateCredentials = true;
            }

            if (mustUpdateCredentials)
            {
                var isUserUpdated = await _userManager.UpdateAsync(user);
                if (!isUserUpdated.Succeeded)
                    throw new BadRequestException(isUserUpdated.Errors.FirstOrDefault()?.Description);
            }

            if (!string.IsNullOrEmpty(userUpdateDto.NewPassword))
            {
                if (userUpdateDto.NewPassword != userUpdateDto.ConfirmNewPassword)
                {
                    throw new BadRequestException(ErrorMessages.Passwords_Do_Not_Match);
                }
                
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var isPasswordUpdated = await _userManager.ResetPasswordAsync(user, token, userUpdateDto.NewPassword);
                
                if (!isPasswordUpdated.Succeeded)
                    throw new BadRequestException(isPasswordUpdated.Errors.FirstOrDefault()?.Description);
            }

            return user;
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