using LdapTools.Data;
using LdapTools.Models;
using LdapTools.Services.Interfaces;

namespace LdapTools.Services.Implementations
{
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _context;

        public LogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<LogEntry> GetLogs()
        {
            return _context.Logs.ToList();
        }
    }
}
