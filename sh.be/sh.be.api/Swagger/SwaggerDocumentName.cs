using Asp.Versioning;
using sh.be.shared.Constants.Swagger;
using sh.be.shared.Constants.Versioning;

namespace sh.be.api.Swagger
{
    public static class SwaggerDocumentName
    {
        public static string Build(string apiVersionGroupName, string? module)
        {
            return string.Format(SwaggerConstants.DOCUMENT_NAME_FORMAT, apiVersionGroupName, module);
        }

        public static string BuildApiVersionGroupName(ApiVersion apiVersion)
        {
            return apiVersion.ToString(ApiVersionConstants.GROUP_NAME_FORMAT);
        }

        public static bool IsAll(string documentName)
        {
            return documentName.EndsWith(Build(string.Empty, SwaggerConstants.ALL));
        }
    }
}
