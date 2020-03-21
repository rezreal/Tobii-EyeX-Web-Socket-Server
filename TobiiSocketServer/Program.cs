using Fleck;
using System;
using Tobii.Interaction;
using Tobii.Interaction.Client;

namespace TobiiSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {

            int port = 8887;
            string address = "127.0.0.1";
            if (args.Length == 1)
            {
                try
                {
                    port = Int32.Parse(args[0]);
                }
                catch (Exception e)
                {
                }
            }
            try
            {

                switch (Host.EyeXAvailability)
                {
                    case EyeXAvailability.NotAvailable:
                        FleckLog.Error("This sample requires the EyeX Engine, but it isn't available.\nPlease install the EyeX Engine and try again.");
                        return;

                    case EyeXAvailability.NotRunning:
                        FleckLog.Error("This sample requires the EyeX Engine, but it isn't rnning.\nPlease make sure that the EyeX Engine is started.");
                        return;
                }


                var host = new Host();


                var server = new SocketServer(port, address, host);
                server.start();
            }
            catch (Exception e)
            {
                FleckLog.Error("Failed to start server on port " + port.ToString() + ": " + e.Message);
            }

            while (true) { Console.ReadLine(); }
        }
    }
}
