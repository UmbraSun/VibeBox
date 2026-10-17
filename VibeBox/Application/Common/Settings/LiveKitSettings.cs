namespace Application.Common.Settings;

public sealed class LiveKitSettings
{
    public const string SectionName = "LiveKit";

    public string Url { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string ApiSecret { get; init; } = string.Empty;
}