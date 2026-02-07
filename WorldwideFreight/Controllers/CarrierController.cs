using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldwideFreight.Data;
using WorldwideFreight.Dtos.ApiDtos;
using WorldwideFreight.Dtos.Carrier;
using WorldwideFreight.Dtos.CommodityDtos;
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
            return StatusCode(500, Ok(new ApiResponseDto<List<Carrier>>
            {
                Success = true,
                Message = "Carriers retrieved successfully.",
            }));
        }
    }

    [HttpGet]
    [Authorize()]
    public async Task<IActionResult> GetCarriers()
    {
        try
        {
            var carriers = await _dbContext.Carriers
                .Include(c => c.Commodity)
                .Select(c => new GetCarriersResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    LogoUrl = c.LogoUrl,
                    Commodity = c.Commodity == null ? null : new CreateCommodityResponse
                    {
                        Id = c.Commodity.Id,
                        Name = c.Commodity.Name,
                        Code = c.Commodity.Code
                    },
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
            
            return Ok(new ApiResponseDto<List<GetCarriersResponseDto>>
            {
                Success = true,
                Message = "Carriers retrieved successfully.",
                Data = carriers
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _logger.LogError(ex, "Error retrieving carriers.");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }
}