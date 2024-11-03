using JourneyHub.Common.Constants;
using JourneyHub.Common.Exceptions;
using JourneyHub.Common.Models.Dtos.Requests;
using JourneyHub.Common.Models.Dtos.Responses;
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
            _jwtConfig = jwtConfig.Value;
            _userManager = userManager;
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto requestDto)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException(ErrorMessages.Invalid_Payload);

            var existingUser = await _userManager.FindByEmailAsync(requestDto.Email);
            if (existingUser == null)
                throw new BadRequestException(ErrorMessages.Invalid_Email);

            var isCorrect = await _userManager.CheckPasswordAsync(existingUser, requestDto.Password);
            if (!isCorrect)
                throw new BadRequestException(ErrorMessages.Invalid_Password);

            var (token, expiration) = GenerateJwtToken(existingUser);
            return Ok(new
            {
                Token = token, Expiration = expiration,
                Email = existingUser.Email, Name = existingUser.UserName,
                UserId = existingUser.Id
            });
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException(ErrorMessages.Invalid_Payload);

            var userExistsByUsername = await _userManager.FindByNameAsync(requestDto.Name);
            if (userExistsByUsername != null)
                throw new BadRequestException(ErrorMessages.Username_Taken);

            var userExistsByEmail = await _userManager.FindByEmailAsync(requestDto.Email);
            if (userExistsByEmail != null)
                throw new BadRequestException(ErrorMessages.Email_Exists);

            if (requestDto.Password != requestDto.ConfirmPassword)
                throw new BadRequestException(ErrorMessages.Passwords_Do_Not_Match);

            var newUser = new IdentityUser
            {
                UserName = requestDto.Name,
                Email = requestDto.Email,
            };

            var isCreated = await _userManager.CreateAsync(newUser, requestDto.Password);

            if (!isCreated.Succeeded)
                throw new BadRequestException(string.Join("; ", isCreated.Errors.Select(e => e.Description)));

            var (token, expiration) = GenerateJwtToken(newUser);
            return Ok(new
            {
                Token = token, Expiration = expiration,
                Email = newUser.Email, Name = requestDto.Name,
                UserId = newUser.Id,
            });
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
            var tokenString = jwtTokenHandler.WriteToken(token);

            return (Token: tokenString, Expiration: expiration);
        }
    }
}