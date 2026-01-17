namespace DaysOff.Models;

public sealed record ShiftDifferential(
    ShiftType ShiftType,
    decimal RatePerHour
);
