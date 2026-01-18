namespace DaysOff.Models;

public sealed record EmployeeSchedule(
    Guid EmployeeId,
    Guid TemplateId,
    DateOnly EffectiveFrom
);
