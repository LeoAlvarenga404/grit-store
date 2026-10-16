namespace Grit.Modules.Identity.Features.GetCurrentUser;

internal sealed record CurrentUserResponse(Guid Id, string Email, IReadOnlyList<string> Roles);
