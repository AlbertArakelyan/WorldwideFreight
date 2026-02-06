using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldwideFreight.Data;
using WorldwideFreight.Dtos.ApiDtos;
using WorldwideFreight.Dtos.CommodityDtos;
using WorldwideFreight.Models;

namespace WorldwideFreight.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class CommodityController : BaseController
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly ILogger<CommodityController> _logger;
    
    public CommodityController(AppDbContext dbContext, IConfiguration config, ILogger<CommodityController> logger)
    {
        _dbContext = dbContext;
        _config = config;
        _logger = logger;
    }
    
    [HttpPost]
    [Authorize()]
    public async Task<ActionResult<ApiResponseDto<CreateCommodityResponse>>> CreateCommodity([FromBody] CreateCommodityRequest commodityRequest)
    {
        try
        {
            if (commodityRequest == null || string.IsNullOrEmpty(commodityRequest.Name) ||
                string.IsNullOrEmpty(commodityRequest.Code))
            {
                return BadRequest("Invalid commodity data.");
            }
            
            var existingCommodity = await _dbContext.Commodities
                .FirstOrDefaultAsync(c => c.Code == commodityRequest.Code || c.Name == commodityRequest.Name);

            if (existingCommodity != null)
            {
                return Conflict(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "A commodity with the same name or code already exists."
                });
            }
            
            var commodity = new Commodity
            {
                Name = commodityRequest.Name,
                Code = commodityRequest.Code
            };
            
            _dbContext.Commodities.Add(commodity);
            await _dbContext.SaveChangesAsync();
            
            var sendData = new CreateCommodityResponse
            {
                Id = commodity.Id,
                Name = commodity.Name,
                Code = commodity.Code
            };

            return Ok(new ApiResponseDto<CreateCommodityResponse>
            {
                Success = true,
                Message = "Commodity created successfully.",
                Data = sendData
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _logger.LogError(ex, "Error occurred while creating commodity.");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
}