using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace proxymodule
{
    class MqttProvider
    {
        private static IMqttClient _client;
        private static IMqttClientOptions _options;

        public MqttProvider()
        {
            try
            {
                // Create a new MQTT client.
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

                //configure options
                _options = new MqttClientOptionsBuilder()
                    .WithClientId("ProxyModule")
                    .WithTcpServer("localhost", 1884) // 127.0.0.1
                    .WithCredentials("sanjay", "%Welcome@123%")
                    .WithCleanSession()
                    .Build();

                //connect
                _client.ConnectAsync(_options).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public IMqttClient CreateClient()
        {
            return _client;
        }
        public void MqttProviders()
        {
            Console.WriteLine("Starting Publisher....");
            try
            {
                //handlers
                _client.UseConnectedHandler(e =>
                {
                    Console.WriteLine("Connected successfully with MQTT Brokers.");
                });

                _client.UseDisconnectedHandler(e =>
                {
                    Console.WriteLine("Disconnected from MQTT Brokers.");
                });

                _client.UseApplicationMessageReceivedHandler(e =>
                {
                    try
                    {
                        string topic = e.ApplicationMessage.Topic;
                        if (string.IsNullOrWhiteSpace(topic) == false)
                        {
                            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                            Console.WriteLine($"Topic: {topic}. Message Received: {payload}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message, ex);
                    }
                });

                Console.WriteLine("Press key to publish message.");
                Console.ReadLine();

                //simulating publish
             

                Console.WriteLine("Simulation ended! press any key to exit.");
                Console.ReadLine();

                Task.Run(() => Thread.Sleep(Timeout.Infinite)).Wait();
                _client.DisconnectAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public Task SendEventsAsync(string topicString, string payloadString)
        {
            var message = new MqttApplicationMessageBuilder()
                   .WithTopic(topicString)
                   .WithPayload(payloadString)
                   .WithExactlyOnceQoS()
                   .WithRetainFlag()
                   .Build();

            if (_client.IsConnected)
            {
                Console.WriteLine($"publishing at {DateTime.UtcNow}");
                _client.PublishAsync(message);
            }
            //Thread.Sleep(2000);
            return Task.CompletedTask;
        }
    }
}
