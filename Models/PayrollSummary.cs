namespace DaysOff.Models;

public sealed record ShiftHoursSummary(
    ShiftType ShiftType,
    decimal Hours,
    decimal DifferentialRate,
    decimal DifferentialPay
);

public sealed record WeeklyScheduleSummary(
    DateOnly WeekStart,
    IReadOnlyList<ShiftHoursSummary> ShiftHours,
    decimal TotalHours,
    decimal BasePay,
    decimal DifferentialPay,
    decimal TotalPay
);

public sealed record EmployeePayrollSummary(
    Guid EmployeeId,
    string EmployeeName,
    string Department,
    DateOnly WeekStart,
    decimal TotalHours,
    decimal BasePay,
    decimal DifferentialPay,
    decimal TotalPay
);
