namespace LdapTools.ViewModels
{
    public class LogEntryViewModel
    {
        public DateTime? Timestamp { get; set; }
        public int Level { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}
