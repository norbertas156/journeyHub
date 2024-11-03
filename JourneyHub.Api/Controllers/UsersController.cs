using JourneyHub.Api.Services.Interfaces;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Models.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JourneyHub.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails()
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new GenericResponse<string>("User ID is missing or invalid."));

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new GenericResponse<string>("User not found."));

            var userInfo = new GetUserInfoDto
            {
                UserName = user.UserName,
                Email = user.Email,
                UserId = userId
            };

            return Ok(new GenericResponse<GetUserInfoDto>(userInfo));
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveCurrentUser()
        {
            var userId = GetUserIdFromClaims();
            var result = await _userService.DeleteCurrentUserAsync(userId);

            if (!result)
                return NotFound(new GenericResponse<string>("User not found or could not be deleted."));

            return Ok(new GenericResponse<string>("User successfully deleted."));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserDetails([FromBody] UserUpdateRequestDto updateUserDto)
        {
            var userId = GetUserIdFromClaims();
            var user = await EnsureUserExists(userId);
            if (user == null) return NotFound(new GenericResponse<string>("User not found."));

            var updatedUser = await _userService.UpdateUserAsync(user, updateUserDto);
            var userInfo = new GetUserInfoDto
            {
                UserName = updatedUser.UserName,
                Email = updatedUser.Email
            };

            return Ok(new GenericResponse<GetUserInfoDto>(userInfo));
        }

        [HttpPost("verify-password")]
        public async Task<IActionResult> VerifyPassword([FromBody] PasswordVerificationRequestDto passwordDto)
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new GenericResponse<string>("User ID is missing or invalid."));

            var isPasswordCorrect = await _userService.VerifyUserPasswordAsync(userId, passwordDto.Password);
            return Ok(new GenericResponse<bool>(isPasswordCorrect));
        }

        // Helper Methods
        private string GetUserIdFromClaims()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task<IdentityUser?> EnsureUserExists(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user ?? null;
        }
    }
}
