using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RealtimeTester
{
    class RabbitMqListener
    {
        private const string ExchangeName = "fabric.realtime.hl7";
        private const string ExchangeType = "topic";

        private const string routingKey = "#";

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

        public CancellationTokenSource StartListening(string mirthhostname)
        {
            // set up the queue first
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            var messageReceivedWaitHandle = new AutoResetEvent(false);
            var channelCreatedWaitHandle = new AutoResetEvent(false);

            var task = Task.Run(() => this.GetMessage(mirthhostname, token, messageReceivedWaitHandle, channelCreatedWaitHandle), token);

            // wait until the channel/queue has been created otherwise any message we sent will not get to the queue
            channelCreatedWaitHandle.WaitOne();

            // wait until the message has been received

            // tell the other thread to end
            //tokenSource.Cancel();

            return tokenSource;
        }

    }
}