namespace DaysOff.Models;

public sealed record ScheduleRule(
    int DayIndex,
    ShiftType ShiftType,
    TimeOnly Start,
    TimeOnly End
);
