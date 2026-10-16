using Grit.BuildingBlocks;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Identity.Features.Register;

internal sealed record RegisterCommand(string Email, string Password) : IRequest<Result<RegisterResponse>>, ICommand;
