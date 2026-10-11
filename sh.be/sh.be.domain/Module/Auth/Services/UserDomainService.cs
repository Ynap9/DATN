using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.shared.Constants.Auth;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Utils;

namespace sh.be.domain.Module.Auth.Services
{
    public class UserDomainService
    {
        private readonly IUserRepository _userRepository;

        public UserDomainService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public string ResolveInitialPassword(bool isRandomPassword, string? password, string? passwordConfirm)
        {
            return isRandomPassword
                ? CryptoUtils.GenerateRandomString(UserConstants.PasswordLength)
                : ValidationUtils.ValidatePassword(password, passwordConfirm);
        }

        public async Task EnsureUniqueAsync(string userName, string email)
        {
            if (await _userRepository.FindByNameAsync(userName) != null)
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Tên đăng nhập đã tồn tại");
            }
            if (await _userRepository.FindByEmailAsync(email) != null)
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Email đã tồn tại");
            }
        }

        public async Task EnsureProfileChangeAllowedAsync(AppUser user, string? userName, string? email)
        {
            if (!string.IsNullOrWhiteSpace(userName) && userName != user.UserName
                && await _userRepository.FindByNameAsync(userName) != null)
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Tên đăng nhập đã tồn tại");
            }
            if (!string.IsNullOrWhiteSpace(email) && email != user.Email
                && await _userRepository.FindByEmailAsync(email) != null)
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Email đã tồn tại");
            }
        }

        public async Task<string> ValidatePasswordChangeAsync(AppUser user, string currentPassword, string? newPassword, string? newPasswordConfirm)
        {
            var validatedPassword = ValidationUtils.ValidatePassword(newPassword, newPasswordConfirm);

            if (!await _userRepository.CheckPasswordAsync(user, currentPassword))
            {
                throw new UserFriendlyException(ErrorCodes.AuthInvalidPassword);
            }
            if (currentPassword == validatedPassword)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Mật khẩu mới phải khác mật khẩu hiện tại");
            }
            return validatedPassword;
        }
    }
}
