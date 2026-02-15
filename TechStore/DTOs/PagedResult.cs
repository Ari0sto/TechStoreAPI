namespace TechStore.DTOs
{
    // <T> значит, что этот класс может хранить
    // список чего угодно
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new(); // Сами данные
        public int TotalItems { get; set; }         // Всего записей в БД (например, 55)
        public int CurrentPage { get; set; }        // Текущая страница (например, 1)
        public int PageSize { get; set; }           // Размер страницы (например, 10)

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
