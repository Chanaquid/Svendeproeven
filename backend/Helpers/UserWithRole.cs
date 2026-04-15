using backend.Models;

namespace backend.Helpers
{
    public class UserWithRole
    {
        public ApplicationUser User { get; set; } = null!;
        public string Role { get; set; } = string.Empty;
    }
}
