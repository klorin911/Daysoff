using DaysOff.Models;

namespace DaysOff.Data.Entities;

public sealed class UserScheduleSelection
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public ScheduleType ScheduleType { get; set; }
    public ShiftType ShiftType { get; set; }
    public DateOnly StartDate { get; set; }
    public PlatoonDaysOffOption PlatoonDaysOff { get; set; }
    public RotatingOffStartDay RotatingOffStartDay { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
