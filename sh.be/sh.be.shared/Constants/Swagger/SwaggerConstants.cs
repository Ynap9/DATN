using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.shared.Constants.Swagger
{
    public static class SwaggerConstants
    {
        public const string ALL = "all";
        public const string AUTH = "auth";
        public const string CORE = "core";
        public const string CONSTRUCTION = "construction";
        public const string COMMON = "common";


        public static readonly string[] MODULES = { AUTH, CORE, CONSTRUCTION };
    }
}
