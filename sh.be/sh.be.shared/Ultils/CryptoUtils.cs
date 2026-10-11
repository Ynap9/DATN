using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.shared.Utils
{
    public static class CryptoUtils
    {
        private static readonly char[] UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] LowerChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] DigitChars = "0123456789".ToCharArray();
        private static readonly char[] SpecialChars = "!@#$%^&*()-_=+[]{};:,.<>?/".ToCharArray();
        private static readonly char[] AllChars =
            UpperChars.Concat(LowerChars).Concat(DigitChars).Concat(SpecialChars).ToArray();

        public static string GenerateRandomString(int length)
        {
            if (length < 4)
                throw new ArgumentException("Length must be at least 4 to fit all required character types.");

            var result = new StringBuilder();

            // Ensure each required type is included at least once
            result.Append(UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)]);
            result.Append(LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)]);
            result.Append(DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)]);
            result.Append(SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)]);

            // Fill the rest randomly
            for (int i = result.Length; i < length; i++)
            {
                result.Append(AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)]);
            }

            // Shuffle to avoid predictable order
            var chars = result.ToString().ToCharArray();
            RandomNumberGenerator.Shuffle<char>(chars);
            return new string(chars);
        }
    }
}
