namespace UserAccessMgt.Application.DTOs.Location;

public class DivisionDto
{
    public int DivisionId { get; set; }
    public string DivisionNameEN { get; set; } = string.Empty;
    public string DivisionNameBN { get; set; } = string.Empty;
}

public class DistrictDto
{
    public int DistrictId { get; set; }
    public string DistrictNameEN { get; set; } = string.Empty;
    public string DistrictNameBN { get; set; } = string.Empty;
    public int DivisionId { get; set; }
}

public class ThanaDto
{
    public int ThanaId { get; set; }
    public string ThanaNameEN { get; set; } = string.Empty;
    public string ThanaNameBN { get; set; } = string.Empty;
    public int DistrictId { get; set; }
}
