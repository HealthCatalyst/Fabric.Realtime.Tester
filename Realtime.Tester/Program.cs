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

                string mirthHostName;

                if (args.Length < 1)
                {
                    // host was not passed on the command line
                    do
                    {
                        Console.Write("Enter host to connect to: ");
                        mirthHostName = Console.ReadLine();
                    }
                    while (string.IsNullOrWhiteSpace(mirthHostName));
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
                                string certificatePassword;
                                if (args.Length < 2)
                                {
                                    do
                                    {
                                        Console.Write("Enter certificate password: ");
                                        certificatePassword = Console.ReadLine();
                                    }
                                    while (string.IsNullOrEmpty(certificatePassword));
                                }
                                else
                                {
                                    certificatePassword = args[1];
                                }

                                certificatePassword = certificatePassword?.Trim();

                                Console.WriteLine("--- Installing Trusted Root certificate ---");
                                CertificateManager.InstallTrustedRootCertificate(mirthHostName, true, certificatePassword, null);

                                break;
                            }

                        case "2":
                            {
                                string certificatePassword;
                                if (args.Length < 2)
                                {
                                    do
                                    {
                                        Console.Write("Enter certificate password: ");
                                        certificatePassword = Console.ReadLine();
                                    }
                                    while (string.IsNullOrEmpty(certificatePassword));
                                }
                                else
                                {
                                    certificatePassword = args[1];
                                }

                                certificatePassword = certificatePassword?.Trim();

                                Console.WriteLine(
                                    "Please enter service account name that runs the data processing engine so we can grant it access to the certificate (Leave empty to give access to All Authenticated Users)");
                                var serviceAccountName = Console.ReadLine();

                                Console.WriteLine("--- Installing SSL client certificate ---");
                                CertificateManager.InstallClientCertificate(
                                    mirthHostName,
                                    true,
                                    certificatePassword,
                                    serviceAccountName);

                                break;
                            }

                        case "3":
                            {
                                CertificateManager.ShowMyCertificates();
                                break;
                            }

                        case "4":
                            {
                                Console.WriteLine("-------- Client Certificates ----------");
                                CertificateManager.ShowExistingCertificates();
                                break;
                            }

                        case "5":
                            {
                                Console.WriteLine("--------- CA root certificates --------");
                                CertificateManager.ShowExistingTrustedRootCertificates();
                                break;
                            }

                        case "6":
                            {
                                Console.WriteLine($"--- Connecting to Mirth: {mirthHostName} ---");
                                MirthTester.PingMirth(mirthHostName);
                                MirthTester.TestConnection(mirthHostName);
                                break;
                            }

                        case "7":
                            {
                                Console.WriteLine($"--- Connecting to RabbitMq host: {mirthHostName} ---");

                                RabbitMqTester.TestSecureConnectionToRabbitMq(rabbitMqHostName);
                                break;
                            }
                            
                        case "8":
                            {
                                Console.WriteLine($"--- Listening to message from RabbitMq at host: {rabbitMqHostName} ---");
                                IRabbitMqListener rabbitMqListener = new RabbitMqListener();

                                Console.WriteLine($"--- Sending HL7 message to host: {mirthHostName} ---");
                                MirthTester.TestSendingHL7(mirthHostName, rabbitMqListener);
                                break;
                            }

                        case "9":
                            {
                                CertificateManager.RemoveMyCertificates();
                                break;
                            }

                        default:
                            {
                                Console.WriteLine($"Invalid choice: {userInput}");
                                break;
                            }
                    }

                    if (userInput != "q")
                    {
                        Console.WriteLine();
                        Console.WriteLine("(Press any key to go to the menu)");
                        Console.ReadKey();
                        Console.Clear();
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
            Console.WriteLine("1: Install Trusted Root Certificate on Local Machine");
            Console.WriteLine("2: Install Certificate on Local Machine");
            Console.WriteLine("3: Show Fabric.Realtime Certificates on Local Machine");
            Console.WriteLine("4: Show Certificates on Local Machine");
            Console.WriteLine("5: Show Trusted Root Certificates on Local Machine");
            Console.WriteLine("6: Test Connection to Mirth");
            Console.WriteLine("7: Test Connection to RabbitMq");
            Console.WriteLine("8: Send a Test Message to Mirth & Listen on RabbitMq");
            Console.WriteLine("9: Delete all Fabric.Realtime certificates");
            Console.WriteLine("q: Exit");
            Console.WriteLine();

            Console.Write("Please make a selection: ");
            var result = Console.ReadLine();
            return result;
        }
    }
}
