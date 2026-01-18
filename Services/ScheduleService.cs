using DaysOff.Models;

namespace DaysOff.Services;

public sealed class ScheduleService
{
    private readonly Employee _employee;
    private readonly List<ShiftDifferential> _differentials;
    private ScheduleSelection _selection;

    public ScheduleService()
    {
        _employee = new Employee(Guid.NewGuid(), "Alex Morgan", "Operations", 26.50m);

        _differentials = new List<ShiftDifferential>
        {
            new(ShiftType.Swing, 1.50m),
            new(ShiftType.Mid, 2.00m)
        };

        _selection = new ScheduleSelection(
            ScheduleType.FiveByEight,
            ShiftType.Day,
            new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1),
            PlatoonDaysOffOption.MonTue,
            RotatingOffStartDay.Monday);
    }

    public Employee GetEmployee() => _employee;

    public ScheduleSelection GetSelection() => _selection;

    public void UpdateSelection(ScheduleSelection selection)
    {
        _selection = selection;
    }

    public WeeklyScheduleSummary BuildWeeklySummary(DateOnly weekStart)
    {
        var days = BuildWeekSchedule(weekStart, _selection.StartDate).Days;
        var totalHours = days.Where(day => day.IsWorkDay).Sum(day => day.Hours);
        var basePay = totalHours * _employee.BaseRate;
        var differentialRate = _differentials.FirstOrDefault(d => d.ShiftType == _selection.ShiftType)?.RatePerHour ?? 0m;
        var differentialPayTotal = totalHours * differentialRate;
        var totalPay = basePay + differentialPayTotal;

        var shiftHours = new List<ShiftHoursSummary>
        {
            new(_selection.ShiftType, totalHours, differentialRate, differentialPayTotal)
        };

        return new WeeklyScheduleSummary(
            weekStart,
            shiftHours,
            totalHours,
            basePay,
            differentialPayTotal,
            totalPay);
    }

    public EmployeePayrollSummary BuildPayrollSummary(DateOnly weekStart)
    {
        var summary = BuildWeeklySummary(weekStart);
        return new EmployeePayrollSummary(
            _employee.Id,
            _employee.Name,
            _employee.Department,
            weekStart,
            summary.TotalHours,
            summary.BasePay,
            summary.DifferentialPay,
            summary.TotalPay);
    }

    public string BuildPayrollCsv(DateOnly weekStart)
    {
        var summary = BuildPayrollSummary(weekStart);
        var lines = new List<string>
        {
            "Employee,Department,WeekStart,TotalHours,BasePay,DifferentialPay,TotalPay"
        };

        lines.Add(string.Join(',',
            Escape(summary.EmployeeName),
            Escape(summary.Department),
            summary.WeekStart.ToString("yyyy-MM-dd"),
            summary.TotalHours.ToString("0.##"),
            summary.BasePay.ToString("0.00"),
            summary.DifferentialPay.ToString("0.00"),
            summary.TotalPay.ToString("0.00")));

        return string.Join(Environment.NewLine, lines);
    }

    public IReadOnlyList<WeekSchedule> BuildYearSchedule(DateOnly startDate)
    {
        var endDate = new DateOnly(startDate.Year, 12, 31);
        var weekStart = GetWeekStart(startDate);
        var weeks = new List<WeekSchedule>();

        while (weekStart <= endDate)
        {
            weeks.Add(BuildWeekSchedule(weekStart, startDate));
            weekStart = weekStart.AddDays(7);
        }

        return weeks;
    }

    private WeekSchedule BuildWeekSchedule(DateOnly weekStart, DateOnly startDate)
    {
        var days = new List<ScheduleDay>(7);
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            var day = BuildScheduleDay(date, startDate);
            days.Add(day);
        }

        return new WeekSchedule(weekStart, days);
    }

    private ScheduleDay BuildScheduleDay(DateOnly date, DateOnly startDate)
    {
        if (date < startDate)
        {
            return new ScheduleDay(date, false, false, _selection.ShiftType, default, default, 0m);
        }

        var isWorkDay = _selection.ScheduleType switch
        {
            ScheduleType.FiveByEight => IsFiveByEightWorkDay(date),
            ScheduleType.PlatoonTen => IsPlatoonWorkDay(date, startDate, _selection.PlatoonDaysOff),
            ScheduleType.RotatingFourByTen => IsRotatingWorkDay(date, startDate, _selection.RotatingOffStartDay),
            _ => false
        };

        var hours = _selection.ScheduleType == ScheduleType.FiveByEight ? 8m : 10m;
        var (start, end) = GetShiftTimes(_selection.ShiftType, hours);
        return new ScheduleDay(date, true, isWorkDay, _selection.ShiftType, start, end, isWorkDay ? hours : 0m);
    }

    private static bool IsFiveByEightWorkDay(DateOnly date)
    {
        var dayIndex = GetDayIndex(date);
        return dayIndex <= 4;
    }

    private static bool IsPlatoonWorkDay(DateOnly date, DateOnly startDate, PlatoonDaysOffOption daysOff)
    {
        var dayIndex = GetDayIndex(date);
        if (dayIndex == 2)
        {
            return true;
        }

        var usesMonTueOff = daysOff == PlatoonDaysOffOption.MonTue;
        var isWeekdayWork = usesMonTueOff
            ? dayIndex is 3 or 4
            : dayIndex is 0 or 1;

        if (dayIndex <= 4)
        {
            return isWeekdayWork;
        }

        var startWeek = GetWeekStart(startDate);
        var weekIndex = (GetWeekStart(date).DayNumber - startWeek.DayNumber) / 7;
        var weekendOnAtStart = GetDayIndex(startDate) >= 5;
        var weekendOn = weekendOnAtStart ? weekIndex % 2 == 0 : weekIndex % 2 == 1;
        return weekendOn;
    }

    private static bool IsRotatingWorkDay(DateOnly date, DateOnly startDate, RotatingOffStartDay offStartDay)
    {
        var monthIndex = (date.Year - startDate.Year) * 12 + (date.Month - startDate.Month);
        var nextMonthFirst = new DateOnly(date.Year, date.Month, 1).AddMonths(1);
        var rotationChangeWeekStart = GetWeekStart(nextMonthFirst);
        if (date >= rotationChangeWeekStart)
        {
            monthIndex += 1;
        }

        var baseRotationStartIndex = Mod(GetDayIndex(offStartDay) - 4, 7);
        var rotationStartIndex = (baseRotationStartIndex + monthIndex) % 7;
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        var weekAnchor = GetWeekStart(monthStart);
        var offsetFromAnchor = date.DayNumber - weekAnchor.DayNumber;
        var offsetFromRotation = Mod(offsetFromAnchor - rotationStartIndex, 7);
        return offsetFromRotation <= 3;
    }

    private static (TimeOnly Start, TimeOnly End) GetShiftTimes(ShiftType shiftType, decimal hours)
    {
        var start = shiftType switch
        {
            ShiftType.Day => new TimeOnly(7, 0),
            ShiftType.Swing => new TimeOnly(15, 0),
            ShiftType.Mid => new TimeOnly(23, 0),
            _ => new TimeOnly(7, 0)
        };

        var end = start.AddHours((double)hours);
        return (start, end);
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var offset = (GetDayIndex(date) + 7) % 7;
        return date.AddDays(-offset);
    }

    private static int GetDayIndex(DateOnly date)
    {
        return ((int)date.DayOfWeek + 6) % 7;
    }

    private static int GetDayIndex(RotatingOffStartDay day)
    {
        return day switch
        {
            RotatingOffStartDay.Monday => 0,
            RotatingOffStartDay.Tuesday => 1,
            RotatingOffStartDay.Wednesday => 2,
            RotatingOffStartDay.Thursday => 3,
            RotatingOffStartDay.Friday => 4,
            RotatingOffStartDay.Saturday => 5,
            RotatingOffStartDay.Sunday => 6,
            _ => 0
        };
    }

    private static int Mod(int value, int modulo)
    {
        var result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
