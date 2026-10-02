using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.shared.Utils
{
    public static class ValidationUtils
    {
        public static string ValidatePassword(string? password, string? passwordConfirm)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Mật khẩu không được để trống");
            }
            if (password != passwordConfirm)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Mật khẩu xác nhận không khớp");
            }
            return password;
        }
    }
}
