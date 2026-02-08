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
    public async Task<ActionResult<ApiResponseDto<CreateCarrierResponseDto>>> CreateCarrier(
        [FromBody] CreateCarrierRequestDto carrierRequest)
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
    
    [HttpPost("bulk")]
    [Authorize()]
    public async Task<ActionResult<ApiResponseDto<object>>> BulkUploadCarriersFromCsv(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "No file uploaded."
                });
            }

            using var stream = new StreamReader(file.OpenReadStream());
            var carriersToAdd = new List<Carrier>();

            while (!stream.EndOfStream)
            {
                var line = await stream.ReadLineAsync();
                var values = line.Split(',');

                if (
                    values.Length != 3 ||
                    string.IsNullOrEmpty(values[0]) ||
                    string.IsNullOrEmpty(values[1]) ||
                    !int.TryParse(values[2], out int commodityId)
                )
                {
                    continue; // Skip invalid lines
                }
                
                var commodity = await _dbContext.Commodities.FindAsync(commodityId);
                
                if (commodity == null)
                {
                    continue; // Skip if commodity does not exist
                }

                carriersToAdd.Add(new Carrier
                {
                    Name = values[0],
                    LogoUrl = values[1],
                    CommodityId = commodityId
                });
            }

            if (carriersToAdd.Count == 0)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "No valid carrier data found in the file."
                });
            }

            _dbContext.Carriers.AddRange(carriersToAdd);
            await _dbContext.SaveChangesAsync();

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = $"{carriersToAdd.Count} carriers uploaded successfully."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _logger.LogError(ex, "Error uploading carriers from CSV.");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
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
                    Commodity = c.Commodity == null
                        ? null
                        : new CreateCommodityResponse
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

    [HttpGet("{id}")]
    [Authorize()]
    public async Task<IActionResult> GetCarrierById(int id)
    {
        try
        {
            var carrier = await _dbContext.Carriers
                .Include(c => c.Commodity)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (carrier == null)
            {
                return NotFound(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Carrier not found."
                });
            }

            var carrierData = new GetCarriersResponseDto
            {
                Id = carrier.Id,
                Name = carrier.Name,
                LogoUrl = carrier.LogoUrl,
                Commodity = carrier.Commodity == null
                    ? null
                    : new CreateCommodityResponse
                    {
                        Id = carrier.Commodity.Id,
                        Name = carrier.Commodity.Name,
                        Code = carrier.Commodity.Code
                    },
                CreatedAt = carrier.CreatedAt,
                UpdatedAt = carrier.UpdatedAt
            };

            return Ok(new ApiResponseDto<GetCarriersResponseDto>
            {
                Success = true,
                Message = "Carrier retrieved successfully.",
                Data = carrierData
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _logger.LogError(ex, "Error retrieving carrier by ID.");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize()]
    public async Task<ActionResult<ApiResponseDto<UpdateCarrierResponseDto>>> UpdateCarrier(int id, [FromBody] UpdateCarrierRequestDto updateRequest)
    {
        try
        {
            if (updateRequest == null || string.IsNullOrEmpty(updateRequest.Name) ||
                string.IsNullOrEmpty(updateRequest.LogoUrl) || updateRequest.CommodityId <= 0)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid carrier data."
                });
            }
            
            var carrier = await _dbContext.Carriers.FindAsync(id);
            if (carrier == null)
            {
                return NotFound(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Carrier not found."
                });
            }

            carrier.Name = updateRequest.Name;
            carrier.LogoUrl = updateRequest.LogoUrl;
            carrier.CommodityId = updateRequest.CommodityId;

            _dbContext.Carriers.Update(carrier);
            await _dbContext.SaveChangesAsync();
            
            var sendData = new UpdateCarrierResponseDto
            {
                Id = carrier.Id,
                Name = carrier.Name,
                LogoUrl = carrier.LogoUrl,
                CommodityId = carrier.CommodityId
            };

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Carrier updated successfully.",
                Data = sendData
            });
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _logger.LogError(ex, "Error updating carrier.");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize()]
    public async Task<ActionResult<ApiResponseDto<object>>> DeleteCarrier(int id)
    {
        try
        {
            var carrier = await _dbContext.Carriers.FindAsync(id);
            if (carrier == null)
            {
                return NotFound(new ApiResponseDto<DeleteResponseDto>
                {
                    Success = false,
                    Message = "Carrier not found.",
                    Data =
                    {
                        Id = id,
                        IsDeleted = false
                    }
                });
            }
            
            _dbContext.Carriers.Remove(carrier);
            await _dbContext.SaveChangesAsync();
            
            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Carrier deleted successfully.",
                Data = new DeleteResponseDto
                {
                    Id = carrier.Id,
                    IsDeleted = true
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _logger.LogError(ex, "Error deleting carrier.");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }
}