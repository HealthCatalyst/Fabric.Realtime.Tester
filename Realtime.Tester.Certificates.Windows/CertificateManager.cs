using System;
using System.IO;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace Realtime.Tester.Certificates.Windows
{
    public class CertificateManager
    {
        public static void InstallCertificate(string hostname, bool ssl, string password)
        {
            string url = (ssl ? "https" : "http") + $"" +
                         $"://{hostname}/certificates/client/fabricrabbitmquser_client_cert.p12";
            byte[] certdata;

            using (var client = new HttpClient())
            {
                using (var result = client.GetAsync(url).Result)
                {
                    if (result.IsSuccessStatusCode)
                    {
                        certdata = result.Content.ReadAsByteArrayAsync().Result;
                    }
                    else
                    {
                        return;
                    }

                }
            }

            X509Certificate2 cert = new X509Certificate2(certdata, password);
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert); //where cert is an X509Certificate object

            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            AddAccessToCertificate(cert, userName);
        }

        private static void AddAccessToCertificate(X509Certificate2 cert, string user)
        {
            RSACryptoServiceProvider rsa = cert.PrivateKey as RSACryptoServiceProvider;

            if (rsa != null)
            {
                string keyfilepath =
                    FindKeyLocation(rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                FileInfo file = new FileInfo(keyfilepath + "\\" +
                                             rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                FileSecurity fs = file.GetAccessControl();

                var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                var account = (NTAccount)sid.Translate(typeof(NTAccount));

                // NTAccount account = new NTAccount(user);
                fs.AddAccessRule(new FileSystemAccessRule(account,
                    FileSystemRights.Read, AccessControlType.Allow));

                file.SetAccessControl(fs);
            }
        }

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
