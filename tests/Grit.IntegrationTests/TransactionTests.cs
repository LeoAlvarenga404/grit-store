using Grit.BuildingBlocks.Behaviors;
using Grit.Modules.Probe;
using Grit.Modules.Probe.Contracts;
using Grit.Modules.Probe.Domain;
using Grit.Modules.Probe.Features.CreateNote;
using Grit.Modules.Probe.Infrastructure;
using Grit.SharedKernel;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

[Collection("Outbox")]
public class TransactionTests(ApiFactory factory)
{
    private readonly string _text = $"transaction-{Guid.NewGuid():N}";
    private readonly Guid _messageId = Guid.NewGuid();

    [Fact]
    public async Task Command_Succeeds_CommitsNoteAndOutboxRows()
    {
        var result = await SaveNoteThen(noteId => Task.FromResult(Result.Success(noteId)));

        result.IsSuccess.ShouldBeTrue();
        (await CountRows()).ShouldBe((1, 1, 1));
    }

    [Fact]
    public async Task Command_FailsAfterSaving_RollsBackNoteAndOutboxRows()
    {
        var result = await SaveNoteThen(_ => Task.FromResult(Result.Failure<Guid>(ProbeErrors.DuplicateText)));

        result.IsFailure.ShouldBeTrue();
        (await CountRows()).ShouldBe((0, 0, 0));
    }

    [Fact]
    public async Task Command_ThrowsAfterSaving_RollsBackNoteAndOutboxRows()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => SaveNoteThen(_ => throw new InvalidOperationException("Bug after saving.")));

        (await CountRows()).ShouldBe((0, 0, 0));
    }

    private async Task<Result<Guid>> SaveNoteThen(Func<Guid, Task<Result<Guid>>> finish)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProbeDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var behavior = new TransactionBehavior<CreateNoteCommand, Result<Guid>>();

        return await behavior.Handle(new CreateNoteCommand(_text), async cancellationToken =>
        {
            var note = Note.Create(_text);
            db.Notes.Add(note);
            await publisher.Publish(new NoteCreated(note.Id.Value, _text), context => context.MessageId = _messageId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return await finish(note.Id.Value);
        }, TestContext.Current.CancellationToken);
    }

    // A new scope and DbContext, so only committed rows are counted.
    private async Task<(int Notes, int OutboxMessages, int OutboxStates)> CountRows()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProbeDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;

        var notes = await db.Notes.CountAsync(note => note.Text == _text, cancellationToken);
        var outboxIds = await db.Set<OutboxMessage>()
            .Where(message => message.MessageId == _messageId && message.OutboxId != null)
            .Select(message => message.OutboxId!.Value)
            .ToListAsync(cancellationToken);
        var outboxStates = await db.Set<OutboxState>().CountAsync(state => outboxIds.Contains(state.OutboxId), cancellationToken);
        return (notes, outboxIds.Count, outboxStates);
    }
}
