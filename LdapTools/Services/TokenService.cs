using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;

namespace LdapTools.Services
{
    public class TokenService
    {
        public string GenerateToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return WebEncoders.Base64UrlEncode(tokenData);
        }

        public string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = sha256.ComputeHash(tokenBytes);
            return WebEncoders.Base64UrlEncode(hashBytes);
        }
    }
}
