using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Mail;
using sh.be.applications.Module.Auth.Dtos.User;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.applications.Module.Auth.Interfaces.Queries;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Implements
{
    public class UserService : BaseService, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserQuery _userQuery;
        private readonly IMailExternal _mailExternal;
        private readonly UserDomainService _userDomainService;
        private readonly RoleDomainService _roleDomainService;

        public UserService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<UserService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IUserRepository userRepository, IRoleRepository roleRepository, IUserQuery userQuery, IMailExternal mailExternal,
            UserDomainService userDomainService, RoleDomainService roleDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userQuery = userQuery;
            _mailExternal = mailExternal;
            _userDomainService = userDomainService;
            _roleDomainService = roleDomainService;
        }

        public ViewMeDto GetMe()
        {
            _logger.LogInformation($"{nameof(GetMe)}");
            var currentUserId = getCurrentUserId();
            var infor = _userQuery.GetMe(currentUserId);
            if (infor == null)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            }
            return infor;
        }

        public async Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            return await _userQuery.FindPaging(dto);
        }

        public async Task Create(CreateUserDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, userName={dto.UserName}, email={dto.Email}, roleIds={string.Join(",", dto.RoleId)}, isRandomPassword={dto.IsRandomPassword}");

            var password = _userDomainService.ResolveInitialPassword(dto.IsRandomPassword, dto.Password, dto.PasswordConfirm);

            await _userDomainService.EnsureUniqueAsync(dto.UserName, dto.Email);

            var roleIds = dto.RoleId.Distinct().ToList();
            await _roleDomainService.EnsureAllExistAsync(roleIds);
            await _roleDomainService.EnsureCompatibleAsync(roleIds);

            var user = AppUser.Create(dto.UserName, dto.FullName, dto.Email, dto.PhoneNumber);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _userRepository.CreateAsync(user, password);

                var currentUserId = getCurrentUserId();
                _userRepository.AddUserRoles(user.AssignRoles(roleIds, currentUserId, null));
                AddAuditLog(AuditLogConstants.ACTION_USER_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo người dùng {user.UserName}",
                    null, new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber, RoleIds = roleIds });
                await _unitOfWork.SaveChangesAsync();

                await _mailExternal.SendAccountCreatedAsync(dto.Email, dto.UserName, password);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Create)}, userName={dto.UserName}, email={dto.Email}");
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task Update(UpdateUserDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, userName={dto.UserName}, email={dto.Email}, fullName={dto.FullName}, phoneNumber={dto.PhoneNumber}");

            var userId = getCurrentUserId();
            var user = await _userRepository.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var oldData = new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber };

            await _userDomainService.EnsureProfileChangeAllowedAsync(user, dto.UserName, dto.Email);
            user.UpdateProfile(dto.UserName, dto.FullName, dto.Email, dto.PhoneNumber);

            AddAuditLog(AuditLogConstants.ACTION_USER_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật người dùng {user.UserName}",
                oldData, new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber });
            await _userRepository.UpdateAsync(user);
        }


        public async Task Delete(string userId)
        {
            _logger.LogInformation($"{nameof(Delete)}");
            var now = GetVietnamTime();
            var userIdDeleted = getCurrentUserId();
            var user = await _userRepository.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _userRepository.DeleteAsync(user);

                var userRoles = await _userRepository.GetUserRolesAsync(userId);
                foreach (var userRole in userRoles)
                {
                    userRole.Delete(userIdDeleted, now);
                }
                AddAuditLog(AuditLogConstants.ACTION_USER_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa người dùng {user.UserName}",
                    new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber, RoleIds = userRoles.Select(ur => ur.RoleId) }, null);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Delete)}, userId={userId}");
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task ChangePassword(ChangePasswordDto dto)
        {
            var userId = getCurrentUserId();
            _logger.LogInformation($"{nameof(ChangePassword)}, userId={userId}");

            var user = await _userRepository.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            var newPassword = await _userDomainService.ValidatePasswordChangeAsync(user, dto.CurrentPassword, dto.NewPassword, dto.NewPasswordConfirm);

            AddAuditLog(AuditLogConstants.ACTION_USER_CHANGE_PASSWORD, AuditLogConstants.ACTION_TYPE_UPDATE, $"Người dùng {user.UserName} đổi mật khẩu");
            await _userRepository.ChangePasswordAsync(user, dto.CurrentPassword, newPassword);
        }

        public async Task UpdateRoleUser(UpdateRoleUserDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateRoleUser)}, dto = {JsonSerializer.Serialize(dto)}");
            var userId = getCurrentUserId();
            var now = GetVietnamTime();
            var user = await _userRepository.FindByIdAsync(dto.UserId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var oldRoleIds = await _userRepository.GetActiveRoleIdsAsync(dto.UserId);

            var roleIds = dto.RoleId.Distinct().ToList();
            await _roleDomainService.EnsureAllExistAsync(roleIds);
            await _roleDomainService.EnsureCompatibleAsync(roleIds);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _userRepository.DeleteUserRolesAsync(dto.UserId);
                _userRepository.AddUserRoles(user.AssignRoles(roleIds, userId, now));
                AddAuditLog(AuditLogConstants.ACTION_USER_UPDATE_ROLE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật vai trò của người dùng {user.UserName}",
                    new { dto.UserId, RoleIds = oldRoleIds }, new { dto.UserId, RoleIds = roleIds });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(UpdateRoleUser)}, userId={dto.UserId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<string>> GetRoleNames(string userId)
        {
            _logger.LogInformation($"{nameof(GetRoleNames)}, userId={userId}");
            return await _roleRepository.GetRoleNamesByUserIdAsync(userId);
        }
    }
}
