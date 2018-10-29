// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the Program type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Realtime.Tester
{
    using System;

    using Realtime.Interfaces;
    using Realtime.Tester.Certificates.Windows;
    using Realtime.Tester.Mirth;
    using Realtime.Tester.RabbitMq;

    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main(string[] args)
        {
            try
            {

                Console.WriteLine("Realtime tester using .Net Framework 4.6.1");

                // string mirthhostname = "fabricrealtimerabbitmq.eastus.cloudapp.azure.com";
                string mirthhostname = "fabricrealtime.eastus.cloudapp.azure.com";
                string certificatepassword;

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

                mirthhostname = mirthhostname?.Trim();

                if (args.Length < 2)
                {
                    Console.WriteLine("Enter certificate password:");
                    certificatepassword = Console.ReadLine();
                }
                else
                {
                    certificatepassword = args[1];
                }

                certificatepassword = certificatepassword?.Trim();

                Console.WriteLine("--- Installing SSL client certificate ---");

                CertificateManager.InstallCertificate(mirthhostname, true, certificatepassword);

                Console.WriteLine($"--- Connecting to rabbitmq host: {mirthhostname} ---");

                string rabbitmqhostname = mirthhostname;

                RabbitMqTester.TestSecureConnectionToRabbitMq(rabbitmqhostname);

                Console.WriteLine($"--- Listening to message from rabbitmq at host: {rabbitmqhostname} ---");
                IRabbitMqListener rabbitMqListener = new RabbitMqListener();

                Console.WriteLine($"--- Sending HL7 message to host: {mirthhostname} ---");
                MirthTester.TestSendingHL7(mirthhostname, rabbitMqListener);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Press Enter to exit");

                Console.ReadLine();
            }
        }
    }
}
