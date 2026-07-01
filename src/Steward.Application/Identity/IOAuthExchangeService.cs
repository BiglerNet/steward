namespace Steward.Application.Identity;

public interface IOAuthExchangeService
{
    string GenerateCode(Guid userId);

    bool TryRedeemCode(string code, out Guid userId);
}
