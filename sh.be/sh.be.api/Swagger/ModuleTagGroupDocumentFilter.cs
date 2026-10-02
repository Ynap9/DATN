using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using sh.be.shared.Constants.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace sh.be.api.Swagger
{
    public class ModuleTagGroupDocumentFilter: IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if(context.DocumentName != SwaggerConstants.ALL)
            {
                return;
            }

            var groups = new OpenApiArray();
            foreach( var module in context.ApiDescriptions
                .GroupBy(x => x.GroupName ?? SwaggerConstants.COMMON)
                .OrderBy(x => x.Key))
            {
                var tags = new OpenApiArray();
                foreach( var controller in module
                    .Select( x => x.ActionDescriptor.RouteValues["controller"])
                    .Distinct()
                    .OrderBy(x => x))
                {
                    tags.Add(new OpenApiString(controller));
                }

                groups.Add(new OpenApiObject
                {
                    ["name"] = new OpenApiString(module.Key.ToUpper()),
                    ["tags"] = tags
                });
            }

            swaggerDoc.Extensions["x-tagGroups"] = groups;
        }
    }
}
