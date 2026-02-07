using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldwideFreight.Data;
using WorldwideFreight.Dtos.ApiDtos;
using WorldwideFreight.Dtos.Carrier;
using WorldwideFreight.Models;

namespace WorldwideFreight.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class CarrierController : BaseController
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly ILogger<CarrierController> _logger;
    
    public CarrierController(AppDbContext dbContext, IConfiguration config, ILogger<CarrierController> logger)
    {
        _dbContext = dbContext;
        _config = config;
        _logger = logger;
    }
    
    [HttpPost]
    [Authorize()]
    public async Task<ActionResult<ApiResponseDto<CreateCarrierResponseDto>>> CreateCarrier([FromBody] CreateCarrierRequestDto carrierRequest)
    {
        try
        {
            if (carrierRequest == null || string.IsNullOrEmpty(carrierRequest.Name) ||
                string.IsNullOrEmpty(carrierRequest.LogoUrl) || carrierRequest.CommodityId <= 0)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid carrier data."
                });
            }
            
            var carrier = new Carrier
            {
                Name = carrierRequest.Name,
                LogoUrl = carrierRequest.LogoUrl,
                CommodityId = carrierRequest.CommodityId
            };
            
            _dbContext.Carriers.Add(carrier);
            await _dbContext.SaveChangesAsync();
            
            var sendData = new CreateCarrierResponseDto
            {
                Id = carrier.Id,
                Name = carrier.Name,
                LogoUrl = carrier.LogoUrl,
                CommodityId = carrier.CommodityId
            };
            
            return Ok(new ApiResponseDto<CreateCarrierResponseDto>
            {
                Success = true,
                Message = "Carrier created successfully.",
                Data = sendData
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _logger.LogError(ex, "Error creating carrier.");
            return StatusCode(500, "An error occurred while creating the carrier.");
        }
    }
}