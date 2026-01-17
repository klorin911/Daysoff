namespace DaysOff.Models;

public sealed record ScheduleDay(
    DateOnly Date,
    bool IsActive,
    bool IsWorkDay,
    ShiftType ShiftType,
    TimeOnly Start,
    TimeOnly End,
    decimal Hours
);

public sealed record WeekSchedule(
    DateOnly WeekStart,
    IReadOnlyList<ScheduleDay> Days
);
