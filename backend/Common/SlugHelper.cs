namespace backend.Common
{
    public static class SlugHelper
    {
        public static string ToSlug(this string name)
        {
            return name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("&", "and")
                //remove all non-alphanumeric characters except hyphens
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .Aggregate("", (s, c) => s + c)
                .Trim('-');
        }
    }
}
