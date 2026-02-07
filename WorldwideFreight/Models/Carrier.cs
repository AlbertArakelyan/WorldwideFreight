using System.Text.Json.Serialization;

namespace WorldwideFreight.Models;

public class Carrier : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    
    // Foreign keys
    public int CommodityId { get; set; }
    
    // Navigation properties
    [JsonIgnore]
    public Commodity Commodity { get; set; }
}