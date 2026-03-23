using System;

namespace NowLink.Service
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using (var host = new ServiceHost())
            {
                host.Start();
                Console.WriteLine("NowLink.Service running. Press Enter to stop.");
                Console.ReadLine();
            }
        }
    }
}
