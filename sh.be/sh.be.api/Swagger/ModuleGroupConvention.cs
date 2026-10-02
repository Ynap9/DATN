using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace sh.be.api.Swagger
{
    public class ModuleGroupConvention: IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var ns = controller.ControllerType.Namespace;
            if(ns == null || !ns.Contains(".Module.")) 
            {
                return;
            }

            var module = ns.Split('.').SkipWhile(x => x != "Module").Skip(1).FirstOrDefault();
            if(module != null)
            {
                controller.ApiExplorer.GroupName = module.ToLower();
            }
        }
    }
}
