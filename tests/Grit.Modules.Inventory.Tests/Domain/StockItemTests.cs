using Grit.Modules.Inventory.Domain;

namespace Grit.Modules.Inventory.Tests.Domain;

public class StockItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _order = Guid.NewGuid();
    private readonly Guid _otherOrder = Guid.NewGuid();

    [Fact]
    public void Reserve_EveryAvailableUnit_LeavesNoneAvailable()
    {
        var item = StockItem.Create(Guid.NewGuid(), 2);

        item.Reserve(_order, 2, Now).IsSuccess.ShouldBeTrue();

        item.Available(Now).ShouldBe(0);
        item.OnHand.ShouldBe(2);
    }

    [Fact]
    public void Reserve_NotEnoughStock_ReturnsInsufficientStock()
    {
        var item = StockItem.Create(Guid.NewGuid(), 1);

        var result = item.Reserve(_order, 2, Now);

        result.Error.Code.ShouldBe("inventory.insufficient_stock");
        item.Reservations.ShouldBeEmpty();
    }

    [Fact]
    public void Reserve_SameOrderTwice_ReservesOnce()
    {
        var item = StockItem.Create(Guid.NewGuid(), 5);

        item.Reserve(_order, 2, Now);
        item.Reserve(_order, 2, Now).IsSuccess.ShouldBeTrue();

        item.Reservations.Count.ShouldBe(1);
        item.Available(Now).ShouldBe(3);
    }

    [Theory]
    [InlineData(14, 0)]
    [InlineData(15, 1)]
    public void Available_MinutesAfterReserving_CountsTheReservationOnlyWithinFifteenMinutes(int minutesLater, int expectedAvailable)
    {
        var item = StockItem.Create(Guid.NewGuid(), 1);
        item.Reserve(_order, 1, Now);

        item.Available(Now.AddMinutes(minutesLater)).ShouldBe(expectedAvailable);
    }

    [Fact]
    public void Reserve_AfterAnotherReservationExpired_Succeeds()
    {
        var item = StockItem.Create(Guid.NewGuid(), 1);
        item.Reserve(_order, 1, Now);

        item.Reserve(_otherOrder, 1, Now.AddMinutes(15)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Commit_ReservedOrder_HoldsTheUnitsPastTheLifetime()
    {
        var item = StockItem.Create(Guid.NewGuid(), 1);
        item.Reserve(_order, 1, Now);

        item.Commit(_order);

        item.Available(Now.AddHours(1)).ShouldBe(0);
        item.Reservations.Single().IsCommitted.ShouldBeTrue();
    }

    [Fact]
    public void Release_ReservedOrder_FreesTheUnits()
    {
        var item = StockItem.Create(Guid.NewGuid(), 2);
        item.Reserve(_order, 2, Now);

        item.Release(_order);

        item.Available(Now).ShouldBe(2);
        item.Reservations.ShouldBeEmpty();
    }

    [Fact]
    public void Release_UnknownOrder_KeepsOtherReservations()
    {
        var item = StockItem.Create(Guid.NewGuid(), 2);
        item.Reserve(_order, 1, Now);

        item.Release(_otherOrder);

        item.Available(Now).ShouldBe(1);
    }

    [Fact]
    public void CanReserve_OrderAlreadyReserved_IsTrueWithoutFreeUnits()
    {
        var item = StockItem.Create(Guid.NewGuid(), 1);
        item.Reserve(_order, 1, Now);

        item.CanReserve(_order, 1, Now).ShouldBeTrue();
        item.CanReserve(_otherOrder, 1, Now).ShouldBeFalse();
    }
}
