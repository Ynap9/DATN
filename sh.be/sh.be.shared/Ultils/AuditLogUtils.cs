using System.Text.Encodings.Web;
using System.Text.Json;

namespace sh.be.shared.Utils
{
    public static class AuditLogUtils
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static string? ToJson(object? data)
        {
            if (data == null)
            {
                return null;
            }
            return JsonSerializer.Serialize(data, JsonOptions);
        }
    }
}
