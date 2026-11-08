using System.ComponentModel.DataAnnotations;

namespace Application.Common.Models;

public sealed record CreateGroupConversationRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [Required]
    [MinLength(2)]
    IReadOnlyList<Guid> UserIds);