namespace App.Core.Base
{
    public class BasePaginatedList<T>
    {
        public IReadOnlyCollection<T> Items { get; private set; }

        public int TotalItems { get; private set; }

        public int PageIndex { get; private set; }

        public int TotalPages { get; private set; }

        public int PageSize { get; private set; }

        public BasePaginatedList(
            IReadOnlyCollection<T> items, 
            int totalItems, 
            int pageIndex,
            int pageSize)
        {
            Items = items;
            TotalItems = totalItems;
            PageIndex = pageIndex;
            PageSize = pageSize > 0 ? pageSize : 10;
            TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)PageSize);
        }

        public bool HasPreviousPage => PageIndex > 0;

        public bool HasNextPage => PageIndex < TotalPages - 1;

        public int CurrentPageNumber => PageIndex + 1;
    }
}
