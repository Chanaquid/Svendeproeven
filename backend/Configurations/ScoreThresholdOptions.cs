namespace backend.Configuration
{
    public class ScoreThresholdOptions
    {
        public const string SectionName = "ScoreThresholds";

        //Users below this score cannot borrow at all
        public int BlockedBelow { get; set; } = 20;

        //Users with a score less than or equal to this (but above blocked) require admin approval
        public int AdminApprovalBelowOrEqual { get; set; } = 50;
    }
}