namespace LdapTools.ViewModels
{
    public class LogViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int LogLevel { get; set; }

        public string? SearchTerm { get; set; }

        public List<LogEntryViewModel> LogEntries { get; set; } = new();

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}