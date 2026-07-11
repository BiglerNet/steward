namespace Steward.Application.VinDecode;

public interface IVinDecodeService
{
    Task<VinDecodeResult> DecodeAsync(string vin, CancellationToken cancellationToken = default);
}
