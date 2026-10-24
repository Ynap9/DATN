using System.Text;

namespace sh.be.domain.Module.Construction.DocumentFields.Services
{
    public static class FieldImportMatchPolicy
    {
        public static bool IsMatch(string? fieldImport, string header)
        {
            var fieldImportWords = SplitWords(fieldImport);
            return fieldImportWords.Length > 0
                && fieldImportWords.SequenceEqual(SplitWords(header), StringComparer.OrdinalIgnoreCase);
        }

        public static string ToMatchKey(string? text)
        {
            return string.Join(' ', SplitWords(text));
        }

        private static string[] SplitWords(string? text)
        {
            return (text ?? String.Empty)
                .Normalize(NormalizationForm.FormC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
