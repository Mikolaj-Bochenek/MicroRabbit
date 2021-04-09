using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus
    {
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handlers;
        private readonly List<Type> _eventTypes;

        public RabbitMQBus(IMediator mediator)
        {
            _mediator = mediator;
            _handlers = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
        }

        // SendCommand is related to our bus sending commands.
        public Task SendCommand<T>(T command) where T : Command => _mediator.Send(command);

        // Publish is related to the events.
        // Publish method is used for different microservices to publish events to the rabbit MQ server.
        public void Publish<T>(T @event) where T : Event
        {
            // Create connection to a RabbitMQ node on the local machine - hence the localhost.
            // Next we create a channel, which is where most of the API for getting things done resides.
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Get event name using reflection.
                var eventName = @event.GetType().Name;

                // Declare a queue with name like event name.
                // Declaring a queue is idempotent - it will only be created if it doesn't exist already.
                channel.QueueDeclare(queue: eventName, durable: false, exclusive: false, arguments: null);

                // The message content is a byte array, so you can encode whatever you like there.
                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);
                
                // Publish event to the rabbit MQ server.
                channel.BasicPublish(exchange: string.Empty, routingKey: eventName, basicProperties: null, body);
            }
        }

        // Subscribe is related to the events.
        // Subscribe method takes an event and also takes the am event handler.
        public void Subscribe<T, TH>() where T : Event where TH : IEventHandler<T>
        {
            // Extract event name
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);

            // Add event type to the type list if not exist yet.
            if (!_eventTypes.Contains(typeof(T)))
                _eventTypes.Add(typeof(T));
            
            // Add new [eventName : eventType list] if the key not exist yet.
            if (!_handlers.ContainsKey(eventName))
                _handlers.Add(eventName, new List<Type>());
            
            // Nake sure it doesn't already have the handler type that we're sending in.
            if (_handlers[eventName].Any(k => k.GetType() == handlerType))
            {
                throw new ArgumentException(
                    message: $"handler Type {handlerType.Name} already is registered for '{eventName}'",
                    paramName: nameof(handlerType)
                );
            }

            _handlers[eventName].Add(handlerType);

            StartBasicConsume<T>();
        }

        // Consumer is an application (or application instance) that consumes messages.
        // The same application can also publish messages and thus be a publisher at the same time.
        private void StartBasicConsume<T>() where T : Event
        {
            // Create connection to a RabbitMQ node on the local machine - hence the localhost.
            // Next we create a channel, which is where most of the API for getting things done resides.
            var factory = new ConnectionFactory()
            { 
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            // NOTE //
            // Only without the using statements will you be able to consume messages that are created while the Transfer project is running.
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            // Get event name using reflection.
            var eventName = typeof(T).Name;

            // Declare a queue with name like event name.
            // Declaring a queue is idempotent - it will only be created if it doesn't exist already.
            channel.QueueDeclare(queue: eventName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            // Create a async basic consumer.
            var consumer = new AsyncEventingBasicConsumer(channel);

            // Add Consumer_Received method to event delgate.
            // Basically listening for any messages in our queue.
            consumer.Received += Consumer_Received;

            channel.BasicConsume(queue: eventName, autoAck: true, consumer);
        }

        // Delegate implementation
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // This is where we make use of reflection activator as we dynamically create the handler based
        // on the handler type in our dictionary of handlers and then invoke the event handler for that type of event.
        // It's very powerful so we have everything in our rabbit MQ bus in one place.
        private async Task ProcessEvent(string eventName, string message)
        {
            // Check if Handlers dictionary contains the key which is event name.
            if (_handlers.ContainsKey(eventName))
            {
                // Store multiple handlers based on the event name.
                var subscriptions = _handlers[eventName];
                foreach (var subscription in subscriptions)
                {
                    // use Activator and create instance of our subscription.
                    var handler = Activator.CreateInstance(subscription);
                    if (handler == null) continue;

                    // Get the type of event.
                    var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);

                    // Deserialize the object to event. 
                    var @event = JsonConvert.DeserializeObject(message, eventType);

                    // Make generic type using the event type
                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                    // Use generics to kickoff the handler method inside of our handler and passing in the event.
                    // Does main work to routing to the right handler.
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                }
            }
        }
    }
}