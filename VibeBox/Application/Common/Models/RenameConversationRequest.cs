using System.ComponentModel.DataAnnotations;

namespace Application.Common.Models;

public sealed record RenameConversationRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name);