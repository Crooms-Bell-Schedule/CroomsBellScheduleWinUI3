using System.Threading.Tasks;

namespace CroomsBellSchedule.Core.Provider;

public interface IBellScheduleProvider
{
    Task<BellScheduleReader> GetTodayActivity();
}