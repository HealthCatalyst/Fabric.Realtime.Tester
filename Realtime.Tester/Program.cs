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
                string mirthHostName;

                if (args.Length < 1)
                {
                    // host was not passed on the command line
                    Console.Write("Enter host to connect to: ");
                    mirthHostName = Console.ReadLine();
                }
                else
                {
                    mirthHostName = args[0];
                }

                mirthHostName = mirthHostName?.Trim();

                string rabbitMqHostName = mirthHostName;

                string userInput;

                do
                {
                    userInput = DisplayMenu();

                    switch (userInput)
                    {
                        case "1":
                            {
                                string certificatepassword;
                                if (args.Length < 2)
                                {
                                    Console.Write("Enter certificate password: ");
                                    certificatepassword = Console.ReadLine();
                                }
                                else
                                {
                                    certificatepassword = args[1];
                                }

                                certificatepassword = certificatepassword?.Trim();

                                Console.WriteLine("--- Installing SSL client certificate ---");

                                CertificateManager.InstallCertificate(mirthHostName, true, certificatepassword);
                                break;
                            }

                        case "2":
                            {
                                CertificateManager.ShowExistingCertificates();
                                break;
                            }

                        case "3":
                            {
                                Console.WriteLine($"--- Connecting to Mirth: {mirthHostName} ---");
                                MirthTester.PingMirth(mirthHostName);
                                MirthTester.TestConnection(mirthHostName);
                                break;
                            }

                        case "4":
                            {
                                Console.WriteLine($"--- Connecting to RabbitMq host: {mirthHostName} ---");

                                RabbitMqTester.TestSecureConnectionToRabbitMq(rabbitMqHostName);
                                break;
                            }
                            
                        case "5":
                            {
                                Console.WriteLine($"--- Listening to message from RabbitMq at host: {rabbitMqHostName} ---");
                                IRabbitMqListener rabbitMqListener = new RabbitMqListener();

                                Console.WriteLine($"--- Sending HL7 message to host: {mirthHostName} ---");
                                MirthTester.TestSendingHL7(mirthHostName, rabbitMqListener);
                                break;
                            }

                        default:
                            {
                                Console.WriteLine($"Invalid choice: {userInput}");
                                break;
                            }
                    }
                }
                while (userInput != "q");

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

        /// <summary>
        /// The display menu.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private static string DisplayMenu()
        {
            Console.WriteLine("----- Tester for Fabric.Realtime ----");
            Console.WriteLine();
            Console.WriteLine("1: Install Certificate on Local Machine");
            Console.WriteLine("2: Show Certificates on Local Machine");
            Console.WriteLine("3: Test Connection to Mirth");
            Console.WriteLine("4: Test Connection to RabbitMq");
            Console.WriteLine("5: Send a Test Message to Mirth & Listen on RabbitMq");
            Console.WriteLine("q: Exit");
            Console.WriteLine();

            Console.Write("Please make a selection: ");
            var result = Console.ReadLine();
            return result;
        }
    }
}
