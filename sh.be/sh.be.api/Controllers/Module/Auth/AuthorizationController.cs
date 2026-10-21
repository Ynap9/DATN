using Asp.Versioning;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.applications.Module.Core.Dtos.AuditLog;
using sh.be.applications.Module.Core.Interfaces.Services;
using sh.be.domain.Module.Auth.Entities;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Settings;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace sh.be.api.Controllers.Module.Auth
{
    [Route("")]
    [ApiController]
    [ApiVersionNeutral]
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly AuthServerSettings _authServerSettings;
        private readonly ILogger<AuthorizationController> _logger;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        public AuthorizationController(
           IOpenIddictApplicationManager applicationManager,
           IOptions<AuthServerSettings> options,
           ILogger<AuthorizationController> logger,
           IUserService userService,
           IAuditLogService auditLogService)
        {
            _applicationManager = applicationManager;
            _authServerSettings = options.Value;
            _logger = logger;
            _userService = userService;
            _auditLogService = auditLogService;
        }
    
        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange([FromServices] UserManager<AppUser> userManager)
        {
            var request = HttpContext.GetOpenIddictServerRequest();
            _logger.LogInformation("Token exchange request received. Grant type: {GrantType}", request.GrantType);

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            try
            {
                if (request.IsClientCredentialsGrantType())
                {
                    _logger.LogInformation("Processing client credentials grant for client: {ClientId}", request.ClientId);

                    // Note: the client credentials are automatically validated by OpenIddict:
                    // if client_id or client_secret are invalid, this action won't be invoked.

                    var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                        throw new InvalidOperationException("The application cannot be found.");

                    // Use the client_id as the subject identifier.
                    identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
                    identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));

                    identity.SetDestinations(static claim => claim.Type switch
                    {
                        // Allow the "name" claim to be stored in both the access and identity tokens
                        // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                        Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                            => [Destinations.AccessToken, Destinations.IdentityToken],

                        // Otherwise, only store the claim in the access tokens.
                        _ => [Destinations.AccessToken]
                    });

                    _logger.LogInformation("Client credentials grant successful for client: {ClientId}", request.ClientId);
                    return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
                else if (request.IsAuthorizationCodeGrantType())
                {
                    _logger.LogInformation("Processing authorization code grant");

                    // Note: the client credentials are automatically validated by OpenIddict:
                    // if client_id or client_secret are invalid, this action won't be invoked.

                    var result = await HttpContext.AuthenticateAsync(
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                    );
                    string subject = result.Principal!.GetClaim(Claims.Subject)!;

                    var user = await userManager.FindByIdAsync(subject)
                        ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

                    _logger.LogInformation("Authorization code grant for user: {UserId}", user.Id);

          
                    identity.SetClaim(Claims.Subject, subject);
                    identity.SetClaim(Claims.Name, user.FullName);
                    identity.SetClaim(Claims.Username, user.UserName);
                    identity.SetClaim(CustomClaimTypes.SecurityStamp, await userManager.GetSecurityStampAsync(user));
                    var roles = await _userService.GetRoleNames(user.Id);
                    foreach (var role in roles)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                    identity.SetScopes(
                            new[]
                            {
                            Scopes.OpenId,
                            Scopes.Email,
                            Scopes.Profile,
                            Scopes.Roles,
                            Scopes.OfflineAccess
                            }.Intersect(result.Principal!.GetScopes())
                        );
                    identity.SetDestinations(claim => claim.Type switch
                    {
                        // Allow the "name" claim to be stored in both the access and identity tokens
                        // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                        Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                            => [Destinations.AccessToken, Destinations.IdentityToken],
                        ClaimTypes.Role => [Destinations.AccessToken, Destinations.IdentityToken],
                        CustomClaimTypes.SecurityStamp => [],
                        // Otherwise, only store the claim in the access tokens.
                        _ => [Destinations.AccessToken]
                    });

                    _logger.LogInformation("Authorization code grant successful for user: {UserId}", user.Id);
                    return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
                else if (request.IsPasswordGrantType())
                {
                    string username = request.Username!;
                    _logger.LogInformation("Processing password grant for user: {Username}", username);

                    // Note: the client credentials are automatically validated by OpenIddict:
                    // if client_id or client_secret are invalid, this action won't be invoked.


                    // Tạo token bình thường
                    var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                        throw new InvalidOperationException("The application cannot be found.");

                    string password = request.Password!;

                    var user = await userManager.FindByNameAsync(username);
                    if (user == null)
                    {
                        await _auditLogService.Create(new CreateAuditLogDto
                        {
                            Action = AuditLogConstants.ACTION_LOGIN_FAILED,
                            ActionType = AuditLogConstants.ACTION_TYPE_LOGIN,
                            Content = $"Đăng nhập thất bại: tài khoản {username} không tồn tại",
                        });
                        throw new UserFriendlyException(ErrorCodes.NotFound, "Tài khoản không tồn tại");
                    }

                    bool isValid = await userManager.CheckPasswordAsync(user, password);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid password attempt for user: {Username}", username);
                        await _auditLogService.Create(new CreateAuditLogDto
                        {
                            UserId = user.Id,
                            Action = AuditLogConstants.ACTION_LOGIN_FAILED,
                            ActionType = AuditLogConstants.ACTION_TYPE_LOGIN,
                            Content = $"Đăng nhập thất bại: tài khoản {username} nhập sai mật khẩu",
                        });
                        throw new UserFriendlyException(ErrorCodes.AuthInvalidPassword, "Mật khẩu không chính xác");
                    }

                    // Use the client_id as the subject identifier.
                    identity.SetClaim(Claims.Subject, user.Id);
                    identity.SetClaim(Claims.Name, user.FullName);
                    identity.SetClaim(Claims.Username, user.UserName);
                    identity.SetClaim(CustomClaimTypes.SecurityStamp, await userManager.GetSecurityStampAsync(user));
                    var roles = await _userService.GetRoleNames(user.Id);
                    foreach (var role in roles)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                    identity.SetScopes(
                            new[]
                            {
                            Scopes.OpenId,
                            Scopes.Email,
                            Scopes.Profile,
                            Scopes.Roles,
                            Scopes.OfflineAccess
                            }.Intersect(request.GetScopes())
                        );
                    identity.SetDestinations(claim => claim.Type switch
                    {
                        // Allow the "name" claim to be stored in both the access and identity tokens
                        // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                        Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                            => [Destinations.AccessToken, Destinations.IdentityToken],
                        ClaimTypes.Role => [Destinations.AccessToken, Destinations.IdentityToken],
                        CustomClaimTypes.SecurityStamp => [],
                        // Otherwise, only store the claim in the access tokens.
                        _ => [Destinations.AccessToken]
                    });

                    await _auditLogService.Create(new CreateAuditLogDto
                    {
                        UserId = user.Id,
                        Action = AuditLogConstants.ACTION_LOGIN,
                        ActionType = AuditLogConstants.ACTION_TYPE_LOGIN,
                        Content = $"Tài khoản {username} đăng nhập thành công",
                    });

                    _logger.LogInformation("Password grant successful for user: {Username}", username);
                    return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
                else if (request.IsRefreshTokenGrantType())
                {
                    _logger.LogInformation("Processing refresh token grant");

                    var result = await HttpContext.AuthenticateAsync(
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                    );

                    string userid = result.Principal!.GetClaim(Claims.Subject)!;
                    string username = result.Principal!.GetClaim(Claims.Username)!;

                    var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                        throw new InvalidOperationException("The application cannot be found.");

                    var user = await userManager.FindByIdAsync(userid)
                        ?? throw new UserFriendlyException(ErrorCodes.NotFound, "Tài khoản không tồn tại");

                    if (await userManager.IsLockedOutAsync(user))
                    {
                        throw new UserFriendlyException(ErrorCodes.Unauthorized, "Tài khoản đã bị khóa");
                    }

                    if (result.Principal!.GetClaim(CustomClaimTypes.SecurityStamp) != await userManager.GetSecurityStampAsync(user))
                    {
                        throw new UserFriendlyException(ErrorCodes.Unauthorized, "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại");
                    }

                    _logger.LogInformation("Refresh token grant for user: {UserId}", user.Id);

                    // Use the client_id as the subject identifier.
                    identity.SetClaim(Claims.Subject, user.Id);
                    identity.SetClaim(Claims.Name, user.FullName);
                    identity.SetClaim(Claims.Username, user.UserName);
                    identity.SetClaim(CustomClaimTypes.SecurityStamp, await userManager.GetSecurityStampAsync(user));
                    var roles = await _userService.GetRoleNames(user.Id);
                    foreach (var role in roles)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                    identity.SetScopes(
                            new[]
                            {
                            Scopes.OpenId,
                            Scopes.Email,
                            Scopes.Profile,
                            Scopes.Roles,
                            Scopes.OfflineAccess
                            }.Intersect(result.Principal!.GetScopes())
                        );
                    identity.SetDestinations(claim => claim.Type switch
                    {
                        // Allow the "name" claim to be stored in both the access and identity tokens
                        // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                        Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                            => [Destinations.AccessToken, Destinations.IdentityToken],
                        ClaimTypes.Role => [Destinations.AccessToken, Destinations.IdentityToken],
                        CustomClaimTypes.SecurityStamp => [],
                        // Otherwise, only store the claim in the access tokens.
                        _ => [Destinations.AccessToken]
                    });

                    _logger.LogInformation("Refresh token grant successful for user: {UserId}", user.Id);
                    return SignIn(
                        new ClaimsPrincipal(identity),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                    );
                }
            }
            catch (UserFriendlyException ex)
            {
                _logger.LogWarning("User friendly exception in token exchange: {Message}", ex.MessageLocalize);

                var properties = new AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            ex.MessageLocalize
                    }
                );
                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in token exchange");

                var properties = new AuthenticationProperties(
                   new Dictionary<string, string?>
                   {
                       [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                           Errors.InvalidGrant,
                       [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                           ex.Message
                   }
               );
                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            _logger.LogWarning("Unsupported grant type: {GrantType}", request.GrantType);
            return BadRequest(
                   new OpenIddictResponse
                   {
                       Error = Errors.UnsupportedGrantType,
                       ErrorDescription = "The specified grant type is not supported."
                   }
               );
        }

        [HttpGet("~/connect/authorize")]
        public async Task<IActionResult> ConnectAuthorize([FromServices] UserManager<AppUser> userManager, string returnUrl = "/")
        {
            _logger.LogInformation("Authorization request received. Return URL: {ReturnUrl}", returnUrl);

            var request = HttpContext.GetOpenIddictServerRequest()
                  ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");


            if(User.Identity?.IsAuthenticated != true)
            {
                _logger.LogInformation("User is not authenticated, redirecting to login page.");
                return Challenge(
                    new AuthenticationProperties { 
                    RedirectUri = Request.PathBase + Request.Path + Request.QueryString
                    },CookieAuthenticationDefaults.AuthenticationScheme);


            }

            _logger.LogInformation("User authenticated, processing authorization for user: {UserId}", User.FindFirst(Claims.Subject)?.Value);

            // At this point, the user info is already in cookie (from ExternalCallback)
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // Copy claims from cookie identity into OpenIddict identity
            foreach (var claim in User.Claims)
            {
                identity.AddClaim(new Claim(claim.Type, claim.Value));
            }

            // Use the client_id as the subject identifier.
            //identity.SetClaim(Claims.Subject, "test");
            //identity.SetClaim(Claims.Name, "nghia test");

            identity.SetDestinations(claim => claim.Type switch
            {
                // Allow the "name" claim to be stored in both the access and identity tokens
                // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                    => [Destinations.AccessToken, Destinations.IdentityToken],
                CustomClaimTypes.UserType when true
                => [Destinations.AccessToken, Destinations.IdentityToken],
                ClaimTypes.Role => [Destinations.AccessToken, Destinations.IdentityToken],
                // Otherwise, only store the claim in the access tokens.
                _ => [Destinations.AccessToken]
            });

            var principal = new ClaimsPrincipal(identity);

            _logger.LogInformation("Authorization successful, issuing tokens for user: {UserId}", User.FindFirst(Claims.Subject)?.Value);

            // ✅ Tell OpenIddict to issue tokens
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
