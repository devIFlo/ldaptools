namespace LdapTools.Services.Interfaces
{
    public interface IEmailSender
    {
        Task SendPasswordResetEmailAsync(string email, string token);
    }
}
