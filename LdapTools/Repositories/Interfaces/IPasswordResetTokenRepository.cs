using LdapTools.Models;

namespace LdapTools.Repositories.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task SavePasswordResetTokenAsync(string username, string email, string hashedToken, string fortigateLogin, DateTime expirationTime);
        Task<PasswordResetToken> GetPasswordResetTokenAsync(string hashedToken);
        Task RemovePasswordResetTokenAsync(int tokenId);
    }
}
