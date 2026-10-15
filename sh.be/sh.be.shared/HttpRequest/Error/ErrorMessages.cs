using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.shared.HttpRequest.Error
{
    public static class ErrorMessages
    {
        private static readonly Dictionary<int, string> _message = new()
        {
            { ErrorCodes.System, "System error" },
            { ErrorCodes.BadRequest, "Bad request" },
            { ErrorCodes.Unauthorized, "Unauthorized" },
            { ErrorCodes.NotFound, "Not found" },
            { ErrorCodes.Found, "Found" },
            { ErrorCodes.InternalServerError, "Internal server error" },


            //Auth
            { ErrorCodes.AuthErrorUserNotFound, "User không tồn tại" },
            { ErrorCodes.AuthErrorRoleNotFound, "Role không tồn tại" },
            { ErrorCodes.AuthInvalidPassword, "Mật khẩu không đúng" },
            { ErrorCodes.AuthErrorCreateUser, "Lỗi tạo user" },
            { ErrorCodes.AuthErrorCreateRole, "Lỗi tạo role" },
            { ErrorCodes.AuthErrorUpdateUser, "Lỗi cập nhật user" },
            { ErrorCodes.AuthErrorChangePassword, "Lỗi đổi mật khẩu" },

            //Core
            { ErrorCodes.CoreErrorAuditLogNotFound, "Audit log không tồn tại" },
            { ErrorCodes.CoreErrorInvalidDataSource, "Đường dẫn nguồn dữ liệu không hợp lệ" },
            { ErrorCodes.CoreErrorReadDataSourceFailed, "Không đọc được dữ liệu từ nguồn, kiểm tra lại quyền chia sẻ" },
            { ErrorCodes.CoreErrorSheetNotFound, "Không tìm thấy sheet trong nguồn dữ liệu" },
            { ErrorCodes.CoreErrorCreateDataSourceFailed, "Không tạo được nguồn dữ liệu, kiểm tra lại cấu hình thư mục và quyền chia sẻ" },
            { ErrorCodes.CoreErrorUpdateDataSourceFailed, "Không cập nhật được nguồn dữ liệu, kiểm tra lại quyền chia sẻ" },
            { ErrorCodes.CoreErrorAuthorizeDataSourceFailed, "Không xác thực được tài khoản nguồn dữ liệu, cần xác thực lại" },
            { ErrorCodes.ConstructionErrorDocumentTypeNotFound, "Loại văn bản không tồn tại" },
            { ErrorCodes.ConstructionErrorDocumentFieldNotFound, "Trường của loại văn bản không tồn tại" },
            { ErrorCodes.ConstructionErrorProjectNotFound, "Dự án không tồn tại" },
            { ErrorCodes.ConstructionErrorProjectConfigNotFound, "Cấu hình dự án không tồn tại" },
        };
        public static string GetMessage(int code)
        {
            if (_message.TryGetValue(code, out var message))
            {
                return message;
            }
            return "Unknown error";
        }
    }
}
