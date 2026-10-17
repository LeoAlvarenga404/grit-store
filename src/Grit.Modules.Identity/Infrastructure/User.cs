using Microsoft.AspNetCore.Identity;

namespace Grit.Modules.Identity.Infrastructure;

// Outside Domain/ because it derives from the framework; Domain/ depends only on the SharedKernel.
internal sealed class User : IdentityUser<Guid>;
