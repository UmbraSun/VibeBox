using System.ComponentModel.DataAnnotations;

namespace Application.Common.Models;

public sealed record AddConversationMembersRequest(
    [Required]
    [MinLength(1)]
    IReadOnlyList<Guid> UserIds);