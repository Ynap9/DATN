using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Module.Auth.Dtos.User;
using sh.be.applications.Module.Auth.Interfaces;
using sh.be.domain.Module.Auth;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.infrastructure.external.Mail.Interfaces;
using sh.be.infrastructure.external.Mail.Templates;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.Mail;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Implements
{
    public class UserService : BaseService, IUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mailService;

        public UserService(ShDbContext shDbContext, IMapper mapper, ILogger<UserService> logger, IHttpContextAccessor httpContextAccessor,
            UserManager<AppUser> userManager, IMailService mailService
        ) : base(shDbContext, mapper, logger, httpContextAccessor)
        {
            _userManager = userManager;
            _mailService = mailService;
        }

        public ViewMeDto GetMe()
        {
            _logger.LogInformation($"{nameof(GetMe)}");
            var currentUserId = getCurrentUserId();
            var infor = _shDbContext.Users
                .Where(u => u.Id == currentUserId)
                .Select(u => new ViewMeDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Roles = _shDbContext.UserRoles
                            .Where(ur => ur.UserId == u.Id)
                            .Join(_shDbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
                            .Select(r => new UserRoleDto
                            {
                                Id = r.Id,
                                Name = r.Name,
                                Description = r.Description,
                                Permissions = _shDbContext.RolePermissions
                                .Where(rp => rp.RoleId == r.Id)
                                .Join(_shDbContext.Permissions, rp => rp.PermissionKey, p => p.Key, (rp, p) => p)
                                .Select(P => new UserPermissionDto
                                {
                                    Key = P.Key,
                                    Description = P.Description,
                                    ParentKey = P.ParentKey,
                                    Module = P.Module
                                }).ToList()


                            }).ToList()
                }).FirstOrDefault();
            if (infor == null)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            }
            return infor;
        }

        public async Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            var query = _shDbContext.Users.AsNoTracking();
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(u => u.UserName!.Contains(dto.Keyword)
                                    || u.FullName!.Contains(dto.Keyword)
                                    || u.Email!.Contains(dto.Keyword)
                                    ||_shDbContext.UserRoles.Any(ur => ur.UserId == u.Id && !ur.Deleted
                                    && _shDbContext.Roles.Any(r => r.Id == ur.RoleId && !r.Deleted && r.Name!.Contains(dto.Keyword)))
                );
            }
           
            var totalCount = await query.CountAsync();

            var user = await query
                .Paging(dto)
                .AsNoTracking()
                .ToListAsync();
            var userIds = user.Select(u=> u.Id).ToList();

            var roles = await _shDbContext.UserRoles
                .Where(ur => userIds.Contains(ur.UserId) && !ur.Deleted)
                .Join(_shDbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Id, r.Name })
                .ToListAsync();
            var roleByUser = roles.ToLookup( x=> x.UserId );
            var items = user.Select( u => new ViewUserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? " ",
                FullName = u.FullName,
                Email = u.Email ?? " ",
                Roles = roleByUser[u.Id]
                        .Select(r => new UserRolePaging
                        {
                            Id = r.Id,
                            Name = r.Name,
                        }).ToList(),
            }).ToList();
            return new BaseResponsePagingDto<ViewUserDto>
            {
                Items = items,
                TotalItems = totalCount,
            };



        }

        public async Task Create(CreateUserDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, userName={dto.UserName}, email={dto.Email}, roleIds={string.Join(",", dto.RoleId)}, isRandomPassword={dto.IsRandomPassword}");

            var password = dto.IsRandomPassword
                ? CryptoUtils.GenerateRandomString(UserConstants.PasswordLength)
                : ValidationUtils.ValidatePassword(dto.Password, dto.PasswordConfirm);

            if (await _userManager.FindByNameAsync(dto.UserName) != null)
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Tên đăng nhập đã tồn tại");
            }
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Email đã tồn tại");
            }

            var roleIds = dto.RoleId.Distinct().ToList();
            var existingRoleCount = await _shDbContext.Roles
                .CountAsync(r => roleIds.Contains(r.Id) && !r.Deleted);
            if (roleIds.Count == 0 || existingRoleCount != roleIds.Count)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }

            var user = new AppUser
            {
                UserName = dto.UserName,
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true,
            };

            await using var transaction = await _shDbContext.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new UserFriendlyException(ErrorCodes.AuthErrorCreateUser, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var currentUserId = getCurrentUserId();
                _shDbContext.UserRoles.AddRange(roleIds.Select(roleId => new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    CreatedBy = currentUserId,
                }));
                AddAuditLog(AuditLogConstants.ACTION_USER_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo người dùng {user.UserName}",
                    null, new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber, RoleIds = roleIds });
                await _shDbContext.SaveChangesAsync();

                await _mailService.SendAsync(dto.Email, MailTemplateConstants.SUBJECT_ACCOUNT_CREATED, AccountCreatedMailTemplate.Build(dto.UserName, password));

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
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var oldData = new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber };

            if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
            {
                if (await _userManager.FindByNameAsync(dto.UserName) != null)
                {
                    throw new UserFriendlyException(ErrorCodes.Found, "Tên đăng nhập đã tồn tại");
                }
                user.UserName = dto.UserName;
            }
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                if (await _userManager.FindByEmailAsync(dto.Email) != null)
                {
                    throw new UserFriendlyException(ErrorCodes.Found, "Email đã tồn tại");
                }
                user.Email = dto.Email;
            }
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                user.FullName = dto.FullName;
            }
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                user.PhoneNumber = dto.PhoneNumber;
            }

            AddAuditLog(AuditLogConstants.ACTION_USER_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật người dùng {user.UserName}",
                oldData, new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber });
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorUpdateUser, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }


        public async Task Delete(string userId)
        {
            _logger.LogInformation($"{nameof(Delete)}");
            var now = GetVietnamTime();
            var userIdDeleted = getCurrentUserId();
            var user = await _shDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            await using var transaction = await _shDbContext.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    throw new UserFriendlyException(ErrorCodes.AuthErrorDeleteUser, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var userRoles = await _shDbContext.UserRoles
                                      .Where(ur => ur.UserId == userId)
                                      .ToListAsync();
                foreach (var userRole in userRoles)
                {
                    userRole.Deleted = true;
                    userRole.DeletedDate = now;
                    userRole.DeletedBy = userIdDeleted;



                }
                AddAuditLog(AuditLogConstants.ACTION_USER_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa người dùng {user.UserName}",
                    new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber, RoleIds = userRoles.Select(ur => ur.RoleId) }, null);
                await _shDbContext.SaveChangesAsync();
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

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            var newPassword = ValidationUtils.ValidatePassword(dto.NewPassword, dto.NewPasswordConfirm);

            if (!await _userManager.CheckPasswordAsync(user, dto.CurrentPassword))
            {
                throw new UserFriendlyException(ErrorCodes.AuthInvalidPassword);
            }
            if (dto.CurrentPassword == newPassword)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Mật khẩu mới phải khác mật khẩu hiện tại");
            }

            AddAuditLog(AuditLogConstants.ACTION_USER_CHANGE_PASSWORD, AuditLogConstants.ACTION_TYPE_UPDATE, $"Người dùng {user.UserName} đổi mật khẩu");
            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, newPassword);
            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorChangePassword, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task UpdateRoleUser(UpdateRoleUserDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateRoleUser)}, dto = {JsonSerializer.Serialize(dto)}");
            var userId = getCurrentUserId();
            var now = GetVietnamTime();
            var user = await _userManager.FindByIdAsync(dto.UserId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var oldRoleIds = await _shDbContext.UserRoles
                .Where(ur => ur.UserId == dto.UserId && !ur.Deleted)
                .Select(ur => ur.RoleId)
                .ToListAsync();
            var roleByUser = await _shDbContext.UserRoles
                             .Where(ur => ur.UserId == dto.UserId)
                             .ExecuteDeleteAsync();
            var roleIds = dto.RoleId.Distinct().ToList();
            var existingRoleCount = await _shDbContext.Roles
                .CountAsync( r => roleIds.Contains(r.Id) && !r.Deleted);
            if (existingRoleCount == 0 || existingRoleCount != roleIds.Count) {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }

            
            _shDbContext.UserRoles.AddRange(roleIds.Select(roleId => new UserRole
            {
                UserId = dto.UserId,
                RoleId = roleId,
                CreatedBy = userId,
                CreatedDate = now,

            }));
            AddAuditLog(AuditLogConstants.ACTION_USER_UPDATE_ROLE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật vai trò của người dùng {user.UserName}",
                new { dto.UserId, RoleIds = oldRoleIds }, new { dto.UserId, RoleIds = roleIds });

            await _shDbContext.SaveChangesAsync();
        }
   

                             
        }
    }

