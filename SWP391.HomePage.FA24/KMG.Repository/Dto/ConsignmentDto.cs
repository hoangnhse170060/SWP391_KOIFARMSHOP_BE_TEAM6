﻿namespace KMG.Repository.Dto
{
    public class ConsignmentDto
    {
        public int ConsignmentId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int KoiTypeId { get; set; }
        public int? KoiId { get; set; }
        public string? ConsignmentType { get; set; }
        public string? Status { get; set; }
        public decimal? ConsignmentPrice { get; set; }
        public DateTime? ConsignmentDateFrom { get; set; }
        public DateTime? ConsignmentDateTo { get; set; }
        public string? UserImage { get; set; }
        public string? ConsignmentTitle { get; set; } 
        public string? ConsignmentDetail { get; set; } 
        public KoiDto? Koi { get; set; }

        public decimal TakeCareFee { get; set; }


    }
}
