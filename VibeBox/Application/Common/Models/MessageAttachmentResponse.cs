namespace Application.Common.Models;

public sealed record MessageAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);