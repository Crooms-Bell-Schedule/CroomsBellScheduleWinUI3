using CroomsBellSchedule.Core.Service.Web;

namespace CroomsBellSchedule.Service
{
    public class Services
    {
        public static readonly ApiClient ApiClient = new();
        public static readonly SocketClient SocketClient = new();
        public static readonly NotificationManager NotificationManager = new();
    }
}
