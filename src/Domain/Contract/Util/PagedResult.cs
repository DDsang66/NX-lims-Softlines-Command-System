namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }

        public PagedResult() { }

        public PagedResult(List<T> items, int totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }

        public static PagedResult<T> Empty(int totalCount = 0)
            => new(new List<T>(), totalCount);
    }
}
