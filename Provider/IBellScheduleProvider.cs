using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Provider;

public interface IBellScheduleProvider
{
    Task<BellScheduleReader> GetTodayActivity();
}