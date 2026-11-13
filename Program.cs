using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MonitorCenter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                UdpClient udpServer = new UdpClient(8888);
                Console.WriteLine("=====================================");
                Console.WriteLine("  电教委员监控中心已启动，监听 8888 端口");
                Console.WriteLine("=====================================");
                
                while (true)
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpServer.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    
                    // 收到警报，用红字打印并发出提示音
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[来自 " + remoteEP.Address + "] 捕获警报：" + message);
                    Console.ResetColor();
                    Console.Beep();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("接收失败（可能是端口被占用或防火墙拦截）：" + ex.Message);
                Console.ReadLine();
            }
        }
    }
}