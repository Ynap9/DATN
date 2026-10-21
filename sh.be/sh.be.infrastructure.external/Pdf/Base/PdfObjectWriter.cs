using sh.be.infrastructure.external.Pdf.Models;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Collections;
using System.Globalization;
using System.Text;

namespace sh.be.infrastructure.external.Pdf.Base
{
    public static class PdfObjectWriter
    {
        private const string NameDelimiters = "()<>[]{}/%#";

        public static string Write(object? value)
        {
            var builder = new StringBuilder();
            WriteValue(builder, value);
            return builder.ToString();
        }

        public static void WriteValue(StringBuilder builder, object? value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    break;
                case PdfName name:
                    builder.Append('/');
                    foreach (var c in name.Value)
                    {
                        if (c is > (char)0x20 and < (char)0x7F && NameDelimiters.IndexOf(c) < 0)
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            builder.Append('#').Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
                        }
                    }
                    break;
                case bool flag:
                    builder.Append(flag ? "true" : "false");
                    break;
                case int number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    break;
                case long number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    break;
                case double number:
                    builder.Append(FormatNumber(number));
                    break;
                case PdfRef reference:
                    builder.Append(reference.Number.ToString(CultureInfo.InvariantCulture)).Append(" 0 R");
                    break;
                case PdfString text:
                    WriteString(builder, text.Value);
                    break;
                case PdfDict dict:
                    builder.Append("<<");
                    foreach (var item in dict.Items)
                    {
                        WriteValue(builder, new PdfName(item.Key));
                        builder.Append(' ');
                        WriteValue(builder, item.Value);
                    }
                    builder.Append(">>");
                    break;
                case IEnumerable list when value is not string:
                    builder.Append('[');
                    var first = true;
                    foreach (var item in list)
                    {
                        if (!first)
                        {
                            builder.Append(' ');
                        }
                        WriteValue(builder, item);
                        first = false;
                    }
                    builder.Append(']');
                    break;
                default:
                    throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }
        }

        public static void WriteString(StringBuilder builder, string value)
        {
            if (value.All(c => c is >= (char)0x20 and < (char)0x7F))
            {
                builder.Append('(');
                foreach (var c in value)
                {
                    if (c is '(' or ')' or '\\')
                    {
                        builder.Append('\\');
                    }
                    builder.Append(c);
                }
                builder.Append(')');
                return;
            }

            builder.Append("<FEFF").Append(Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(value))).Append('>');
        }

        public static string FormatNumber(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        public static string? AppendToArray(string dictRaw, string key, int objectNumber)
        {
            var marker = "/" + key;
            var at = -1;
            var from = 0;
            while (true)
            {
                var index = dictRaw.IndexOf(marker, from, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }

                var after = index + marker.Length;
                if (after >= dictRaw.Length || dictRaw[after] is ' ' or '\r' or '\n' or '\t' or '[' or '/' or '<')
                {
                    at = index;
                    break;
                }
                from = index + 1;
            }

            if (at < 0)
            {
                var close = dictRaw.LastIndexOf(">>", StringComparison.Ordinal);
                return close < 0 ? null : dictRaw[..close] + marker + "[" + objectNumber + " 0 R]" + dictRaw[close..];
            }

            var p = PdfRevisionReader.SkipWhiteSpace(dictRaw, at + marker.Length);
            if (p >= dictRaw.Length || dictRaw[p] != '[')
            {
                return null;
            }

            var depth = 0;
            var q = p;
            while (q < dictRaw.Length)
            {
                if (dictRaw[q] == '[')
                {
                    depth++;
                }
                else if (dictRaw[q] == ']' && --depth == 0)
                {
                    break;
                }
                q++;
            }

            return q >= dictRaw.Length ? null : dictRaw[..q] + " " + objectNumber + " 0 R" + dictRaw[q..];
        }
    }
}
