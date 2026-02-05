using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorldwideFreight.Data;
using WorldwideFreight.Dtos.ApiDtos;
using WorldwideFreight.Dtos.UserDtos;
using WorldwideFreight.Models;

namespace WorldwideFreight.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class UserController : BaseController
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _config;
        private readonly ILogger<UserController> _logger;
        
        public UserController(AppDbContext dbContext, IConfiguration config, ILogger<UserController> logger)
        {
            _dbContext = dbContext;
            _config = config;
            _logger = logger;
        }

        [HttpPost("signUp")]
        public async Task<ActionResult<ApiResponseDto<UserSignUpResponseDto>>> SignUp([FromBody] UserSignUpRequestDto signUpRequest)
        {
            try
            {
                if (signUpRequest == null || string.IsNullOrEmpty(signUpRequest.FullName) ||
                    string.IsNullOrEmpty(signUpRequest.Email) || string.IsNullOrEmpty(signUpRequest.Password))
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid sign-up request data."
                    });
                }
                
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == signUpRequest.Email);

                if (existingUser != null)
                {
                    return Conflict(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "A user with this email already exists."
                    });
                }
                
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(signUpRequest.Password);
                
                if (string.IsNullOrEmpty(hashedPassword) || hashedPassword.Length < 8)
                {
                    return StatusCode(500, new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred during sign-up."
                    });
                }

                var newUser = new User
                {
                    FullName = signUpRequest.FullName,
                    Email = signUpRequest.Email,
                    Password = hashedPassword
                };
                
                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                var userResponse = new UserSignUpResponseDto
                {
                    Id = newUser.Id,
                    FullName = newUser.FullName,
                    Email = newUser.Email
                };
            
                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "User signed up successfully.",
                    Data = userResponse
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _logger.LogError(ex, "UserController/signUp: An error occurred during sign-up.");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during sign-up.",
                });
            }
        }

        [HttpPost("signIn")]
        public async Task<IActionResult> SignIn([FromBody] UserSignInRequestDto signInRequest)
        {
            try
            {
                if (signInRequest == null || string.IsNullOrEmpty(signInRequest.Email) ||
                    string.IsNullOrEmpty(signInRequest.Password))
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid sign-in request data."
                    });
                }
                
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == signInRequest.Email);
                
                var isPasswordCorrect = BCrypt.Net.BCrypt.Verify(signInRequest.Password, user.Password);
                
                if (user == null || !isPasswordCorrect)
                {
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    });
                }

                var userData = new UserDto
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl
                };
                var accessToken = GenerateJwtToken(user);
                var sendData = new UserSignInResponseDto
                {
                    User = userData,
                    AccessToken = accessToken
                };
                
                return Ok(new ApiResponseDto<UserSignInResponseDto>
                {
                    Success = true,
                    Message = "User signed in successfully.",
                    Data = sendData 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _logger.LogError(ex, "UserController/signIn: An error occurred during sign-in.");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during sign-in.",
                });
            }
        }

        private string? GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName)
            };
            
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}

