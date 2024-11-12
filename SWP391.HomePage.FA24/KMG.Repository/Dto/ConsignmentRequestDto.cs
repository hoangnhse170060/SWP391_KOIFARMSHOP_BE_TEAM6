public class ConsignmentRequestDto
{
    public int KoiTypeId { get; set; }
    public int KoiId { get; set; }
    public string ConsignmentType { get; set; }
    public DateTime ConsignmentDateTo { get; set; }
    public string? UserImage { get; set; }
    public string? ConsignmentTitle { get; set; }
    public string? ConsignmentDetail { get; set; }
}
