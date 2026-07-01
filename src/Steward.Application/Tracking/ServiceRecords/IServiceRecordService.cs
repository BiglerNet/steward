namespace Steward.Application.Tracking.ServiceRecords;

public interface IServiceRecordService
{
    Task<ServiceRecordResponse> CreateAsync(
        Guid assetId, CreateServiceRecordRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ServiceRecordResponse>> ListAsync(
        Guid assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<ServiceRecordResponse> UpdateAsync(
        Guid assetId,
        Guid serviceRecordId,
        UpdateServiceRecordRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid assetId, Guid serviceRecordId, CancellationToken cancellationToken = default);
}
