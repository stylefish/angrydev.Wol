using System;

namespace angrydev.Wol
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Usage();
                return;
            }

            var mac = args[0];
            try
            {
                WakeOnLanClient.WakeUp(mac);
                Console.WriteLine("wol packet sent.");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: {e.Message}");
                Console.ResetColor();
            }
        }

        private static void Usage()
        {
            Console.WriteLine("usage: wol <mac address>");
            Console.WriteLine("example: wol ab-cd-ef-fe-dc-ba");
        }
    }
}
