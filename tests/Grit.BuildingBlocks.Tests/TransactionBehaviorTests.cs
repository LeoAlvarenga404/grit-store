using System.Transactions;
using Grit.BuildingBlocks.Behaviors;
using Grit.SharedKernel;

namespace Grit.BuildingBlocks.Tests;

public class TransactionBehaviorTests
{
    private static readonly Error SomeError = new("some.code", "Some message.", ErrorType.Conflict);

    [Fact]
    public async Task Handle_Command_RunsInsideReadCommittedTransaction()
    {
        IsolationLevel? isolationLevel = null;

        await new TransactionBehavior<TestCommand, Result>().Handle(new TestCommand(), _ =>
        {
            isolationLevel = Transaction.Current?.IsolationLevel;
            return Task.FromResult(Result.Success());
        }, TestContext.Current.CancellationToken);

        isolationLevel.ShouldBe(IsolationLevel.ReadCommitted);
    }

    [Fact]
    public async Task Handle_Query_RunsWithoutTransaction()
    {
        var hadTransaction = true;

        await new TransactionBehavior<TestQuery, Result>().Handle(new TestQuery(), _ =>
        {
            hadTransaction = Transaction.Current is not null;
            return Task.FromResult(Result.Success());
        }, TestContext.Current.CancellationToken);

        hadTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_CommandSucceeds_CommitsTransaction()
    {
        var status = await StatusAfter(() => Task.FromResult(Result.Success()));

        status.ShouldBe(TransactionStatus.Committed);
    }

    [Fact]
    public async Task Handle_CommandFails_RollsBackTransaction()
    {
        var status = await StatusAfter(() => Task.FromResult(Result.Failure(SomeError)));

        status.ShouldBe(TransactionStatus.Aborted);
    }

    [Fact]
    public async Task Handle_CommandThrows_RollsBackTransaction()
    {
        TransactionStatus? status = null;

        await Should.ThrowAsync<InvalidOperationException>(() => new TransactionBehavior<TestCommand, Result>().Handle(new TestCommand(), _ =>
        {
            Transaction.Current!.TransactionCompleted += (_, completed) => status = completed.Transaction!.TransactionInformation.Status;
            throw new InvalidOperationException("Bug.");
        }, TestContext.Current.CancellationToken));

        status.ShouldBe(TransactionStatus.Aborted);
    }

    [Fact]
    public async Task Handle_CommandReturningNonResult_CommitsTransaction()
    {
        TransactionStatus? status = null;

        await new TransactionBehavior<TestCommand, string>().Handle(new TestCommand(), _ =>
        {
            Transaction.Current!.TransactionCompleted += (_, completed) => status = completed.Transaction!.TransactionInformation.Status;
            return Task.FromResult("done");
        }, TestContext.Current.CancellationToken);

        status.ShouldBe(TransactionStatus.Committed);
    }

    private static async Task<TransactionStatus?> StatusAfter(Func<Task<Result>> handler)
    {
        TransactionStatus? status = null;

        await new TransactionBehavior<TestCommand, Result>().Handle(new TestCommand(), _ =>
        {
            Transaction.Current!.TransactionCompleted += (_, completed) => status = completed.Transaction!.TransactionInformation.Status;
            return handler();
        }, TestContext.Current.CancellationToken);

        return status;
    }

    private sealed record TestCommand : ICommand;

    private sealed record TestQuery;
}
