using Realtime.Tester.RabbitMq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realtime.Tester.Certificates.Windows;
using Realtime.Tester.Mirth;

namespace Realtime.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                Console.WriteLine("Realtime tester using .Net Framework 4.6.1");

                //string mirthhostname = "fabricrealtimerabbitmq.eastus.cloudapp.azure.com";
                string mirthhostname = "fabricrealtime.eastus.cloudapp.azure.com";
                string certificatepassword = "";

                if (args.Length < 1)
                {
                    // host was not passed on the command line
                    Console.WriteLine("Enter host to connect to:");
                    mirthhostname = Console.ReadLine();
                    Console.WriteLine("Enter certificate password:");
                    certificatepassword = Console.ReadLine();
                }
                else
                {
                    mirthhostname = args[0];
                }

                CertificateManager.InstallCertificate(mirthhostname, false, certificatepassword);

                Console.WriteLine($"Connecting to host: {mirthhostname}");

                string rabbitmqhostname = mirthhostname;

                RabbitMqTester.TestSecureConnectionToRabbitMq(rabbitmqhostname);

                IRabbitMqListener rabbitMqListener = new RabbitMqListener();

                MirthTester.TestSendingHL7(mirthhostname, rabbitMqListener);

                //var rabbitMqListener = new RabbitMqListener();

                //rabbitMqListener.StartListening(mirthhostname);

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
