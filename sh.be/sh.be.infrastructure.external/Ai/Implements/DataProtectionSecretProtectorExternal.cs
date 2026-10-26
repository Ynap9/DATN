using Microsoft.AspNetCore.DataProtection;
using sh.be.applications.Base.Interfaces.External.Ai;
using System.Security.Cryptography;

namespace sh.be.infrastructure.external.Ai.Implements
{
    public class DataProtectionSecretProtectorExternal : ISecretProtectorExternal
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public DataProtectionSecretProtectorExternal(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtectionProvider = dataProtectionProvider;
        }

        public string Protect(string plainText, string purpose)
        {
            return _dataProtectionProvider.CreateProtector(purpose).Protect(plainText);
        }

        public string? TryUnprotect(string cipherText, string purpose)
        {
            try
            {
                return _dataProtectionProvider.CreateProtector(purpose).Unprotect(cipherText);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return null;
            }
        }
    }
}
