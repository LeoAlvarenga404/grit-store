namespace Grit.SharedKernel.Tests;

public class AggregateRootTests
{
    [Fact]
    public void Constructor_SetsId()
    {
        var id = Guid.NewGuid();

        var aggregate = new TestAggregate(id);

        aggregate.Id.ShouldBe(id);
    }

    [Fact]
    public void NewAggregate_HasNoDomainEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Raise_KeepsEventsInOrder()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var first = new TestEvent(1);
        var second = new TestEvent(2);

        aggregate.Record(first);
        aggregate.Record(second);

        aggregate.DomainEvents.ShouldBe([first, second]);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.Record(new TestEvent(1));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_IsReadOnlyForCallers()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        var events = aggregate.DomainEvents.ShouldBeAssignableTo<ICollection<IDomainEvent>>();

        events.IsReadOnly.ShouldBeTrue();
    }

    private sealed record TestEvent(int Number) : IDomainEvent;

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Record(IDomainEvent domainEvent) => Raise(domainEvent);
    }
}
