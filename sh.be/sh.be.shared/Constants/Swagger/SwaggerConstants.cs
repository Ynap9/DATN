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
        public const string DIGITAL_SIGNATURE = "digitalsignature";
        public const string COMMON = "common";
        public const string DOCUMENT_NAME_FORMAT = "{0}-{1}";
        public const string DEPRECATED_DESCRIPTION = "Phiên bản này đã ngừng phát triển, hãy chuyển sang phiên bản mới hơn.";
        public const string SUNSET_DESCRIPTION = "Ngừng hoạt động từ ngày {0:dd/MM/yyyy}.";


        public static readonly string[] MODULES = { AUTH, CORE, CONSTRUCTION, DIGITAL_SIGNATURE };
        public static readonly string[] DOCUMENTS = [ALL, .. MODULES];
    }
}
