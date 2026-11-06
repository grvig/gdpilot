using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class LitPlacesSizeTests
{
    private static readonly ScreenPoint Place = new(100, 100);

    /// <summary>A red square of the given side at the place, on black.</summary>
    private static Func<int, int, (byte Red, byte Green, byte Blue)> RedSquare(int side)
    {
        return (x, y) =>
        {
            if (Math.Abs(x - Place.X) * 2 < side && Math.Abs(y - Place.Y) * 2 < side)
            {
                return (120, 10, 10);
            }

            return (0, 0, 0);
        };
    }

    [Fact]
    public void AFaintLitPlaceDrawnSmallerStillCounts()
    {
        Assert.False(LitPlaces.Lit(RedSquare(3), Place));
        Assert.True(LitPlaces.Lit(RedSquare(3), Place, 0.7));
    }

    [Fact]
    public void WhatIsFaintAtTheMeasuredSizeIsNothingWhenDrawnLarger()
    {
        Assert.True(LitPlaces.Lit(RedSquare(5), Place));
        Assert.False(LitPlaces.Lit(RedSquare(5), Place, 1.415));
    }

    [Fact]
    public void ALargerInterfaceIsSearchedFurtherOut()
    {
        Func<int, int, (byte Red, byte Green, byte Blue)> beside = (x, y) =>
        {
            if (x >= Place.X + 8 && x <= Place.X + 9 && Math.Abs(y - Place.Y) <= 9)
            {
                return (120, 10, 10);
            }

            return (0, 0, 0);
        };

        Assert.False(LitPlaces.Lit(beside, Place));
        Assert.True(LitPlaces.Lit(beside, Place, 1.415));
    }
}
