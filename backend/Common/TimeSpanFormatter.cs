namespace backend.Common
{
    public static class TimeSpanFormatter
    {
        public static string ToReadableString(TimeSpan duration)
        {
            var parts = new List<string>();

            if (duration.Days > 0) parts.Add($"{duration.Days}d");
            if (duration.Hours > 0) parts.Add($"{duration.Hours}h");
            if (duration.Minutes > 0) parts.Add($"{duration.Minutes}m");

            if (!parts.Any()) parts.Add("less than a minute");

            return string.Join(" ", parts);
        }
    }
}