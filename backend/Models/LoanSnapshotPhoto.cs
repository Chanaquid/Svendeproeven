using backend.Models;

public class LoanSnapshotPhoto
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public Loan Loan { get; set; } = null!;
    public string PhotoUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0; //preserved from original ItemPhoto order
    public DateTime SnapshotTakenAt { get; set; } = DateTime.UtcNow; //when loan became Active
}