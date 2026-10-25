namespace Api.Contracts.Messages;

public sealed record MessageAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);