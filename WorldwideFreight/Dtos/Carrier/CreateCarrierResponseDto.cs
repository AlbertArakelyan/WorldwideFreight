namespace WorldwideFreight.Dtos.Carrier;

public class CreateCarrierResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public int CommodityId { get; set; }
}