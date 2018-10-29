// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateManager.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the CertificateManager type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Realtime.Tester.Certificates.Windows
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;

    /// <summary>
    /// The certificate manager.
    /// </summary>
    public class CertificateManager
    {
        /// <summary>
        /// The install certificate.
        /// </summary>
        /// <param name="hostname">
        /// The hostname.
        /// </param>
        /// <param name="ssl">
        /// The ssl.
        /// </param>
        /// <param name="password">
        /// The password.
        /// </param>
        /// <exception cref="Exception">exception thrown
        /// </exception>
        public static void InstallCertificate(string hostname, bool ssl, string password)
        {
            string url = (ssl ? "https" : "http") + string.Empty +
                         $"://{hostname}/certificates/client/fabricrabbitmquser_client_cert.p12";
            byte[] certdata;

            Console.WriteLine($"Download certificate from {url}");

            // disable certificate check for testing
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
                {
                    Console.WriteLine($"SSL error: {errors}");
                    return true;
                };

            using (var client = new HttpClient())
            {
                using (var result = client.GetAsync(url).Result)
                {
                    if (result.IsSuccessStatusCode)
                    {
                        certdata = result.Content.ReadAsByteArrayAsync().Result;
                        Console.Write(certdata);
                    }
                    else
                    {
                        if (result.Content != null)
                        {
                            throw new Exception(result.Content.ReadAsStringAsync().Result);
                        }

                        throw new Exception($"Error code {result.StatusCode} from url: {url}");
                    }
                }
            }

            X509Certificate2 cert = new X509Certificate2(certdata, password);
            var foo = cert.Subject;
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert); // where cert is an X509Certificate object
            Console.WriteLine("Added cert to LocalMachine store");

            string userName = WindowsIdentity.GetCurrent().Name;
            AddAccessToCertificate(cert, userName);
        }

        /// <summary>
        /// The add access to certificate.
        /// </summary>
        /// <param name="cert">
        /// The cert.
        /// </param>
        /// <param name="user">
        /// The user.
        /// </param>
        private static void AddAccessToCertificate(X509Certificate2 cert, string user)
        {
            if (cert.PrivateKey is RSACryptoServiceProvider rsa)
            {
                string keyFileLocation =
                    FindKeyLocation(rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                FileInfo file = new FileInfo(keyFileLocation + "\\" +
                                             rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                FileSecurity fs = file.GetAccessControl();

                var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                var account = (NTAccount)sid.Translate(typeof(NTAccount));

                // NTAccount account = new NTAccount(user);
                fs.AddAccessRule(new FileSystemAccessRule(account, FileSystemRights.Read, AccessControlType.Allow));

                file.SetAccessControl(fs);

                Console.WriteLine("Added access to the cert's private key to all authenticated users");

            }
        }

        /// <summary>
        /// The find key location.
        /// </summary>
        /// <param name="keyFileName">
        /// The key file name.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string FindKeyLocation(string keyFileName)
        {
            string text1 =
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string text2 = text1 + @"\Microsoft\Crypto\RSA\MachineKeys";
            string[] textArray1 = Directory.GetFiles(text2, keyFileName);
            if (textArray1.Length > 0)
            {
                return text2;
            }
            string text3 =
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string text4 = text3 + @"\Microsoft\Crypto\RSA\";
            textArray1 = Directory.GetDirectories(text4);
            if (textArray1.Length > 0)
            {
                foreach (string text5 in textArray1)
                {
                    textArray1 = Directory.GetFiles(text5, keyFileName);
                    if (textArray1.Length != 0)
                    {
                        return text5;
                    }
                }
            }
            return "Private key exists but is not accessible";
        }
    }
}
