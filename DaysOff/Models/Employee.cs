namespace DaysOff.Models;

public sealed record Employee(
    Guid Id,
    string Name,
    string Department,
    decimal BaseRate
);
