using System;
using System.Collections.Generic;
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
    public class RabbitMqTester
    {
        internal static void TestSecureConnectionToRabbitMq(string rabbitmqhostname)
        {
            try
            {
                var cf = RabbitMqConnectionFactory.GetConnectionFactory(rabbitmqhostname);

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
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("inner:");
                    ex = ex.InnerException;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

    }
}
