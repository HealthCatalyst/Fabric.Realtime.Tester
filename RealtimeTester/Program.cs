// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the TestSsl type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Realtime.TesterCore
{
    using System;

    using Realtime.Interfaces;
    using Realtime.Tester.Mirth;
    using Realtime.Tester.RabbitMq;

    /// <summary>
    /// The test ssl.
    /// </summary>
    public class TestSsl
    {
        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int Main(string[] args)
        {
            try
            {

                // string mirthhostname = "fabricrealtimerabbitmq.eastus.cloudapp.azure.com";
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

                IRabbitMqListener rabbitMqListener = new RabbitMqListener();

                MirthTester.TestSendingHL7(mirthhostname, rabbitMqListener);

                // var rabbitMqListener = new RabbitMqListener();

                // rabbitMqListener.StartListening(mirthhostname);

                Console.WriteLine("Press Enter to exit");

                Console.ReadLine();

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }


    }
}