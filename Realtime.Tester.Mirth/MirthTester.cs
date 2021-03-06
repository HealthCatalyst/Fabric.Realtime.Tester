﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MirthTester.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the MirthTester type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Realtime.Tester.Mirth
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Realtime.Interfaces;

    /// <summary>
    /// The mirth tester.
    /// </summary>
    public class MirthTester
    {
        /// <summary>
        /// The mirth port.
        /// </summary>
        protected internal const int MirthPort = 6661;

        /// <summary>
        /// The test sending h l 7.
        /// </summary>
        /// <param name="mirthhostname">
        /// The mirthhostname.
        /// </param>
        /// <param name="rabbitMqListener">
        /// The rabbit mq listener.
        /// </param>
        public static void TestSendingHL7(string mirthhostname, IRabbitMqListener rabbitMqListener)
        {
            // from http://www.mieweb.com/wiki/Sample_HL7_Messages#ADT.5EA01
            var message =
                @"MSH|^~\&|SENDING_APPLICATION|SENDING_FACILITY|RECEIVING_APPLICATION|RECEIVING_FACILITY|20110613083617||ADT^A01|934576120110613083617|P|2.3||||
EVN|A01|20110613083617|||
PID|1||135769||MOUSE^MICKEY^||19281118|M|||123 Main St.^^Lake Buena Vista^FL^32830||(407)939-1289^^^theMainMouse@disney.com|||||1719|99999999||||||||||||||||||||
PV1|1|O|||||^^^^^^^^|^^^^^^^^";

            // set up the queue first
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            var messageReceivedWaitHandle = new AutoResetEvent(false);
            var channelCreatedWaitHandle = new AutoResetEvent(false);

            var task = Task.Run(() => rabbitMqListener.GetMessage(mirthhostname, token, messageReceivedWaitHandle, channelCreatedWaitHandle), token);

            // wait until the channel/queue has been created otherwise any message we sent will not get to the queue
            channelCreatedWaitHandle.WaitOne();

            var result = SendHL7(mirthhostname, MirthPort, message);

            // wait until the message has been received
            messageReceivedWaitHandle.WaitOne();

            // tell the other thread to end
            tokenSource.Cancel();

            // wait for other thread to end
            // ReSharper disable once MethodSupportsCancellation
            task.Wait();

            var r = task.Result;
        }

        /// <summary>
        /// The ping mirth.
        /// </summary>
        /// <param name="server">
        /// The server.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool PingMirth(string server)
        {
            try
            {
                Console.WriteLine($"Pinging {server} on port {MirthPort}");

                using (var client = new TcpClient())
                {
                    client.ConnectAsync(server, MirthPort).Wait();

                    Console.WriteLine($"Successfully pinged mirth at {server}");
                    return true;
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error pinging host:'" + server + ":" + MirthPort + "'");
                Console.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// The test connection.
        /// </summary>
        /// <param name="server">
        /// The server.
        /// </param>
        /// <exception cref="Exception">exception thrown
        /// </exception>
        public static void TestConnection(string server)
        {
            using (Socket s = ConnectSocket(server, MirthPort))
            {
                // If the socket could not get a connection, then return false.
                if (s == null)
                {
                    throw new Exception($"Could not connect to Mirth at {server} on port {MirthPort}");
                }

                Console.WriteLine($"Successfully connected to {server} on port {MirthPort}");
            }
        }

        /// <summary>
        /// The send h l 7.
        /// </summary>
        /// <param name="server">
        /// The server.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <param name="hl7Message">
        /// The hl 7 message.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        /// <exception cref="Exception">exception thrown
        /// </exception>
        internal static bool SendHL7(string server, int port, string hl7Message)
        {
            try
            {
                // Add the leading and trailing characters so it is LLP complaint.
                string llpHL7Message = Convert.ToChar(11).ToString() + hl7Message + Convert.ToChar(28).ToString() + Convert.ToChar(13).ToString();

                // Get the size of the message that we have to send.
                byte[] bytesSent = Encoding.ASCII.GetBytes(llpHL7Message);
                byte[] bytesReceived = new byte[256];

                Console.WriteLine($"Connecting to server: {server} on port {port}");

                // Create a socket connection with the specified server and port.
                using (Socket s = ConnectSocket(server, port))
                {
                    // If the socket could not get a connection, then return false.
                    if (s == null)
                    {
                        throw new Exception("Could not connect to Mirth");
                    }

                    Console.WriteLine($"------- Sending HL7 message to {server} -------");
                    Console.WriteLine(llpHL7Message);
                    Console.WriteLine("---------------------------------");

                    // Send message to the server.
                    s.Send(bytesSent, bytesSent.Length, 0);


                    // Receive the response back
                    int bytes = 0;

                    s.ReceiveTimeout = 30 * 1000;
                    bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                    string page = Encoding.ASCII.GetString(bytesReceived, 0, bytes);

                    Console.WriteLine("-------- Response from Mirth -------");
                    Console.Write(page);
                    Console.WriteLine("---------------------------------");

                    // Check to see if it was successful
                    if (page.Contains("MSA|AA"))
                    {
                        Console.WriteLine("Received valid response from Mirth");
                        return true;
                    }
                    else
                    {
                        throw new Exception($"Got invalid response from Mirth:[{page}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// The connect socket.
        /// </summary>
        /// <param name="server">
        /// The server.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <returns>
        /// The <see cref="Socket"/>.
        /// </returns>
        private static Socket ConnectSocket(string server, int port)
        {
            // first see if the string is an IPAddress
            // ReSharper disable once InlineOutVariableDeclaration
            IPAddress ipAddress;
            if (IPAddress.TryParse(server, out ipAddress))
            {
                return ConnectSocket(ipAddress, port);
            }

            // Get host related information.
            var hostEntry = Dns.GetHostEntryAsync(server).Result;

            foreach (IPAddress address in hostEntry.AddressList)
            {
                var socket = ConnectSocket(address, port);
                if (socket != null)
                {
                    return socket;
                }
            }

            return null;
        }

        /// <summary>
        /// The connect socket.
        /// </summary>
        /// <param name="address">
        /// The address.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <returns>
        /// The <see cref="Socket"/>.
        /// </returns>
        private static Socket ConnectSocket(IPAddress address, int port)
        {
            IPEndPoint ipe = new IPEndPoint(address, port);
            Socket tempSocket =
                new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            tempSocket.Connect(ipe);

            if (tempSocket.Connected)
            {
                return tempSocket;
            }

            return null;
        }
    }
}
