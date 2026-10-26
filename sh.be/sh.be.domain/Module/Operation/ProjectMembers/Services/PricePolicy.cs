using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Operation.ProjectMembers.Services
{
    public static class PricePolicy
    {
        public static void EnsureValid(decimal companyPrice, decimal actualPrice)
        {
            if (companyPrice < 0 || companyPrice > PriceConst.PRICE_MAX || actualPrice < 0 || actualPrice > companyPrice)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorPriceInvalid);
            }
        }
    }
}
