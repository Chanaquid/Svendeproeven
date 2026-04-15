namespace backend.Common
{
    public class CallerContext
    {
        public string UserId { get; set; }
        public bool IsAdmin { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }

    }
}
