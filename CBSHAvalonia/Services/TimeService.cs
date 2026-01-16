using CroomsBellSchedule.Service;
using System;
using System.Net;
using System.Net.Sockets;

namespace CBSHAvalonia.Service
{
    public class TimeService
    {
        /// <summary>
        /// How much behind/ahead the device's clock is. Positive: behind, negative: ahead
        /// </summary>
        public static TimeSpan ClientDelay { get; private set; }
        public static bool IsTimeWrong { get; private set; }
        public static DateTime Now
        {
            get
            {
                if (SettingsManager.Settings.DisableNTPTimeSync) return DateTime.Now;

                return DateTime.Now.Subtract(ClientDelay);
            }
        }
        private const int AcceptableLimit = 5; // 5 seconds

        public static void Sync()
        {
            if (SettingsManager.Settings.DisableNTPTimeSync) return;
            if (!OperatingSystem.IsWindows()) return;

            //if (!Win32.HasNetworkAccessAndIsUnrestricted()) return;
            //AVALONIA TODO^

            // Figure out how much behind the devices the time is
            ClientDelay = GetNetworkTimeDifference();

            // if its behind or ahead by more than AcceptableLimit seconds, use network time
            // don't use network time as it could be inaccurate
            if (ClientDelay.TotalSeconds > AcceptableLimit || ClientDelay.TotalSeconds < -AcceptableLimit)
            {
                IsTimeWrong = true;
            }
        }

        public static TimeSpan GetNetworkTimeDifference()
        {
            DateTime clientTime;
            //default Windows time server
            const string ntpServer = "time.windows.com";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);

                clientTime = DateTime.Now;
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime() - DateTime.Now;
        }

        // stackoverflow.com/a/3294698/162671
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }

        public static string GetOffsetString() => ClientDelay.TotalSeconds > 0
                ? $"{(int)ClientDelay.TotalSeconds} seconds behind"
                : $"{(int)ClientDelay.TotalSeconds} seconds ahead";
    }
}
