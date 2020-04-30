using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.MessagePatterns;
using Veracity.EventHub.Abstraction;
using ISubscription = Veracity.EventHub.Abstraction.ISubscription;

namespace Veracity.EventHub.RabbitMQ
{
    public class RabbitMQHub: IEventHub
    {
        public class DiagnosticOperations
        {
            public const string MessageHandling = "Message_Handling";
            public const string MessageHandlingError = "Message_Handling_Error";
        }

        internal class Subscription : ISubscription
        {
            private readonly Action _unsubscribe;

            public Subscription(string @namespace, string eventType, Action unsubscribe)
            {
                if (string.IsNullOrEmpty(@namespace))
                    throw new ArgumentNullException(nameof(@namespace));

                if (string.IsNullOrEmpty(eventType))
                    throw new ArgumentNullException(nameof(eventType));

                Namespace = @namespace;
                EventType = eventType;
                _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
            }

            public string Namespace { get; }

            public string EventType { get; }

            public void Unsubscribe()
            {
                _unsubscribe();
            }
        }

        private const string ChannelType = "topic";
        private readonly IConnection _conn;
        private readonly IModel _channel;
        private readonly DiagnosticListener _diagnostics;

        public RabbitMQHub(IConnectionFactory connFactory)
        {
            if (connFactory == null)
                throw new ArgumentNullException(nameof(connFactory));
            
            _conn = connFactory.CreateConnection();
            _channel = _conn.CreateModel();
            _diagnostics = null;
        }

        public RabbitMQHub(IConnectionFactory connFactory, DiagnosticListener diagnostic)
        {
            if (connFactory == null)
                throw new ArgumentNullException(nameof(connFactory));
            
            _conn = connFactory.CreateConnection();
            _channel = _conn.CreateModel();
            _diagnostics = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
        }

        public ISubscription Subscribe(string @namespace, Func<EventMessage, Task> handler)
        {
            //subscribe without eventTypeFilter
            return Subscribe(@namespace, "#", handler);
        }

        public ISubscription Subscribe(string @namespace, string eventTypeFilter, Func<EventMessage, Task> handler)
        {
            if (string.IsNullOrEmpty(@namespace))
                throw new ArgumentNullException(nameof(@namespace));
            
            if (string.IsNullOrEmpty(eventTypeFilter))
                throw new ArgumentNullException(nameof(eventTypeFilter));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var channel = _conn.CreateModel();
            channel.ExchangeDeclare(@namespace, ChannelType);
            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queueName, @namespace, eventTypeFilter);
            var consumer = new AsyncEventingBasicConsumer(channel);

            async Task EventHandler(object sender, BasicDeliverEventArgs @event)
            {
                var eventMessage = new EventMessage
                {
                    Namespace = @event.Exchange,
                    RoutingKey = @event.RoutingKey,
                    EventType = @event.BasicProperties.Type,
                    ContentType = @event.BasicProperties.ContentType,
                    ContentEncoding = @event.BasicProperties.ContentEncoding,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(@event.BasicProperties.Timestamp.UnixTime),
                    MessageBody = @event.Body
                };

                Activity activity = null;

                if (_diagnostics != null && _diagnostics.IsEnabled() && _diagnostics.IsEnabled(DiagnosticOperations.MessageHandling, @event))
                {
                    activity = new Activity(DiagnosticOperations.MessageHandling);
                    if (@event.BasicProperties != null)
                    {
                        if (!string.IsNullOrEmpty(@event.BasicProperties.CorrelationId)) activity.SetParentId(@event.BasicProperties.CorrelationId);
                        if (@event.BasicProperties.Headers != null && @event.BasicProperties.Headers.Any()) @event.BasicProperties.Headers.ToList().ForEach(i => activity.AddBaggage(i.Key, i.Value.ToString()));
                    }

                    _diagnostics.StartActivity(activity, @event);
                }

                try
                {
                    await handler(eventMessage);
                }
                catch (Exception e)
                {
                    if (_diagnostics != null && _diagnostics.IsEnabled() && _diagnostics.IsEnabled(DiagnosticOperations.MessageHandling, @event))
                    {
                        _diagnostics.Write(DiagnosticOperations.MessageHandlingError, e);
                    }
                }
                finally
                {
                    if (_diagnostics != null && _diagnostics.IsEnabled() && _diagnostics.IsEnabled(DiagnosticOperations.MessageHandling, @event) && activity != null)
                    {
                        _diagnostics.StopActivity(activity, @event);
                    }
                }
            }

            consumer.Received += EventHandler;
            channel.BasicConsume(consumer, queueName, true);

            return new Subscription(@namespace, eventTypeFilter, () => consumer.Received -= EventHandler);
        }

        public void Publish(EventMessage eventMessage)
        {
            if (eventMessage == null)
                throw new ArgumentNullException(nameof(eventMessage));

            if (string.IsNullOrEmpty(eventMessage.Namespace))
                throw new ArgumentNullException(nameof(eventMessage.Namespace));

            var properties = new BasicProperties
            {
                ContentType = eventMessage.ContentEncoding,
                ContentEncoding = eventMessage.ContentEncoding,
                Type = eventMessage.EventType,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            };
            
            if (Activity.Current != null)
            {
                properties.Headers = Activity.Current.Baggage.ToDictionary(
                    i => i.Key,
                    i => (object) i.Value);
                properties.MessageId = Activity.Current.Id;
                properties.CorrelationId = Activity.Current.RootId;
            }
            
            _channel.ExchangeDeclare(eventMessage.Namespace, ChannelType);
            _channel.BasicPublish(
                eventMessage.Namespace, 
                (string.IsNullOrEmpty(eventMessage.RoutingKey)? eventMessage.EventType: eventMessage.RoutingKey), 
                properties, eventMessage.MessageBody);
        }
    }
}
