namespace backend.Dtos
{
    public class PagedRequest
    {
        private const int MaxPageSize = 100;

        private int _page = 1;
        public int Page
        {
            get => _page;
            set => _page = Math.Max(1, value);
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
        }

        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}