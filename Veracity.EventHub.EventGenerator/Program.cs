﻿using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Veracity.EventHub.Abstraction;
using Veracity.EventHub.RabbitMQ;

namespace Veracity.EventHub.EventGenerator
{
    class Program
    {
        private static string[] events = {"crew_onboard_departure", "wind_speed", "passengers_onboard_departure"};

        static string BodyExample = "{ UniqueId: \"dd12abbd-1276-45aa-b6db-e0908226ae632019-12-19T08:32:42.6502203\", ItemId: \"dd12abbd-1276-45aa-b6db-e0908226ae63\", LocationId: \"9733105\", LocationType: \"Vessel\", OperationEventId: \"Voy-1001\", EventLatLong: \"\", EventTimeUTC: \"2019-12-19T08:32:42.6502203\", LocationTimeZone: \"\", }";
        
        public static void Main(string[] args)
        {
            var rabbit = new RabbitMQHub(new ConnectionFactory
            {
                Endpoint = new AmqpTcpEndpoint(new Uri("amqp://localhost:5672"))
            });
            var routingKeyPrefix = args.Length > 0 ? args[0] : "dnvgl";
            for (var i = 0; i < 1000; ++i)
            {
                var routingKey = $"{routingKeyPrefix}{(i % 10 + 1):0000}";
                rabbit.Publish(new EventMessage
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json",
                    EventType = events[i % events.Length],
                    Namespace = "MaranicsEvent",
                    RoutingKey = routingKey,
                    Timestamp = DateTimeOffset.UtcNow,
                    MessageBody = Encoding.UTF8.GetBytes(BodyExample)
                });

                Console.WriteLine($"Generated event no {i + 1}. (routingKey: {routingKey})");
            }
        }
    }
}
