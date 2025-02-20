using LdapTools.Models;

namespace LdapTools.Services.Interfaces
{
    public interface ILogService
    {
        List<LogEntry> GetLogs();
    }
}
