﻿// --------------------------------------------------------------------------------------------------------------------
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

    using Realtime.Tester.Certificates.Windows;
    using Realtime.Tester.Mirth;
    using Realtime.Tester.RabbitMq;

    public class Program
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
                }
                else
                {
                    mirthhostname = args[0];
                }

                mirthhostname = mirthhostname?.Trim();

                //if (args.Length < 2)
                //{
                //    Console.WriteLine("Enter certificate password:");
                //    certificatepassword = Console.ReadLine();
                //}
                //else
                //{
                //    certificatepassword = args[1];
                //}

                //certificatepassword = certificatepassword?.Trim();

                //CertificateManager.InstallCertificate(mirthhostname, true, certificatepassword);

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
