using WorldwideFreight.Models;

namespace WorldwideFreight.Dtos.CommodityDtos;

public class CreateCommodityRequest
{
    public string Name { get; set; }
    public string Code { get; set; }
}