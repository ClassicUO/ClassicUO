using ClassicUO.Ecs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Unit tests for the vendor transaction quantity step (NextTxnQty) — the
// money-sensitive +/- math pulled out of AdjustTxn. The click routing /
// network send around it stays in the ECS system; this locks the clamp rules.
public class VendorTests
{
    [Theory]
    [InlineData(0, 5, 1)]   // +1 from empty
    [InlineData(2, 5, 3)]   // +1 mid-stack
    [InlineData(4, 5, 5)]   // +1 to the cap
    public void NextTxnQty_buy_steps_up_by_one(int chosen, int stock, int expected)
        => Assert.Equal(expected, VendorGumpPlugin.NextTxnQty(chosen, stock, dir: 1, shift: false));

    [Fact]
    public void NextTxnQty_buy_at_max_is_unchanged()
        => Assert.Equal(5, VendorGumpPlugin.NextTxnQty(5, 5, dir: 1, shift: false));

    [Theory]
    [InlineData(0, 5, 5)]   // shift-buy grabs the whole stock
    [InlineData(2, 5, 5)]   // ...up to the remaining stock
    public void NextTxnQty_shift_buy_takes_remaining_stock(int chosen, int stock, int expected)
        => Assert.Equal(expected, VendorGumpPlugin.NextTxnQty(chosen, stock, dir: 1, shift: true));

    [Theory]
    [InlineData(3, 2)]      // -1 mid-stack
    [InlineData(1, 0)]      // -1 down to zero (=> remove)
    [InlineData(0, 0)]      // -1 on an absent line stays zero
    public void NextTxnQty_sell_steps_down_flooring_at_zero(int chosen, int expected)
        => Assert.Equal(expected, VendorGumpPlugin.NextTxnQty(chosen, 5, dir: -1, shift: false));

    [Fact]
    public void NextTxnQty_shift_sell_clears_the_line()
        => Assert.Equal(0, VendorGumpPlugin.NextTxnQty(3, 5, dir: -1, shift: true));
}
