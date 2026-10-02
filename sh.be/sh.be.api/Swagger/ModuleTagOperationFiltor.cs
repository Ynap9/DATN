using Microsoft.OpenApi.Models;
using sh.be.shared.Constants.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace sh.be.api.Swagger
{
    public class ModuleTagOperationFiltor: IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if(context.DocumentName != SwaggerConstants.ALL)
            {
                return;
            }
            var module = context.ApiDescription.GroupName ?? SwaggerConstants.COMMON;
            operation.Tags = new List<OpenApiTag> { new() { Name = module.ToLower()} };
        }
    }
}
