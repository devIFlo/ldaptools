using LdapTools.Models;

namespace LdapTools.ViewModels
{
    public class LogViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? LogLevel { get; set; }
        public List<LogEntry>? LogEntries { get; set; }
    }
}
