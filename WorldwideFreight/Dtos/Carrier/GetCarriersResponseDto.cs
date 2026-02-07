using WorldwideFreight.Dtos.CommodityDtos;

namespace WorldwideFreight.Dtos.Carrier;

public class GetCarriersResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public CreateCommodityResponse Commodity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } 
}