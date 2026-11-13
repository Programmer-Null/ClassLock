using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClassLock
{
    public static class NetworkHelper
    {
        public static void SendLanAlert(string message)
        {
            try
            {
                UdpClient udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;
                byte[] data = Encoding.UTF8.GetBytes(message);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, 8888);
                udpClient.Send(data, data.Length, ipEndPoint);
                udpClient.Close();
            }
            catch { /* 静默失败，不影响主程序 */ }
        }
    }
}