namespace DaysOff.Models;

public sealed record ScheduleTemplate(
    Guid Id,
    string Name,
    IReadOnlyList<ScheduleRule> Rules
);
