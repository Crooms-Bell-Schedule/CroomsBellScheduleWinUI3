using CroomsBellScheduleCS.Service.Web;

namespace CroomsBellScheduleCS.Service
{
    public class Services
    {
        public static readonly ApiClient ApiClient = new();
        public static readonly NotificationManager NotificationManager = new();
    }
}
