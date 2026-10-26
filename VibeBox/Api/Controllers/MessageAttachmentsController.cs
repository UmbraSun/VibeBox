
using Application.Features.Messages.Queries.GetMessageAttachment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Api.Controllers;

/// <summary>
/// API controller for managing message attachments, including downloading attachment content.
/// </summary>
[ApiController]
[Route("api/attachments")]
[Authorize]
public sealed class MessageAttachmentsController : ControllerBase
{
    private readonly ISender _sender;

    public MessageAttachmentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns the specified message attachment as a file result.
    /// </summary>
    /// <param name="attachmentId">Unique identifier of the attachment to download.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A file result containing the attachment content if found; otherwise, an appropriate error response.</returns>
    [HttpGet("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await _sender.Send(new GetMessageAttachmentQuery(attachmentId), cancellationToken);

        Response.Headers[HeaderNames.ContentDisposition] = new ContentDispositionHeaderValue("inline") { FileNameStar = attachment.FileName }.ToString();

        return File(attachment.Content, attachment.ContentType);
    }
}
