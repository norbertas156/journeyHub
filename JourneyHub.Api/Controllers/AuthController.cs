using JourneyHub.Common.Constants;
using JourneyHub.Common.Exceptions;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JourneyHub.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;

        public AuthController(UserManager<IdentityUser> userManager, IOptions<JwtConfig> jwtConfig)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.Value;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto requestDto)
        {
            ValidateModel();

            var user = await FindUserByEmailAsync(requestDto.Email);
            await VerifyPasswordAsync(user, requestDto.Password);

            var tokenResult = GenerateJwtToken(user);
            return Ok(CreateAuthResponse(user, tokenResult));
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto)
        {
            ValidateModel();

            await ValidateUserDoesNotExist(requestDto);
            VerifyPasswordsMatch(requestDto.Password, requestDto.ConfirmPassword);

            var newUser = new IdentityUser
            {
                UserName = requestDto.Name,
                Email = requestDto.Email,
            };

            await CreateUserAsync(newUser, requestDto.Password);

            var tokenResult = GenerateJwtToken(newUser);
            return Ok(CreateAuthResponse(newUser, tokenResult));
        }

        // Helper Methods
        private void ValidateModel()
        {
            if (!ModelState.IsValid)
                throw new BadRequestException(ErrorMessages.Invalid_Payload);
        }

        private async Task<IdentityUser> FindUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new BadRequestException(ErrorMessages.Invalid_Email);

            return user;
        }

        private async Task VerifyPasswordAsync(IdentityUser user, string password)
        {
            var isCorrect = await _userManager.CheckPasswordAsync(user, password);
            if (!isCorrect)
                throw new BadRequestException(ErrorMessages.Invalid_Password);
        }

        private async Task ValidateUserDoesNotExist(UserRegistrationRequestDto requestDto)
        {
            if (await _userManager.FindByNameAsync(requestDto.Name) != null)
                throw new BadRequestException(ErrorMessages.Username_Taken);

            if (await _userManager.FindByEmailAsync(requestDto.Email) != null)
                throw new BadRequestException(ErrorMessages.Email_Exists);
        }

        private void VerifyPasswordsMatch(string password, string confirmPassword)
        {
            if (password != confirmPassword)
                throw new BadRequestException(ErrorMessages.Passwords_Do_Not_Match);
        }

        private async Task CreateUserAsync(IdentityUser user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        private (string Token, DateTime Expiration) GenerateJwtToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiration = DateTime.UtcNow.AddHours(_jwtConfig.ExpirationInHours);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiration,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return (jwtTokenHandler.WriteToken(token), expiration);
        }

        private object CreateAuthResponse(IdentityUser user, (string Token, DateTime Expiration) tokenResult)
        {
            return new
            {
                Token = tokenResult.Token,
                Expiration = tokenResult.Expiration,
                Email = user.Email,
                Name = user.UserName,
                UserId = user.Id
            };
        }
    }
}
