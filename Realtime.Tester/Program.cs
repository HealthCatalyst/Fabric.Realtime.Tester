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
    using System.Diagnostics;

    using Realtime.Interfaces;
    using Realtime.Tester.Certificates.Windows;
    using Realtime.Tester.Mirth;
    using Realtime.Tester.RabbitMq;

    using SimpleImpersonation;

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

                Console.WriteLine("---------------------------------------------------------------------------------------------------------------");
                Console.WriteLine("NOTE: This tester should be run on the ETL machine so you can install client certificates and test connectivity");
                Console.WriteLine("---------------------------------------------------------------------------------------------------------------");

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

                string userInput = string.Empty;

                do
                {
                    try
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
                                            Console.Write("Enter certificate password: (You can get this by typing 'dos' in the kubernetes master VM)");
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
                                            Console.Write("Enter certificate password: (You can get this by typing 'dos' in the kubernetes master VM)");
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
                                    string sqlServer;
                                    do
                                    {
                                        Console.Write("Sql Server: ");
                                        sqlServer = Console.ReadLine();
                                    }
                                    while (string.IsNullOrEmpty(sqlServer));

                                    TestingHelper.SetObjectAttributeBase(sqlServer, mirthHostName);
                                    break;
                                }

                            case "21":
                                {
                                    CertificateManager.ShowMyCertificates();
                                    break;
                                }

                            case "22":
                                {
                                    Console.WriteLine("-------- Client Certificates ----------");
                                    CertificateManager.ShowExistingCertificates();
                                    break;
                                }

                            case "23":
                                {
                                    Console.WriteLine("--------- CA root certificates --------");
                                    CertificateManager.ShowExistingTrustedRootCertificates();
                                    break;
                                }

                            case "31":
                                {
                                    Console.WriteLine($"--- Connecting to Mirth: {mirthHostName} ---");
                                    MirthTester.PingMirth(mirthHostName);
                                    MirthTester.TestConnection(mirthHostName);
                                    break;
                                }

                            case "32":
                                {
                                    Console.WriteLine($"--- Connecting to RabbitMq host: {mirthHostName} ---");

                                    RabbitMqTester.TestSecureConnectionToRabbitMq(rabbitMqHostName);
                                    break;
                                }

                            case "33":
                                {
                                    Console.WriteLine($"--- Listening to message from RabbitMq at host: {rabbitMqHostName} ---");
                                    IRabbitMqListener rabbitMqListener = new RabbitMqListener();

                                    Console.WriteLine($"--- Sending HL7 message to host: {mirthHostName} ---");
                                    MirthTester.TestSendingHL7(mirthHostName, rabbitMqListener);
                                    break;
                                }

                            case "34":
                                {
                                    string domain;
                                    do
                                    {
                                        Console.Write("Domain: ");
                                        domain = Console.ReadLine();
                                    }
                                    while (string.IsNullOrEmpty(domain));

                                    string username;
                                    do
                                    {
                                        Console.Write("User Name: ");
                                        username = Console.ReadLine();
                                    }
                                    while (string.IsNullOrEmpty(username));

                                    string password;
                                    do
                                    {
                                        Console.Write("Password: ");
                                        password = Console.ReadLine();
                                    }
                                    while (string.IsNullOrEmpty(password));

                                    var credentials = new UserCredentials(domain, username, password);
                                    var logonType = LogonType.Interactive;
                                    Impersonation.RunAsUser(
                                        credentials,
                                        logonType,
                                        () =>
                                            {
                                                Console.WriteLine($"--- Listening to message from RabbitMq at host: {rabbitMqHostName} ---");
                                                IRabbitMqListener rabbitMqListener = new RabbitMqListener();

                                                Console.WriteLine($"--- Sending HL7 message to host: {mirthHostName} ---");
                                                MirthTester.TestSendingHL7(mirthHostName, rabbitMqListener);
                                        });

                                    break;
                                }

                            case "41":
                                {
                                    Console.WriteLine("User name and password are available in the kubernetes VM. Just run the dos menu and choose Fabric Realtime Menu");
                                    Process.Start($"http://{mirthHostName}/rabbitmq");
                                    break;
                                }

                            case "42":
                                {
                                    Console.WriteLine("User name and password are available in the kubernetes VM. Just run the dos menu and choose Fabric Realtime Menu");
                                    Process.Start($"http://{mirthHostName}/mirth");
                                    break;
                                }

                            case "51":
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
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
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
            Console.WriteLine("3: Set connection in EdwAdmin database");
            Console.WriteLine("--------- Troubleshooting --------");
            Console.WriteLine("21: Show Fabric.Realtime Certificates on Local Machine");
            Console.WriteLine("22: Show Certificates on Local Machine");
            Console.WriteLine("23: Show Trusted Root Certificates on Local Machine");
            Console.WriteLine("------- Testers ----------");
            Console.WriteLine("31: Test Connection to Mirth");
            Console.WriteLine("32: Test Connection to RabbitMq");
            Console.WriteLine("33: Send a Test Message to Mirth & Listen on RabbitMq");
            Console.WriteLine("34: Send a Test Message to Mirth & Listen on RabbitMq (Different user)");
            Console.WriteLine("------- web portals ----------");
            Console.WriteLine("41: Open RabbitMq web portal");
            Console.WriteLine("42: Open Mirth web portal");
            Console.WriteLine("------- Cleanup ----------");
            Console.WriteLine("51: Delete all Fabric.Realtime certificates");
            Console.WriteLine("q: Exit");
            Console.WriteLine();

            Console.Write("Please make a selection: ");
            var result = Console.ReadLine();
            return result;
        }
    }
}
