using PdfSharp.Fonts;
using sh.be.shared.Constants.DigitalSignature;
using System.Collections.Concurrent;

namespace sh.be.infrastructure.external.DigitalSignature.Base
{
    public class AppearanceFontResolver : IFontResolver
    {
        private static readonly EnumerationOptions FileSearchOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        private readonly ConcurrentDictionary<string, string?> _pathByFamily = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte[]> _bytesByPath = new();

        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
        {
            var path = _pathByFamily.GetOrAdd(familyName, family => Locate(PdfSigningConst.FONT_FILES.TryGetValue(family, out var files) ? files : PdfSigningConst.SERIF_REGULAR_FILES));
            return path == null ? null : new FontResolverInfo(path, bold, italic);
        }

        public byte[]? GetFont(string faceName)
        {
            return _bytesByPath.GetOrAdd(faceName, File.ReadAllBytes);
        }

        private static string? Locate(IReadOnlyList<string> fileNames)
        {
            var directories = new[] { Environment.GetFolderPath(Environment.SpecialFolder.Fonts) }
                .Concat(PdfSigningConst.FONT_SEARCH_DIRECTORIES)
                .Where(directory => !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory));
            foreach (var directory in directories)
            {
                foreach (var fileName in fileNames)
                {
                    var found = Directory.EnumerateFiles(directory, fileName, FileSearchOptions).FirstOrDefault();
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }
    }
}
