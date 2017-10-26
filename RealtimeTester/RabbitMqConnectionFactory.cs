using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using RabbitMQ.Client;

namespace RealtimeTester
{
    class RabbitMqConnectionFactory
    {
        internal static ConnectionFactory GetConnectionFactory(string rabbitmqhostname)
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
                    "FabricCertificateAuthority",
                    false
                );

            if (!x509Certificate2Collection
                .OfType<X509Certificate>().Any())
            {
                throw new Exception("No client certificate found on this machine with IssuerName=FabricCertificateAuthority");
            }

            X509Certificate cert = x509Certificate2Collection
                .OfType<X509Certificate>()
                .OrderByDescending(a => a.GetExpirationDateString())
                .First();

            // check that we can access the private key with the user this application is running under
            try
            {
                var key = ((X509Certificate2)cert).PrivateKey;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Cannot access private key for the certificate.  Make sure the user account of this application has access to private key for the client certificate with IssuerName=FabricCertificateAuthority.  https://docs.secureauth.com/display/KBA/Grant+Permission+to+Use+Signing+Certificate+Private+Key");
            }

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

            cf.SocketReadTimeout = 1000 * 30;
            cf.SocketWriteTimeout = 1000 * 30;
            return cf;
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
