using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sh.be.shared.HttpRequest.AppException;
using sh.be.shared.HttpRequest.Error;

namespace sh.be.shared.HttpRequest.Exception
{
    public class UserFriendlyException : BaseException
    {
        public UserFriendlyException(int errorCode) : base(errorCode)
        {
        }

        public UserFriendlyException(int errorCode, string? messsage) : base(errorCode, messsage)
        {
        }

        public override string Message => string.IsNullOrWhiteSpace(MessageLocalize) ? ErrorMessages.GetMessage(ErrorCode) : MessageLocalize;
    }
}
