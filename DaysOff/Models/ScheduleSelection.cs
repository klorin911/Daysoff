namespace DaysOff.Models;

public sealed record ScheduleSelection(
    ScheduleType ScheduleType,
    ShiftType ShiftType,
    DateOnly StartDate,
    PlatoonDaysOffOption PlatoonDaysOff,
    RotatingOffStartDay RotatingOffStartDay
);
