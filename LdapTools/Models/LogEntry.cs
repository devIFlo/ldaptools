namespace LdapTools.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string? Message { get; set; }
        public int? Level { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? Exception { get; set; }
        public string? Properties { get; set; }
    }
}
