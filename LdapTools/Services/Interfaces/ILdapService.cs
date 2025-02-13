using LdapTools.Models;

namespace LdapTools.Services.Interfaces
{
    public interface ILdapService
    {
        Task<bool> IsAuthenticated(string username, string password);
        Task<string?> GetEmailByUsernameAsync(string userName);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
        Task<LdapUser?> GetUserByEmailAsync(string email);
        Task<List<LdapUser>> GetLdapUsers();
        Task<List<LdapUser>> GetLdapUsers(List<string> usernames);
        Task ImportLdapUsers(List<LdapUser> ldapUsers);
    }
}
