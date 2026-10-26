namespace Application.Common.Settings;

public sealed class ObjectStorageSettings
{
    public const string SectionName = "ObjectStorage";

    public string ServiceUrl { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-1";
}