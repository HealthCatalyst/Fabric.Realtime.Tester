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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
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
        /// <param name="serviceAccountToGrantAccess">service account to grant access</param>
        /// <exception cref="Exception">exception thrown
        /// </exception>
        public static void InstallClientCertificate(
            string hostname,
            bool ssl,
            string password,
            string serviceAccountToGrantAccess)
        {
            string url = (ssl ? "https" : "http") + string.Empty
                                                  // ReSharper disable once StringLiteralTypo
                                                  + $"://{hostname}/certificates/client/fabricrabbitmquser_client_cert.p12";

            InternalInstallCertificate(StoreName.My, url, password, serviceAccountToGrantAccess, true);
        }

        /// <summary>
        /// The install trusted root certificate.
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
        /// <param name="serviceAccountToGrantAccess">
        /// The service account to grant access.
        /// </param>
        public static void InstallTrustedRootCertificate(
            string hostname,
            bool ssl,
            string password,
            string serviceAccountToGrantAccess)
        {
            string url = (ssl ? "https" : "http") + string.Empty
                                                  + $"://{hostname}/certificates/client/fabric_ca_cert.p12";

            InternalInstallCertificate(StoreName.Root, url, password, serviceAccountToGrantAccess, false);
        }

        /// <summary>
        /// The show existing certificates.
        /// </summary>
        public static void ShowExistingCertificates()
        {
            ShowCertificates(StoreName.My);
        }

        /// <summary>
        /// The show existing ca certificates.
        /// </summary>
        public static void ShowExistingTrustedRootCertificates()
        {
            ShowCertificates(StoreName.Root);
        }

        /// <summary>
        /// The show my certificates.
        /// </summary>
        public static void ShowMyCertificates()
        {
            var issuer = "O=HealthCatalyst, CN=FabricCertificateAuthority";
            Console.WriteLine("---- Root certificate ----");
            ShowCertificates(StoreName.Root, issuer);
            Console.WriteLine("----- client certificate ----");
            ShowCertificates(StoreName.My, issuer);
        }

        /// <summary>
        /// The remove my certificates.
        /// </summary>
        public static void RemoveMyCertificates()
        {
            // ReSharper disable once StringLiteralTypo
            var subjectForClientCertificate = "O=HealthCatalyst, CN=fabricrabbitmquser";
            var subjectForRootCertificate = "O=HealthCatalyst, CN=FabricCertificateAuthority";

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            RemoveCertificatesWithSubjectName(store, subjectForClientCertificate);
            store.Close();

            store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            RemoveCertificatesWithSubjectName(store, subjectForRootCertificate);
            store.Close();
        }

        /// <summary>
        /// The internal show certificates.
        /// </summary>
        /// <param name="storeName">
        /// The store name.
        /// </param>
        /// <param name="filterByIssuer">issuer to filter by</param>
        private static void ShowCertificates(StoreName storeName, string filterByIssuer = null)
        {
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadWrite);

                foreach (var certificate in store.Certificates)
                {
                    if (!string.IsNullOrWhiteSpace(filterByIssuer))
                    {
                        if (certificate.Issuer != filterByIssuer)
                        {
                            continue;
                        }
                    }
                    Console.WriteLine($"--- Certificate {certificate.FriendlyName}-----");
                    Console.WriteLine($"Issuer: {certificate.IssuerName.Name}");
                    Console.WriteLine($"Subject: {certificate.SubjectName.Name}");
                    Console.WriteLine($"Subject Simple Name: {certificate.GetNameInfo(X509NameType.SimpleName, false)}");
                    Console.WriteLine($"Subject DNS Name: {certificate.GetNameInfo(X509NameType.DnsName, false)}");
                    Console.WriteLine($"Issuer Simple Name: {certificate.GetNameInfo(X509NameType.SimpleName, true)}");
                    Console.WriteLine($"Issuer DNS Name: {certificate.GetNameInfo(X509NameType.DnsName, true)}");
                    Console.WriteLine($"Effective Date: {certificate.GetEffectiveDateString()}");
                    Console.WriteLine($"Expiration Date: {certificate.GetExpirationDateString()}");
                }
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// The internal install certificate.
        /// </summary>
        /// <param name="storeName">store name</param>
        /// <param name="url">
        ///     The url.
        /// </param>
        /// <param name="password">
        ///     The password.
        /// </param>
        /// <param name="serviceAccountToGrantAccess">
        ///     The service account to grant access.
        /// </param>
        /// <param name="addAccessToCertificateForServiceAccount">whether to add access to certificate</param>
        /// <exception cref="Exception">exception thrown
        /// </exception>
        private static void InternalInstallCertificate(
            StoreName storeName,
            string url,
            string password,
            string serviceAccountToGrantAccess,
            bool addAccessToCertificateForServiceAccount)
        {
            byte[] certificateData;

            Console.WriteLine($"Downloading certificate from {url}");

            // disable certificate check for installation
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
            {
                if (errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
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

                    // allow certificate errors here since we are installing the certificate
                    return true;
                }

                return true;
            };

            using (var client = new HttpClient())
            {
                using (var result = client.GetAsync(url).Result)
                {
                    if (result.IsSuccessStatusCode)
                    {
                        certificateData = result.Content.ReadAsByteArrayAsync().Result;
                        Console.WriteLine($"Successfully downloaded certificate (length={certificateData.Length})");
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

            X509Certificate2 newCertificate = new X509Certificate2(certificateData, password);
            Console.WriteLine($"Subject of certificate: {newCertificate.SubjectName.Name}");

            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            RemoveCertificatesWithSubjectName(store, newCertificate.Subject);

            store.Add(newCertificate); // where cert is an X509Certificate object
            Console.WriteLine("Added cert to LocalMachine store");

            if (addAccessToCertificateForServiceAccount)
            {
                AddAccessToCertificate(newCertificate, serviceAccountToGrantAccess);
            }

            store.Close();
        }

        /// <summary>
        /// The remove certificates with subject name.
        /// </summary>
        /// <param name="store">
        /// The store.
        /// </param>
        /// <param name="newCertificateSubject">
        /// The new certificate subject.
        /// </param>
        private static void RemoveCertificatesWithSubjectName(X509Store store, string newCertificateSubject)
        {
            var certificatesToRemove = new List<X509Certificate2>();

            foreach (var certificate in store.Certificates)
            {
                // ReSharper disable once StringLiteralTypo
                if (certificate.Subject == newCertificateSubject)
                {
                    certificatesToRemove.Add(certificate);
                }
            }

            if (certificatesToRemove.Count == 0)
            {
                return;
            }

            Console.WriteLine($"Found {certificatesToRemove.Count} existing certificate(s).  Removing them...");
            certificatesToRemove.ForEach(
                certificate2 =>
                    {
                        Console.WriteLine($"Removing: {certificate2.Subject}");
                        store.Remove(certificate2);
                    });
        }

        /// <summary>
        /// The add access to certificate.
        /// </summary>
        /// <param name="cert">
        /// The cert.
        /// </param>
        /// <param name="serviceAccount">
        /// The serviceAccount.
        /// </param>
        private static void AddAccessToCertificate(X509Certificate2 cert, string serviceAccount)
        {
            if (cert.PrivateKey is RSACryptoServiceProvider rsa)
            {
                string keyFileLocation =
                    FindKeyLocation(rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                FileInfo file = new FileInfo(keyFileLocation + "\\" +
                                             rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                FileSecurity fs = file.GetAccessControl();

                if (string.IsNullOrWhiteSpace(serviceAccount))
                {
                    var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                    var account = (NTAccount)sid.Translate(typeof(NTAccount));

                    Console.WriteLine("Adding access for all authenticated users");
                    fs.AddAccessRule(new FileSystemAccessRule(account, FileSystemRights.Read, AccessControlType.Allow));
                }
                else
                {
                    Console.WriteLine($"Adding access for {serviceAccount}");
                    NTAccount account = new NTAccount(serviceAccount);
                    fs.AddAccessRule(new FileSystemAccessRule(account, FileSystemRights.Read, AccessControlType.Allow));
                }

                {
                    // always add permission for current user for testing
                    var currentUser = WindowsIdentity.GetCurrent().Name;
                    Console.WriteLine($"Adding access for current user {currentUser}");
                    NTAccount account = new NTAccount(currentUser);
                    fs.AddAccessRule(new FileSystemAccessRule(account, FileSystemRights.Read, AccessControlType.Allow));
                }

                file.SetAccessControl(fs);

                Console.WriteLine("Added access to certificate");
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
