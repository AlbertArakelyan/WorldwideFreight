using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    }
}

