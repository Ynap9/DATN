using sh.be.infrastructure.external.Pdf.Models;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.Pdf.Base
{
    public static class PdfRevisionReader
    {
        public static PdfRevision Load(byte[] bytes)
        {
            if (bytes.Length < 32 || bytes[0] != '%' || bytes[1] != 'P')
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var tailStart = Math.Max(0, bytes.Length - 2048);
            var tail = Encoding.Latin1.GetString(bytes, tailStart, bytes.Length - tailStart);
            var startXrefs = Regex.Matches(tail, @"startxref\s+(\d+)");
            if (startXrefs.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var lastXref = long.Parse(startXrefs[^1].Groups[1].Value);
            if (lastXref <= 0 || lastXref >= bytes.Length)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var revision = new PdfRevision
            {
                Bytes = bytes,
                Text = Encoding.Latin1.GetString(bytes),
                LastXrefOffset = lastXref,
            };

            var visited = new HashSet<long>();
            long? offset = lastXref;
            string? firstTrailer = null;
            while (offset is long current && visited.Add(current))
            {
                if (current < 0 || current >= bytes.Length)
                {
                    break;
                }

                var probe = Encoding.Latin1.GetString(bytes, (int)current, Math.Min(20, bytes.Length - (int)current)).TrimStart();
                var isTable = probe.StartsWith("xref", StringComparison.Ordinal);
                var trailer = isTable ? ReadClassicTable(revision, current) : ReadXrefStream(revision, current);
                if (firstTrailer == null)
                {
                    firstTrailer = trailer;
                    revision.LastXrefIsStream = !isTable;
                }

                var prev = Regex.Match(trailer, @"/Prev\s+(\d+)");
                offset = prev.Success ? long.Parse(prev.Groups[1].Value) : null;
            }

            if (firstTrailer == null)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }
            if (Regex.IsMatch(firstTrailer, @"/Encrypt\s"))
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfEncrypted);
            }

            var root = Regex.Match(firstTrailer, @"/Root\s+(\d+)\s+\d+\s+R");
            var size = Regex.Match(firstTrailer, @"/Size\s+(\d+)");
            if (!root.Success || !size.Success)
            {
                throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfUnsupported);
            }

            var id = Regex.Match(firstTrailer, @"/ID\s*(\[[^\]]*\])");
            var info = Regex.Match(firstTrailer, @"/Info\s+(\d+\s+\d+\s+R)");
            revision.RootObjectNumber = int.Parse(root.Groups[1].Value);
            revision.Size = int.Parse(size.Groups[1].Value);
            revision.IdRaw = id.Success ? id.Groups[1].Value : null;
            revision.InfoRaw = info.Success ? info.Groups[1].Value : null;

            foreach (Match match in Regex.Matches(revision.Text, @"/DocMDP"))
            {
                var window = revision.Text.Substring(match.Index, Math.Min(400, revision.Text.Length - match.Index));
                var permission = Regex.Match(window, @"/P\s+(\d+)");
                if (permission.Success && permission.Groups[1].Value == "1")
                {
                    throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorPdfLocked);
                }
            }

            return revision;
        }

        public static string? GetObjectBody(PdfRevision revision, int number)
        {
            if (!revision.Objects.TryGetValue(number, out var location))
            {
                return null;
            }

            var text = revision.Text;
            if (location.Type == 1)
            {
                if (location.Value < 0 || location.Value >= revision.Bytes.Length)
                {
                    return null;
                }

                var objAt = text.IndexOf("obj", (int)location.Value, StringComparison.Ordinal);
                if (objAt < 0 || objAt > location.Value + 40)
                {
                    return null;
                }

                var p = SkipWhiteSpace(text, objAt + 3);
                if (p >= text.Length)
                {
                    return null;
                }
                if (IsDictionaryStart(text, p))
                {
                    return ExtractDictionary(text, p);
                }

                var end = text.IndexOf("endobj", p, StringComparison.Ordinal);
                return end < 0 ? null : text[p..end].Trim();
            }

            if (!revision.Objects.TryGetValue((int)location.Value, out var streamLocation) || streamLocation.Type != 1)
            {
                return null;
            }

            var streamDictAt = text.IndexOf("<<", (int)streamLocation.Value, StringComparison.Ordinal);
            if (streamDictAt < 0)
            {
                return null;
            }

            var streamDict = ExtractDictionary(text, streamDictAt);
            var raw = ReadStreamData(revision, streamDictAt + streamDict.Length, streamDict);
            var count = Regex.Match(streamDict, @"/N\s+(\d+)");
            var first = Regex.Match(streamDict, @"/First\s+(\d+)");
            if (raw == null || !count.Success || !first.Success)
            {
                return null;
            }

            var objectCount = int.Parse(count.Groups[1].Value);
            var firstOffset = int.Parse(first.Groups[1].Value);
            if (firstOffset > raw.Length)
            {
                return null;
            }

            var header = Encoding.Latin1.GetString(raw, 0, firstOffset).Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);
            var body = Encoding.Latin1.GetString(raw);
            for (var i = 0; i + 1 < header.Length && i / 2 < objectCount; i += 2)
            {
                if (int.Parse(header[i]) != number)
                {
                    continue;
                }

                var start = firstOffset + int.Parse(header[i + 1]);
                var end = i + 3 < header.Length ? firstOffset + int.Parse(header[i + 3]) : raw.Length;
                if (start < 0 || end > raw.Length || start >= end)
                {
                    return null;
                }

                var slice = body[start..end];
                var dictAt = slice.IndexOf("<<", StringComparison.Ordinal);
                return dictAt >= 0 ? ExtractDictionary(slice, dictAt) : slice.Trim();
            }

            return null;
        }

        public static byte[]? GetRawStreamBytes(PdfRevision revision, int number)
        {
            if (!revision.Objects.TryGetValue(number, out var location) || location.Type != 1)
            {
                return null;
            }

            var text = revision.Text;
            var objAt = text.IndexOf("obj", (int)location.Value, StringComparison.Ordinal);
            if (objAt < 0 || objAt > location.Value + 40)
            {
                return null;
            }

            var dictAt = SkipWhiteSpace(text, objAt + 3);
            if (!IsDictionaryStart(text, dictAt))
            {
                return null;
            }

            var dict = ExtractDictionary(text, dictAt);
            var streamAt = text.IndexOf("stream", dictAt + dict.Length, StringComparison.Ordinal);
            if (streamAt < 0 || streamAt > dictAt + dict.Length + 20)
            {
                return null;
            }

            var p = SkipStreamLineBreak(text, streamAt + 6);
            var lengthMatch = Regex.Match(dict, @"/Length\s+(\d+)\b(?!\s+\d+\s+R)");
            int length;
            if (lengthMatch.Success)
            {
                length = int.Parse(lengthMatch.Groups[1].Value);
            }
            else
            {
                var endAt = text.IndexOf("endstream", p, StringComparison.Ordinal);
                if (endAt < 0)
                {
                    return null;
                }
                length = endAt - p;
                while (length > 0 && (text[p + length - 1] == '\n' || text[p + length - 1] == '\r'))
                {
                    length--;
                }
            }

            if (p + length > revision.Bytes.Length)
            {
                return null;
            }
            return revision.Bytes.AsSpan(p, length).ToArray();
        }

        public static string ExtractDictionary(string text, int start)
        {
            var depth = 0;
            var i = start;
            while (i < text.Length - 1)
            {
                var c = text[i];
                if (c == '(')
                {
                    var level = 1;
                    i++;
                    while (i < text.Length && level > 0)
                    {
                        if (text[i] == '\\')
                        {
                            i += 2;
                            continue;
                        }
                        if (text[i] == '(')
                        {
                            level++;
                        }
                        else if (text[i] == ')')
                        {
                            level--;
                        }
                        i++;
                    }
                    continue;
                }

                if (c == '<' && text[i + 1] == '<')
                {
                    depth++;
                    i += 2;
                    continue;
                }
                if (c == '>' && text[i + 1] == '>')
                {
                    depth--;
                    i += 2;
                    if (depth == 0)
                    {
                        return text[start..i];
                    }
                    continue;
                }
                i++;
            }
            return text[start..];
        }

        public static int SkipWhiteSpace(string text, int position)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                position++;
            }
            return position;
        }

        public static bool IsDictionaryStart(string text, int position)
        {
            return position + 1 < text.Length && text[position] == '<' && text[position + 1] == '<';
        }

        private static string ReadClassicTable(PdfRevision revision, long offset)
        {
            var text = revision.Text;
            var pos = (int)offset;
            pos += Regex.Match(text[pos..Math.Min(text.Length, pos + 20)], @"\s*xref\s*").Length;

            while (true)
            {
                var head = Regex.Match(text[pos..Math.Min(text.Length, pos + 40)], @"^\s*(\d+)\s+(\d+)\s*");
                if (!head.Success)
                {
                    break;
                }

                var start = int.Parse(head.Groups[1].Value);
                var count = int.Parse(head.Groups[2].Value);
                pos += head.Length;
                for (var i = 0; i < count; i++)
                {
                    var entry = Regex.Match(text.Substring(pos, Math.Min(20, text.Length - pos)), @"(\d{10})\s+(\d{5})\s+([nf])");
                    if (entry.Success && entry.Groups[3].Value == "n" && !revision.Objects.ContainsKey(start + i))
                    {
                        revision.Objects[start + i] = new PdfObjectLocation { Type = 1, Value = long.Parse(entry.Groups[1].Value) };
                    }
                    pos += 20;
                }
            }

            var trailerAt = text.IndexOf("trailer", pos, StringComparison.Ordinal);
            if (trailerAt < 0)
            {
                return String.Empty;
            }
            var dictAt = text.IndexOf("<<", trailerAt, StringComparison.Ordinal);
            return dictAt < 0 ? String.Empty : ExtractDictionary(text, dictAt);
        }

        private static string ReadXrefStream(PdfRevision revision, long offset)
        {
            var text = revision.Text;
            var dictAt = text.IndexOf("<<", (int)offset, StringComparison.Ordinal);
            if (dictAt < 0 || dictAt > offset + 60)
            {
                return String.Empty;
            }

            var dict = ExtractDictionary(text, dictAt);
            var data = ReadStreamData(revision, dictAt + dict.Length, dict);
            var widthMatch = Regex.Match(dict, @"/W\s*\[([^\]]*)\]");
            if (data == null || !widthMatch.Success)
            {
                return dict;
            }

            var widths = widthMatch.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            var rowWidth = widths.Sum();
            if (rowWidth == 0)
            {
                return dict;
            }

            var sizeMatch = Regex.Match(dict, @"/Size\s+(\d+)");
            var size = sizeMatch.Success ? int.Parse(sizeMatch.Groups[1].Value) : 0;
            var indexMatch = Regex.Match(dict, @"/Index\s*\[([^\]]*)\]");
            var index = indexMatch.Success
                ? indexMatch.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()
                : [0, size];

            var pos = 0;
            for (var k = 0; k + 1 < index.Length; k += 2)
            {
                for (var n = index[k]; n < index[k] + index[k + 1]; n++)
                {
                    if (pos + rowWidth > data.Length)
                    {
                        return dict;
                    }

                    var fields = new long[3];
                    var cursor = pos;
                    for (var c = 0; c < widths.Length && c < 3; c++)
                    {
                        long value = 0;
                        for (var b = 0; b < widths[c]; b++)
                        {
                            value = value << 8 | data[cursor++];
                        }
                        fields[c] = value;
                    }
                    pos += rowWidth;

                    var type = widths[0] == 0 ? 1 : (int)fields[0];
                    if ((type == 1 || type == 2) && !revision.Objects.ContainsKey(n))
                    {
                        revision.Objects[n] = new PdfObjectLocation { Type = type, Value = fields[1], IndexInStream = (int)fields[2] };
                    }
                }
            }

            return dict;
        }

        private static byte[]? ReadStreamData(PdfRevision revision, int afterDict, string dict)
        {
            var text = revision.Text;
            var streamAt = text.IndexOf("stream", afterDict, StringComparison.Ordinal);
            var lengthMatch = Regex.Match(dict, @"/Length\s+(\d+)");
            if (streamAt < 0 || !lengthMatch.Success)
            {
                return null;
            }

            var p = SkipStreamLineBreak(text, streamAt + 6);
            var length = int.Parse(lengthMatch.Groups[1].Value);
            if (p + length > revision.Bytes.Length)
            {
                return null;
            }

            var raw = revision.Bytes.AsSpan(p, length).ToArray();
            if (dict.Contains("/FlateDecode", StringComparison.Ordinal))
            {
                try
                {
                    using var input = new MemoryStream(raw);
                    using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    zlib.CopyTo(output);
                    raw = output.ToArray();
                }
                catch (InvalidDataException)
                {
                    return null;
                }
            }
            else if (Regex.IsMatch(dict, @"/Filter\s*/?\w"))
            {
                return null;
            }

            var predictor = Regex.Match(dict, @"/Predictor\s+(\d+)");
            if (predictor.Success && int.Parse(predictor.Groups[1].Value) >= 10)
            {
                var columns = Regex.Match(dict, @"/Columns\s+(\d+)");
                raw = RemovePngPredictor(raw, columns.Success ? int.Parse(columns.Groups[1].Value) : 1);
            }
            return raw;
        }

        private static byte[] RemovePngPredictor(byte[] raw, int columns)
        {
            if (columns <= 0)
            {
                return raw;
            }

            var output = new List<byte>(raw.Length);
            var previous = new byte[columns];
            var q = 0;
            while (q + 1 + columns <= raw.Length)
            {
                var filter = raw[q++];
                var row = raw.AsSpan(q, columns).ToArray();
                q += columns;
                for (var i = 0; i < columns; i++)
                {
                    row[i] = filter switch
                    {
                        1 => (byte)(row[i] + (i > 0 ? row[i - 1] : 0)),
                        2 => (byte)(row[i] + previous[i]),
                        _ => row[i],
                    };
                }
                output.AddRange(row);
                previous = row;
            }
            return output.ToArray();
        }

        private static int SkipStreamLineBreak(string text, int position)
        {
            if (position + 1 < text.Length && text[position] == '\r' && text[position + 1] == '\n')
            {
                return position + 2;
            }
            if (position < text.Length && (text[position] == '\n' || text[position] == '\r'))
            {
                return position + 1;
            }
            return position;
        }
    }
}
