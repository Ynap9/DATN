using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sh.be.shared.HttpRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.api.Base
{
    [ApiController]
    public class BaseController : Controller
    {
        private const int DeadlockErrorNumber = 1205;

        private readonly ILogger<BaseController> _logger;

        public BaseController(ILogger<BaseController> logger)
        {
            _logger = logger;
        }
        [NonAction]
        public ApiResponse OkException(Exception ex)
        {
            //var mapErrorCode = HttpContext.RequestServices.GetRequiredService<IMapErrorCode>();
            //var localization = HttpContext.RequestServices.GetRequiredService<LocalizationBase>();

            var request = HttpContext.Request;
            string errStr =
                $"Path = {request.Path}, Query = {JsonSerializer.Serialize(request.Query)}";
            int errorCode;
            string message = ex.Message;
            object? data = null;

            if (ex is UserFriendlyException userFriendlyException)
            {
                errorCode = userFriendlyException.ErrorCode;
                try
                {
                    if (!string.IsNullOrWhiteSpace(userFriendlyException.MessageLocalize))
                    {
                        message = userFriendlyException.MessageLocalize;
                    }
                    else
                    {
                        message = ErrorMessages.GetMessage(errorCode);
                    }
                }
                catch (Exception exc)
                {
                    message = exc.Message;
                }
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
            }
            else if (IsDeadlockVictim(ex))
            {
                errorCode = ErrorCodes.ConcurrencyConflict;
                message = ErrorMessages.GetMessage(errorCode);
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
            }
            else if (ex is DbUpdateException dbUpdateException)
            {
                errorCode = ErrorCodes.InternalServerError;
                if (dbUpdateException.InnerException != null)
                {
                    message = dbUpdateException.InnerException.Message;
                }
                else
                {
                    message = dbUpdateException.Message;
                }
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
            }
            else if (ex is InvalidOperationException invalidOperationException)
            {
                errorCode = ErrorCodes.InternalServerError;
                if (invalidOperationException.InnerException != null)
                {
                    message = invalidOperationException.InnerException.Message;
                }
                else
                {
                    message = invalidOperationException.Message;
                }
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
            }
            else
            {
                errorCode = ErrorCodes.InternalServerError;
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
                message = "Internal server error";
            }
            _logger?.LogError(
                ex,
                $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}");

            return new ApiResponse(StatusCodeE.Error, data, errorCode, message);
        }

        // SQL Server chọn request này làm nạn nhân deadlock (1205, có thể nằm trong DbUpdateException): giao dịch đã rollback, báo thử lại thay vì lỗi hệ thống
        private static bool IsDeadlockVictim(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is SqlException { Number: DeadlockErrorNumber })
                {
                    return true;
                }
            }
            return false;
        }
    }
}
