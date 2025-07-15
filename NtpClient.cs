using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ragaccountmgr
{
    public static class NtpClient
    {
        private const string NtpServer = "pool.ntp.org";
        private const int NtpPort = 123;
        private const int NtpDataLength = 48;
        private const byte NtpVersion = 3;
        private const int NtpTimeout = 3000; // ms

        public static async Task<DateTime?> GetNetworkUtcTimeAsync()
        {
            try
            {
                var ntpData = new byte[NtpDataLength];
                ntpData[0] = 0x1B; // LI = 0, VN = 3, Mode = 3 (client)

                var addresses = await Dns.GetHostAddressesAsync(NtpServer);
                var ipEndPoint = new IPEndPoint(addresses[0], NtpPort);

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.ReceiveTimeout = NtpTimeout;
                    socket.SendTimeout = NtpTimeout;

                    await socket.SendToAsync(new ArraySegment<byte>(ntpData), SocketFlags.None, ipEndPoint);
                    var receiveBuffer = new ArraySegment<byte>(ntpData);
                    await socket.ReceiveAsync(receiveBuffer, SocketFlags.None);
                }

                const byte serverReplyTime = 40;
                ulong intPart = (ulong)ntpData[serverReplyTime] << 24 |
                                (ulong)ntpData[serverReplyTime + 1] << 16 |
                                (ulong)ntpData[serverReplyTime + 2] << 8 |
                                (ulong)ntpData[serverReplyTime + 3];
                ulong fractPart = (ulong)ntpData[serverReplyTime + 4] << 24 |
                                  (ulong)ntpData[serverReplyTime + 5] << 16 |
                                  (ulong)ntpData[serverReplyTime + 6] << 8 |
                                  (ulong)ntpData[serverReplyTime + 7];

                // Convert NTP timestamp to DateTime
                // NTP timestamp is seconds since 1900-01-01 UTC, with fractional seconds
                var seconds = intPart + (fractPart / (double)0x100000000L);
                var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);
                return networkDateTime;
            }
            catch
            {
                return null;
            }
        }
    }
} 