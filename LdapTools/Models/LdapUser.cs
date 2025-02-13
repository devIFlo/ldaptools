namespace LdapTools.Models
{
    public class LdapUser
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DistinguishedName { get; set; }
        public string? FullName => $"{FirstName} {LastName}";
        public string? DisplayUser => $"{Username} ({FirstName} {LastName})";
    }
}
