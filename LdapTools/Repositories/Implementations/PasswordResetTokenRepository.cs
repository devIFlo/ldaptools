using LdapTools.Data;
using LdapTools.Models;
using LdapTools.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LdapTools.Repositories.Implementations
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public PasswordResetTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SavePasswordResetTokenAsync(string username, string email, string hashedToken, string fortigateLogin, DateTime expirationTime)
        {
            var token = new PasswordResetToken
            {
                Username = username,
                Email = email,
                HashedToken = hashedToken,
                FortigateLogin = fortigateLogin,
                ExpirationTime = expirationTime,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetToken> GetPasswordResetTokenAsync(string hashedToken)
        {
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.HashedToken == hashedToken && t.ExpirationTime > DateTime.UtcNow);
        }

        public async Task RemovePasswordResetTokenAsync(int tokenId)
        {
            var token = await _context.PasswordResetTokens.FindAsync(tokenId);
            if (token != null)
            {
                _context.PasswordResetTokens.Remove(token);
                await _context.SaveChangesAsync();
            }
        }
    }
}