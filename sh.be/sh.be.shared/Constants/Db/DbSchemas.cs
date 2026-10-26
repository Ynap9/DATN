using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.shared.Constants.Db
{
    public static class DbSchemas
    {
        public const string TableMigrationsHistory = "__EFMigrationsHistory";
        public const string Default = "dbo";

        public const string Auth = "auth";
        public const string Core = "core";
        public const string Construction = "construction";
        public const string DigitalSignature = "digitalsignature";
        public const string Packaging = "packaging";
        public const string Operation = "operation";
        public const string Ai = "ai";
        public const string Hangfire = "hangfire";
    }
}
