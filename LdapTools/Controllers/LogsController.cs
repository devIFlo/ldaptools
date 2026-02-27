using LdapTools.Services.Interfaces;
using LdapTools.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LdapTools.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly ILogService _logService;

        public LogsController(ILogService logService)
        {
            _logService = logService;
        }

        public IActionResult Index(
            DateTime? startDate,
            DateTime? endDate,
            int logLevel = 0,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 10)
        {
            var logsQuery = _logService.GetLogs().AsQueryable();

            if (startDate.HasValue)
                logsQuery = logsQuery.Where(log => log.Timestamp.HasValue && log.Timestamp.Value.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                logsQuery = logsQuery.Where(log => log.Timestamp.HasValue && log.Timestamp.Value.Date <= endDate.Value.Date);

            if (logLevel != 0)
                logsQuery = logsQuery.Where(log => log.Level == logLevel);

            if (!string.IsNullOrEmpty(searchTerm))
                logsQuery = logsQuery.Where(log => !string.IsNullOrEmpty(log.Message) &&
                                                   log.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

            logsQuery = logsQuery.OrderByDescending(log => log.Timestamp);

            var totalItems = logsQuery.Count();

            var logs = logsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new LogEntryViewModel
                {
                    Timestamp = log.Timestamp,
                    Level = log.Level,
                    Message = log.Message,
                    Exception = log.Exception
                })
                .ToList();

            var logViewModel = new LogViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                LogLevel = logLevel,
                SearchTerm = searchTerm,
                LogEntries = logs,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(logViewModel);
        }
    }
}
