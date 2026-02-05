using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WorldwideFreight.Dtos.UserDtos;

namespace WorldwideFreight.Controllers
{
    public class BaseController : ControllerBase
    {
        protected UserClaimDto GetUserData()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userEmailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            int? userId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            return new UserClaimDto
            {
                Id = userId,
                Email = userEmailClaim
            };
        }
    }
}

