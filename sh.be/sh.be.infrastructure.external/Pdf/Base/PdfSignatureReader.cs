using sh.be.infrastructure.external.Pdf.Models;
using sh.be.shared.Constants.DigitalSignature;
using System.Formats.Asn1;
using System.Text;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.Pdf.Base
{
    public static class PdfSignatureReader
    {
        public static bool HasSignature(byte[] bytes)
        {
            return bytes.Length > 0 && Regex.IsMatch(Encoding.Latin1.GetString(bytes), PdfSigningConst.SIGNATURE_MARKER_PATTERN);
        }

        public static PdfSignature? ReadLast(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return null;
            }

            var text = Encoding.Latin1.GetString(bytes);
            var byteRange = Regex.Matches(text, PdfSigningConst.BYTE_RANGE_PATTERN).LastOrDefault();
            var contents = Regex.Matches(text, PdfSigningConst.CONTENTS_PATTERN).LastOrDefault();
            if (byteRange == null || contents == null)
            {
                return null;
            }

            var firstOffset = int.Parse(byteRange.Groups[1].Value);
            var firstLength = int.Parse(byteRange.Groups[2].Value);
            var secondOffset = int.Parse(byteRange.Groups[3].Value);
            var secondLength = int.Parse(byteRange.Groups[4].Value);
            if (firstOffset < 0 || firstLength < 0 || secondLength < 0 || secondOffset < firstOffset + firstLength || secondOffset + secondLength > bytes.Length)
            {
                return null;
            }

            var signedContent = new byte[firstLength + secondLength];
            Array.Copy(bytes, firstOffset, signedContent, 0, firstLength);
            Array.Copy(bytes, secondOffset, signedContent, firstLength, secondLength);
            var hex = contents.Groups[1];
            return new PdfSignature
            {
                SignedContent = signedContent,
                Cms = TrimDer(hex.Value),
                CoversWholeFile = firstOffset == 0
                    && firstLength == hex.Index - 1
                    && secondOffset == hex.Index + hex.Length + 1
                    && secondOffset + secondLength == bytes.Length,
            };
        }

        private static byte[] TrimDer(string hex)
        {
            var raw = Convert.FromHexString(hex.Length % 2 == 0 ? hex : hex[..^1]);
            try
            {
                AsnDecoder.ReadEncodedValue(raw, AsnEncodingRules.DER, out _, out _, out var length);
                return raw[..length];
            }
            catch (AsnContentException)
            {
                return raw;
            }
        }
    }
}
