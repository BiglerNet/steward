namespace Steward.Application.Storage;

public class FileUploadOptions
{
    public const string SectionName = "Storage";

    public string[] AllowedContentTypes { get; set; } =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
    ];

    public long MaxUploadSizeBytes { get; set; } = 10 * 1024 * 1024;

    public long MaxPhotoUploadSizeBytes { get; set; } = 15 * 1024 * 1024;

    public long HouseholdQuotaBytes { get; set; } = 1024L * 1024 * 1024;
}
