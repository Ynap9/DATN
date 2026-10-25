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
            { ErrorCodes.CoreErrorStorageNotConfigured, "Chưa cấu hình kho lưu trữ file" },
            { ErrorCodes.CoreErrorUploadFileFailed, "Không tải được file lên kho lưu trữ" },
            { ErrorCodes.CoreErrorDownloadFileFailed, "Không đọc được file từ kho lưu trữ" },
            { ErrorCodes.CoreErrorBackgroundJobUnavailable, "Hệ thống xử lý nền đang tắt, hãy thử lại sau" },
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
            { ErrorCodes.ConstructionErrorProjectDoneNotEditable, "Dự án đã hoàn thành, không được sửa hồ sơ" },
            { ErrorCodes.ConstructionErrorSubmissionPositionOutOfRange, "Không còn hồ sơ ở vị trí này" },
            { ErrorCodes.ConstructionErrorSubmissionDataDuplicateField, "Một trường được gửi nhiều lần trong cùng yêu cầu" },
            { ErrorCodes.ConstructionErrorSubmissionNotInDocumentType, "Hồ sơ không thuộc dự án và loại văn bản đang chọn" },
            { ErrorCodes.ConstructionErrorSubmissionNotCheckedForQa, "Chỉ QA được hồ sơ đã kiểm tra" },
            { ErrorCodes.ConstructionErrorQaReasonRequired, "Đánh giá không đạt phải nhập lý do" },
            { ErrorCodes.ConstructionErrorExportTooManyRows, "Số hồ sơ cần xuất vượt quá giới hạn, hãy lọc bớt" },
            { ErrorCodes.ConstructionErrorProjectExpired, "Dự án đã hết hạn, không được thao tác hồ sơ" },
            { ErrorCodes.ConstructionErrorQaWrongFieldRequired, "Đánh giá không đạt phải chọn ít nhất một trường sai" },
            { ErrorCodes.ConstructionErrorSubmissionCheckedNotDeletable, "Không được xoá hồ sơ đã kiểm tra" },
            { ErrorCodes.ConstructionErrorExportJobNotFound, "Không tìm thấy yêu cầu xuất Excel hoặc yêu cầu đã hết hạn" },
            { ErrorCodes.DigitalSignatureErrorTemplateNotFound, "Mẫu chữ ký không tồn tại" },
            { ErrorCodes.DigitalSignatureErrorInvalidTemplatePositionKind, "Loại khối chữ ký không hợp lệ" },
            { ErrorCodes.DigitalSignatureErrorTemplatePositionOutOfPage, "Tọa độ khối chữ ký nằm ngoài trang hoặc có kích thước bằng 0" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchNotFound, "Lô ký không tồn tại" },
            { ErrorCodes.DigitalSignatureErrorSignedFolderNameRequired, "Chưa nhập tên đợt ký" },
            { ErrorCodes.DigitalSignatureErrorProjectBatchRejectsUpload, "Lô này ký file của dự án nên không nhận file tải lên" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchStarted, "Lô đã bắt đầu ký nên không nhận thêm file được" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchRunning, "Lô đang ký nên chưa xóa file được. Tạm dừng lô rồi xóa" },
            { ErrorCodes.DigitalSignatureErrorCertificateRequired, "Chưa chọn chứng thư số để ký" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchFileNotFound, "File không tồn tại trong lô ký" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchFileNotDeletable, "Chỉ xóa được file đang chờ ký hoặc bị lỗi" },
            { ErrorCodes.DigitalSignatureErrorInvalidTemplateImage, "Ảnh phải là file .png, .jpg hoặc .jpeg, không rỗng và không quá 5 MB" },
            { ErrorCodes.DigitalSignatureErrorPluginSetupMissing, "Máy chủ chưa có bộ cài plugin ký số" },
            { ErrorCodes.DigitalSignatureErrorInvalidCertificate, "Không đọc được chứng thư số" },
            { ErrorCodes.DigitalSignatureErrorUntrustedCertificate, "Chứng thư số không do CA Ban Cơ yếu Chính phủ cấp hoặc đã hết hạn" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchNotOwned, "Lô ký không thuộc về bạn" },
            { ErrorCodes.DigitalSignatureErrorTooManyUploadFiles, "Mỗi lần chỉ tải lên tối đa 200 file" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchNotSignable, "Lô đang ký hoặc đã kết thúc nên không mở phiên ký được" },
            { ErrorCodes.DigitalSignatureErrorSigningSessionNotOpen, "Chưa mở phiên ký hoặc phiên ký đã đóng" },
            { ErrorCodes.DigitalSignatureErrorSigningCertificateMismatch, "Chứng thư của phiên ký khác chứng thư đã chọn cho lô" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchEmpty, "Lô không còn file nào chờ ký" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchNotRunning, "Lô không ở trạng thái đang ký" },
            { ErrorCodes.DigitalSignatureErrorSigningBatchFinished, "Lô đã kết thúc" },
            { ErrorCodes.DigitalSignatureErrorSignedFileNotReady, "File chưa ký xong" },
            { ErrorCodes.DigitalSignatureErrorPdfUnsupported, "File PDF hỏng hoặc có cấu trúc chưa hỗ trợ ký nối bản" },
            { ErrorCodes.DigitalSignatureErrorPdfEncrypted, "File PDF được mã hóa nên không ký được" },
            { ErrorCodes.DigitalSignatureErrorPdfLocked, "File PDF có chữ ký chứng thực cấm mọi thay đổi" },
            { ErrorCodes.DigitalSignatureErrorPdfAlreadySigned, "File đã có chữ ký số. Bật \"Cho phép ký đè\" ở mẫu chữ ký nếu vẫn muốn ký thêm" },
            { ErrorCodes.DigitalSignatureErrorSignatureTooLarge, "Khối chữ ký vượt chỗ trống đã chừa trong file" },
            { ErrorCodes.DigitalSignatureErrorTimestampFailed, "Không lấy được dấu thời gian từ TSA" },
            { ErrorCodes.DigitalSignatureErrorSignatureInvalid, "Chữ ký vừa ghép không hợp lệ" },
            { ErrorCodes.DigitalSignatureErrorSignatureRejected, "Plugin không ký được file này" },
            { ErrorCodes.DigitalSignatureErrorTemplateImageUnreadable, "Không đọc được ảnh dấu đỏ hoặc chữ ký tay của mẫu chữ ký" },
            { ErrorCodes.DigitalSignatureErrorSigningRequestTimeout, "Quá thời gian chờ plugin ký" },
            { ErrorCodes.DigitalSignatureErrorAppearanceBuildFailed, "Không dựng được hình chữ ký trên trang" },
            { ErrorCodes.PackagingErrorPackageTypeInvalid, "Loại gói không hợp lệ" },
            { ErrorCodes.PackagingErrorMetadataTypeInvalid, "Loại metadata không hợp lệ" },
            { ErrorCodes.PackagingErrorSchemaFieldNotFound, "Loại gói và loại metadata đã chọn chưa có cấu hình trường" },
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
