using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Veracity.EventHub.Abstraction;

namespace Veracity.EventHub.RabbitMQ
{
    public class RabbitMQProvider: IEventHub
    {
        public class DiagnosticOperationNames
        {

        }

        private const string ChannelType = "topic";

        private readonly IConnection _conn;
        private readonly IModel _channel;
        private readonly DiagnosticSource _diagnostics;

        public RabbitMQProvider(IConnectionFactory connFactory, DiagnosticSource diagnosticSource)
        {
            if (connFactory == null)
                throw new ArgumentNullException(nameof(connFactory));

            _conn = connFactory.CreateConnection();
            _channel = _conn.CreateModel();
            _diagnostics = diagnosticSource ?? throw new ArgumentNullException(nameof(diagnosticSource));
        }

        public void Subscribe(string @namespace, string eventType, Func<EventMessage, Task> handler)
        {
            var channel = _conn.CreateModel();
            channel.ExchangeDeclare(@namespace, ChannelType);
            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queueName, @namespace, eventType);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (sender, @event) =>
            {
                var eventMessage = new EventMessage
                {
                    Namespace = @event.Exchange,
                    EventType = @event.RoutingKey,
                    MessageBody = @event.Body
                };
                
                if (_diagnostics.IsEnabled())
                var activity = new Activity("Http_Out");
                _diagnostics.StartActivity();

                try
                {
                    await handler(eventMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    _diagnostics.StopActivity();
                }
            };
        }

        public void Publish(EventMessage eventMessage)
        {
            if (eventMessage == null)
                throw new ArgumentNullException(nameof(eventMessage));

            if (string.IsNullOrEmpty(eventMessage.Namespace))
                throw new ArgumentNullException(nameof(eventMessage.Namespace));

            _channel.ExchangeDeclare(eventMessage.Namespace, ChannelType);
            _channel.BasicPublish(eventMessage.Namespace, eventMessage.EventType, null, eventMessage.MessageBody);
        }
    }
}
