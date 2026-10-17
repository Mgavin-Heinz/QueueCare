using System;

namespace QueueCare.Validation
{
    /// <summary>
    /// Rules for a clinic day's daily capacity and opening hours (requirements FR-47 and FR-48).
    /// Kept apart from the controller so they can be unit-tested.
    /// </summary>
    public static class CapacityRules
    {
        /// <param name="capacity">The capacity the admin typed in.</param>
        /// <param name="alreadyUsed">Patients already booked or checked in for that day (BR-01).</param>
        public static ValidationOutcome CheckCapacity(int? capacity, int alreadyUsed)
        {
            if (capacity is null)
            {
                return ValidationOutcome.Fail("Enter the daily capacity.");
            }

            if (capacity.Value < 1)
            {
                return ValidationOutcome.Fail("Capacity must be a whole number greater than 0.");
            }

            if (capacity.Value < alreadyUsed)
            {
                return ValidationOutcome.Fail(
                    $"Capacity cannot be lower than the {alreadyUsed} patient(s) already booked or checked in for this day.");
            }

            return ValidationOutcome.Ok;
        }

        public static ValidationOutcome CheckHours(TimeOnly? open, TimeOnly? close)
        {
            if (open is null || close is null)
            {
                return ValidationOutcome.Fail("Enter the opening and closing times.");
            }

            if (close.Value <= open.Value)
            {
                return ValidationOutcome.Fail("The closing time must be after the opening time.");
            }

            return ValidationOutcome.Ok;
        }

        public static ValidationOutcome CheckDate(DateOnly? date, DateOnly today)
        {
            if (date is null)
            {
                return ValidationOutcome.Fail("Enter the clinic date.");
            }

            if (date.Value < today)
            {
                return ValidationOutcome.Fail("The clinic date cannot be in the past.");
            }

            return ValidationOutcome.Ok;
        }

        /// <summary>Places still free: capacity minus used, never below zero.</summary>
        public static int Remaining(int capacity, int used) => Math.Max(0, capacity - used);
    }
}
