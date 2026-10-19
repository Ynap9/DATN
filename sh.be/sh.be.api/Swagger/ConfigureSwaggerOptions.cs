using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using sh.be.shared.Constants.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace sh.be.api.Swagger
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _apiVersionDescriptionProvider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
        {
            _apiVersionDescriptionProvider = apiVersionDescriptionProvider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var apiVersionDescriptions in _apiVersionDescriptionProvider.ApiVersionDescriptions.GroupBy(description => description.ApiVersion))
            {
                var apiVersionGroupName = SwaggerDocumentName.BuildApiVersionGroupName(apiVersionDescriptions.Key);
                var lifecycleNotes = new List<string>();
                if (apiVersionDescriptions.All(description => description.IsDeprecated))
                {
                    lifecycleNotes.Add(SwaggerConstants.DEPRECATED_DESCRIPTION);
                }
                if (apiVersionDescriptions.First().SunsetPolicy?.Date is DateTimeOffset sunsetDate)
                {
                    lifecycleNotes.Add(string.Format(SwaggerConstants.SUNSET_DESCRIPTION, sunsetDate));
                }

                foreach (var module in SwaggerConstants.DOCUMENTS)
                {
                    options.SwaggerDoc(SwaggerDocumentName.Build(apiVersionGroupName, module), new OpenApiInfo
                    {
                        Title = $"Sh API {apiVersionGroupName} - {module.ToUpper()}",
                        Version = apiVersionDescriptions.Key.ToString(),
                        Description = string.Join(" ", lifecycleNotes),
                    });
                }
            }
        }
    }
}
