using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;

namespace RealtimeTester
{
    public class TestSSL
    {

        public static int Main(string[] args)
        {
            string rabbitmqhostname = "fabricrealtimerabbitmq.eastus.cloudapp.azure.com";
            string path_to_certificate_file = "/path/to/client/keycert.p12";
            string password = "MySecretPassword";

            try
            {
                // from http://blog.johnruiz.com/2011/12/establishing-ssl-connection-to-rabbitmq.html
                // and https://www.rabbitmq.com/ssl.html
                // and https://weblogs.asp.net/jeffreyabecker/Using-SSL-client-certificates-for-authentication-with-RabbitMQ

                // we use the rabbit connection factory, just like normal
                ConnectionFactory cf = new ConnectionFactory();

                // set the hostname and the port
                cf.HostName = rabbitmqhostname;
                cf.Port = AmqpTcpEndpoint.DefaultAmqpSslPort;
                cf.AuthMechanisms = new AuthMechanismFactory[] { new ExternalMechanismFactory() };


                // I've imported my certificate into my certificate store 
                // (the Personal/Certificates folder in the certmgr mmc snap-in)
                // Let's open that store right now.
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);

                foreach (X509Certificate2 certificate2 in store.Certificates)
                {
                    Debug.WriteLine("Expire:" + certificate2.GetExpirationDateString());
                    Debug.WriteLine($"Issuer:[{certificate2.Issuer}]");
                    Debug.WriteLine("Effective:" + certificate2.GetEffectiveDateString());
                    Debug.WriteLine("SimpleName:" + certificate2.GetNameInfo(X509NameType.SimpleName, true));
                    Debug.WriteLine("HasPrivateKey:" + certificate2.HasPrivateKey);
                    Debug.WriteLine("SubjectName:" + certificate2.SubjectName.Name);
                    Debug.WriteLine($"IssuerName:[{certificate2.IssuerName.Name}]");
                    Debug.WriteLine("-----------------------------------");
                }

                // and find my certificate by its thumbprint.
                var x509Certificate2Collection = store.Certificates
                    .Find(
                        X509FindType.FindByIssuerName,
                        "FabricRabbitMqCA",
                        false
                    );

                X509Certificate cert = x509Certificate2Collection
                    .OfType<X509Certificate>()
                    .First();

                // now, let's set the connection factory's ssl-specific settings
                // NOTE: it's absolutely required that what you set as Ssl.ServerName be
                //       what's on your rabbitmq server's certificate (its CN - common name)


                cf.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = rabbitmqhostname,
                    CertificateValidationCallback = MyValidateServerCertificate,
                    //AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch |
                    //                         SslPolicyErrors.RemoteCertificateChainErrors,
                    Certs = new X509CertificateCollection(new X509Certificate[] { cert }),
                };

                using (IConnection conn = cf.CreateConnection())
                {
                    using (IModel ch = conn.CreateModel())
                    {
                        ch.QueueDeclare("rabbitmq-dotnet-test", false, false, false, null);
                        ch.BasicPublish("", "rabbitmq-dotnet-test", null,
                            Encoding.UTF8.GetBytes("Hello, World"));
                        BasicGetResult result = ch.BasicGet("rabbitmq-dotnet-test", true);
                        if (result == null)
                        {
                            Console.WriteLine("No message received.");
                        }
                        else
                        {
                            Console.WriteLine("Received:");
                            DebugUtil.DumpProperties(result, Console.Out, 0);
                        }
                        ch.QueueDelete("rabbitmq-dotnet-test");
                    }
                }
            }
            catch (BrokerUnreachableException bex)
            {
                Exception ex = bex;
                while (ex != null)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("inner:");
                    ex = ex.InnerException;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return 0;
        }

        /// <summary>
        /// from https://msdn.microsoft.com/en-us/library/system.net.security.remotecertificatevalidationcallback(v=vs.110).aspx
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool MyValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            // allow the CA to not be present
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        /// <summary>
        /// from https://msdn.microsoft.com/en-us/library/system.net.security.localcertificateselectioncallback(v=vs.110).aspx
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetHost"></param>
        /// <param name="localCertificates"></param>
        /// <param name="remoteCertificate"></param>
        /// <param name="acceptableIssuers"></param>
        /// <returns></returns>
        public static X509Certificate SelectLocalCertificate(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            Console.WriteLine("Client is selecting a local certificate.");
            if (acceptableIssuers != null &&
                acceptableIssuers.Length > 0 &&
                localCertificates != null &&
                localCertificates.Count > 0)
            {
                // Use the first certificate that is from an acceptable issuer.
                foreach (X509Certificate certificate in localCertificates)
                {
                    string issuer = certificate.Issuer;
                    if (Array.IndexOf(acceptableIssuers, issuer) != -1)
                        return certificate;
                }
            }
            if (localCertificates != null &&
                localCertificates.Count > 0)
                return localCertificates[0];

            return null;
        }

    }
}