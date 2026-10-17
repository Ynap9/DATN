using sh.be.infrastructure.external.Pdf.Models;
using sh.be.shared.Constants.DigitalSignature;
using System.Globalization;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.Pdf.Base
{
    public static class HexColorReader
    {
        public static RgbColor? Read(string? hex)
        {
            var value = hex?.Trim() ?? String.Empty;
            if (!Regex.IsMatch(value, TemplateConst.COLOR_PATTERN))
            {
                return null;
            }

            return new RgbColor
            {
                Red = int.Parse(value.Substring(1, 2), NumberStyles.HexNumber) / 255d,
                Green = int.Parse(value.Substring(3, 2), NumberStyles.HexNumber) / 255d,
                Blue = int.Parse(value.Substring(5, 2), NumberStyles.HexNumber) / 255d,
            };
        }
    }
}
