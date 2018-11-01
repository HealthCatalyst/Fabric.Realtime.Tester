// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitMqConnectionFactory.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the RabbitMqConnectionFactory type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Realtime.Tester.RabbitMq
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    using RabbitMQ.Client;

    /// <summary>
    /// The rabbit mq connection factory.
    /// </summary>
    public class RabbitMqConnectionFactory
    {
        /// <summary>
        /// The get connection factory.
        /// </summary>
        /// <param name="rabbitMqHostName">
        /// The rabbitMqHostName.
        /// </param>
        /// <returns>
        /// The <see cref="ConnectionFactory"/>.
        /// </returns>
        /// <exception cref="Exception">exception thrown
        /// </exception>
        public static ConnectionFactory GetConnectionFactory(string rabbitMqHostName)
        {
            // from http://blog.johnruiz.com/2011/12/establishing-ssl-connection-to-rabbitmq.html
            // and https://www.rabbitmq.com/ssl.html
            // and https://weblogs.asp.net/jeffreyabecker/Using-SSL-client-certificates-for-authentication-with-RabbitMQ

            // we use the rabbit connection factory, just like normal
            ConnectionFactory cf = new ConnectionFactory();

            // set the hostname and the port
            cf.HostName = rabbitMqHostName;
            cf.Port = AmqpTcpEndpoint.DefaultAmqpSslPort;

            if (!rabbitMqHostName.Equals("localhost"))
            {
                cf.AuthMechanisms = new AuthMechanismFactory[] {new ExternalMechanismFactory()};


                // I've imported my certificate into my certificate store 
                // (the Personal/Certificates folder in the certmgr mmc snap-in)
                // Let's open that store right now.
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);

                foreach (X509Certificate2 certificate2 in store.Certificates)
                {
                    Console.WriteLine("Expire:" + certificate2.NotAfter);
                    Console.WriteLine($"Issuer:[{certificate2.Issuer}]");
                    Console.WriteLine("Effective:" + certificate2.NotBefore);
                    Console.WriteLine("SimpleName:" + certificate2.GetNameInfo(X509NameType.SimpleName, true));
                    Console.WriteLine("HasPrivateKey:" + certificate2.HasPrivateKey);
                    Console.WriteLine("SubjectName:" + certificate2.SubjectName.Name);
                    Console.WriteLine($"IssuerName:[{certificate2.IssuerName.Name}]");
                    Console.WriteLine("-----------------------------------");
                }

                // and find my certificate by its thumbprint.
                var x509Certificate2Collection = store.Certificates
                    .Find(
                        X509FindType.FindByIssuerName,
                        "FabricCertificateAuthority",
                        false);

                if (!x509Certificate2Collection
                    .OfType<X509Certificate>().Any())
                {
                    throw new Exception(
                        "No client certificate found on this machine with IssuerName=FabricCertificateAuthority");
                }

                X509Certificate cert = x509Certificate2Collection
                    .OfType<X509Certificate2>()
                    .OrderByDescending(a => a.NotAfter)
                    .First();

                //// check that we can access the private key with the user this application is running under
                //try
                //{
                //    var key = ((X509Certificate2) cert).PrivateKey;
                //}
                //catch (Exception ex)
                //{
                //    throw new Exception(
                //        "Cannot access private key for the certificate.  Make sure the user account of this application has access to private key for the client certificate with IssuerName=FabricCertificateAuthority.  https://docs.secureauth.com/display/KBA/Grant+Permission+to+Use+Signing+Certificate+Private+Key");
                //}

                // now, let's set the connection factory's ssl-specific settings
                // NOTE: it's absolutely required that what you set as Ssl.ServerName be
                //       what's on your rabbitmq server's certificate (its CN - common name)


                cf.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = rabbitMqHostName,
                    CertificateValidationCallback = MyValidateServerCertificate,
                    AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch |
                                             SslPolicyErrors.RemoteCertificateChainErrors,
                    Certs = new X509CertificateCollection(new X509Certificate[] { cert }),
                    Version = System.Security.Authentication.SslProtocols.Tls12
                };
            }
            else
            {
                // ReSharper disable once StringLiteralTypo
                cf.UserName = "fabricrabbitmquser";
                cf.Password = "gryxA8wpqk8YU5hy";
            }

            cf.SocketReadTimeout = 1000 * 30;
            cf.SocketWriteTimeout = 1000 * 30;
            return cf;
        }

        /// <summary>
        /// from https://msdn.microsoft.com/en-us/library/system.net.security.remotecertificatevalidationcallback(v=vs.110).aspx
        /// </summary>
        /// <param name="sender">sender name</param>
        /// <param name="certificate">certificate name</param>
        /// <param name="chain">chain of cert</param>
        /// <param name="sslPolicyErrors">ssl policy errors</param>
        /// <returns>whether cert is valid </returns>
        public static bool MyValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            Console.WriteLine($"Certificate Subject= {certificate.Subject}");
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("================================");
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            Console.WriteLine($"Certificate Subject= {certificate.Subject}");
            Console.WriteLine("================================");
            Console.ResetColor();

            // allow the CA to not be present
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
            {
                if (chain.ChainStatus.Length > 0)
                {
                    foreach (var chainStatus in chain.ChainStatus)
                    {
                        Console.WriteLine("----- Chain Error --------");
                        Console.WriteLine($"Status: {chainStatus.Status}");
                        Console.WriteLine($"Status Information: {chainStatus.StatusInformation}");
                        Console.WriteLine("---------------------------------");
                    }

                    Console.WriteLine("-------- Chain elements ----------");

                    foreach (var chainElement in chain.ChainElements)
                    {
                        Console.WriteLine("-------- Chain element ----------");
                        Console.WriteLine($"{chainElement.Certificate.Subject}");
                    }

                    Console.WriteLine("---------------------------------");
                }
            }

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        /// <summary>
        /// from https://msdn.microsoft.com/en-us/library/system.net.security.localcertificateselectioncallback(v=vs.110).aspx
        /// </summary>
        /// <param name="sender">sender name</param>
        /// <param name="targetHost">target host</param>
        /// <param name="localCertificates">local certificates</param>
        /// <param name="remoteCertificate">remote certificates</param>
        /// <param name="acceptableIssuers">acceptable issuers</param>
        /// <returns>local certificate</returns>
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
                    {
                        return certificate;
                    }
                }
            }

            if (localCertificates != null && localCertificates.Count > 0)
            {
                return localCertificates[0];
            }

            return null;
        }
    }
}
