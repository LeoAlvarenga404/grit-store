using Grit.BuildingBlocks;
using Grit.BuildingBlocks.Behaviors;
using Grit.Modules.Probe.Features.CreateNote;
using Grit.SharedKernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class PipelineTests(ApiFactory factory)
{
    [Fact]
    public void CreateNote_RunsLoggingValidationAndTransactionInOrder()
    {
        using var scope = factory.Services.CreateScope();

        var behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<CreateNoteCommand, Result<Guid>>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
            .ToList();

        behaviors.ShouldBe([typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>), typeof(TransactionBehavior<,>)]);
        typeof(CreateNoteCommand).IsAssignableTo(typeof(ICommand)).ShouldBeTrue();
    }
}
