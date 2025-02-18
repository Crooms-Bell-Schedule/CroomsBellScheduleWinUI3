using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Provider
{
    public interface IBellScheduleProvider
    {
        abstract Task<BellScheduleReader> GetTodayActivity();
    }
}
