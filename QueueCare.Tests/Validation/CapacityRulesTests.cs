using System;
using QueueCare.Validation;
using Xunit;

namespace QueueCare.Tests.Validation;

// Requirements FR-47 and FR-48 (test cases TC-047 and TC-048 in the test workbook).
public class CapacityRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 12);

    // ---- capacity (FR-47, FR-48, BR-01) ----

    [Fact]
    public void CheckCapacity_Empty_IsRejected()
    {
        ValidationOutcome result = CapacityRules.CheckCapacity(null, 0);

        Assert.False(result.IsValid);
        Assert.Contains("Enter the daily capacity", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CheckCapacity_ZeroOrNegative_IsRejected(int capacity)
    {
        ValidationOutcome result = CapacityRules.CheckCapacity(capacity, 0);

        Assert.False(result.IsValid);
        Assert.Contains("greater than 0", result.ErrorMessage);
    }

    [Fact]
    public void CheckCapacity_LowerThanAlreadyBooked_IsRejected()
    {
        // 3 patients are booked, so the admin may not lower the capacity to 2.
        ValidationOutcome result = CapacityRules.CheckCapacity(2, 3);

        Assert.False(result.IsValid);
        Assert.Contains("lower than the 3", result.ErrorMessage);
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(10, 3)]
    [InlineData(1, 0)]
    public void CheckCapacity_AtLeastTheNumberAlreadyBooked_IsAccepted(int capacity, int used)
    {
        ValidationOutcome result = CapacityRules.CheckCapacity(capacity, used);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    // ---- opening hours ----

    [Theory]
    [InlineData(16, 0, 8, 0)]
    [InlineData(8, 0, 8, 0)]
    public void CheckHours_CloseNotAfterOpen_IsRejected(int openHour, int openMinute, int closeHour, int closeMinute)
    {
        ValidationOutcome result = CapacityRules.CheckHours(
            new TimeOnly(openHour, openMinute), new TimeOnly(closeHour, closeMinute));

        Assert.False(result.IsValid);
        Assert.Contains("closing time must be after", result.ErrorMessage);
    }

    [Fact]
    public void CheckHours_CloseAfterOpen_IsAccepted()
    {
        Assert.True(CapacityRules.CheckHours(new TimeOnly(8, 0), new TimeOnly(16, 0)).IsValid);
    }

    [Fact]
    public void CheckHours_Empty_IsRejected()
    {
        Assert.False(CapacityRules.CheckHours(null, new TimeOnly(16, 0)).IsValid);
        Assert.False(CapacityRules.CheckHours(new TimeOnly(8, 0), null).IsValid);
    }

    // ---- date ----

    [Fact]
    public void CheckDate_Empty_IsRejected()
    {
        Assert.False(CapacityRules.CheckDate(null, Today).IsValid);
    }

    [Fact]
    public void CheckDate_InThePast_IsRejected()
    {
        ValidationOutcome result = CapacityRules.CheckDate(Today.AddDays(-1), Today);

        Assert.False(result.IsValid);
        Assert.Contains("cannot be in the past", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    public void CheckDate_TodayOrLater_IsAccepted(int daysFromToday)
    {
        Assert.True(CapacityRules.CheckDate(Today.AddDays(daysFromToday), Today).IsValid);
    }

    // ---- places left ----

    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(5, 5, 0)]
    [InlineData(5, 8, 0)]
    public void Remaining_IsCapacityMinusUsed_NeverBelowZero(int capacity, int used, int expected)
    {
        Assert.Equal(expected, CapacityRules.Remaining(capacity, used));
    }
}
