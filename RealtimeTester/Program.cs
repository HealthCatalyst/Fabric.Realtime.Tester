using System;

namespace RealtimeTester
{
    public class TestSSL
    {

        public static int Main(string[] args)
        {
            //string mirthhostname = "fabricrealtimerabbitmq.eastus.cloudapp.azure.com";
            string mirthhostname = "fabricrealtime.eastus.cloudapp.azure.com";

            if (args.Length < 1)
            {
                // host was not passed on the command line
                Console.WriteLine("Enter host to connect to:");
                mirthhostname = Console.ReadLine();
            }
            else
            {
                mirthhostname = args[0];
            }

            Console.WriteLine($"Connecting to host: {mirthhostname}");

            string rabbitmqhostname = mirthhostname;

            RabbitMqTester.TestSecureConnectionToRabbitMq(rabbitmqhostname);

            MirthTester.TestSendingHL7(mirthhostname);

            var rabbitMqListener = new RabbitMqListener();

            rabbitMqListener.StartListening(mirthhostname);

            Console.WriteLine("Press Enter to exit");

            Console.ReadLine();

            return 0;
        }


    }
}