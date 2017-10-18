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
        private const string ExchangeName = "fabric.realtime.hl7";
        private const string ExchangeType = "topic";

        private const string routingKey = "#";

        public void SetupExchange(string hostname)
        {
            var factory = RabbitMqConnectionFactory.GetConnectionFactory(hostname);
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType, durable: true);
            }

        }

        public string GetMessage(string hostname, CancellationToken token, AutoResetEvent messageReceivedWaitHandle, AutoResetEvent channelCreatedWaitHandle)
        {
            var factory = RabbitMqConnectionFactory.GetConnectionFactory(hostname);

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType, durable: true);
                var queueName = channel.QueueDeclare().QueueName;

                channel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: routingKey);

                channelCreatedWaitHandle.Set();

                var consumer = new EventingBasicConsumer(channel);
                string myMessage = null;

                Console.WriteLine(
                    $"Listening for messages on host:{hostname} for queue:{queueName}, exchange:{ExchangeName} with routing key:{routingKey}");

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    myMessage = message;
                    Console.WriteLine($"Received {routingKey}: {message}");
                    messageReceivedWaitHandle.Set();
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