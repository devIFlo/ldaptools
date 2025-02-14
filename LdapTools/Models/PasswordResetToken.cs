namespace LdapTools.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string HashedToken { get; set; }
        public required string FortigateLogin { get; set; }
        public DateTime ExpirationTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}