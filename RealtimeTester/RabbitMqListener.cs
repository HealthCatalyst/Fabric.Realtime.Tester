using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RealtimeTester
{
    class RabbitMqListener
    {
        private const string ExchangeName = "fabric.interfaceengine";
        private const string ExchangeType = "direct";

        private const string routingKey = "mirth.connect.inbound";

        public void SetupExchange(string hostname)
        {
            var factory = new ConnectionFactory {HostName = hostname};
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType);

            }
        }

        public string GetMessage(string hostname, CancellationToken token, AutoResetEvent waitHandle)
        {
            var factory = RabbitMqConnectionFactory.GetConnectionFactory(hostname);

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType);
                var queueName = channel.QueueDeclare().QueueName;

                channel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: routingKey);

                var consumer = new EventingBasicConsumer(channel);
                string myMessage = null;

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    myMessage = message;
                    Console.WriteLine($"Received {routingKey}: {message}");
                    waitHandle.Set();
                };
                var basicConsume = channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                while (!token.IsCancellationRequested)
                {

                }

                return myMessage;
            }

        }
    }
}