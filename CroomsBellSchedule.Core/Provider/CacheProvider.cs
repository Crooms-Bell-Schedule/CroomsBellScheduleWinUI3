using System;
using System.Threading.Tasks;

namespace CroomsBellSchedule.Core.Provider;

public class CacheProvider(IBellScheduleProvider actualProvider) : IBellScheduleProvider
{
    private IBellScheduleProvider _bellScheduleProvider = actualProvider;
    private BellScheduleReader? _cache;
    private int CacheDay;

    public IBellScheduleProvider Provider { get => _bellScheduleProvider; }
    public bool RequiresUpdate => CacheDay != DateTime.Now.DayOfYear;

    public async Task<BellScheduleReader> GetTodayActivity()
    {
        if (_cache == null || RequiresUpdate)
        {
            _cache = await _bellScheduleProvider.GetTodayActivity();
            CacheDay = DateTime.Now.DayOfYear;
            return _cache;
        }
        else return _cache;
    }

    public void SetProvider(IBellScheduleProvider provider)
    {
        _bellScheduleProvider = provider;
        CacheDay = -1;
    }
}