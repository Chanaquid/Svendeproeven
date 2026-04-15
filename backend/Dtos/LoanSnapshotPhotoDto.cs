namespace backend.Dtos
{

    public class LoanSnapshotPhotoDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime SnapshotTakenAt { get; set; }
    }


    public class LoanSnapshotPhotoListDto
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    
}
