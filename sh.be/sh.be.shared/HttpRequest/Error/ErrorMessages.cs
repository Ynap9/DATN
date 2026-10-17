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
            { ErrorCodes.ConstructionErrorSubmissionNotFound, "Hồ sơ không tồn tại" },
            { ErrorCodes.ConstructionErrorImportSheetEmpty, "Google Sheet không có dữ liệu" },
            { ErrorCodes.ConstructionErrorImportColumnNotFound, "Không tìm thấy cột trên Google Sheet" },
            { ErrorCodes.ConstructionErrorImportDuplicateSubmissionKey, "Google Sheet có hồ sơ bị trùng" },
            { ErrorCodes.ConstructionErrorImportNotEnoughFieldMapping, "Số cột trùng tên trên sheet nhiều hơn số trường được cấu hình" },
            { ErrorCodes.ConstructionErrorImportInProgress, "Đang có người import cho loại văn bản này, vui lòng thử lại sau" },
            { ErrorCodes.ConstructionErrorDocumentFieldNotInProject, "Trường không thuộc cấu hình import của dự án và loại văn bản" },
            { ErrorCodes.ConstructionErrorDocumentTypeNotInProject, "Loại văn bản không thuộc dự án" },
            { ErrorCodes.ConstructionErrorProjectDone, "Dự án đã hoàn thành, không được import" },
            { ErrorCodes.ConstructionErrorImportFileUrlMissing, "Google Sheet có hồ sơ thiếu link file" },
            { ErrorCodes.ConstructionErrorFileUnavailable, "Không tải được file (file không tồn tại, không truy cập được hoặc không phải PDF)" },
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
