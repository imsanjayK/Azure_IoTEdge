﻿namespace MqttNetConverter.MqttHubService
{
    using MQTTnet.Extensions.ManagedClient;
    using MQTTnet.Client.Publishing;
    using System.Threading.Tasks;
    using MQTTnet.Client.Options;
    using System.Threading;
    using MQTTnet;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Net;
    using System.Security.Authentication;

    class MqttHub
    {
        private static IManagedMqttClient _client;
        private static readonly AutoResetEvent _closing = new AutoResetEvent(false);
        public IManagedMqttClient Client
        {
            get
            {
                return _client;
            }
        }
        static MqttHub()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                return true;
            };
            string clientId = Guid.NewGuid().ToString();
            string mqttURI = "localhost";
            string mqttUser = "";
            string mqttPassword = "";
            int mqttPort = 1883;
            bool mqttSecure = true;

            //configure options
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId("converter")
                .WithTcpServer("localhost", 8883)
                .WithCredentials("sanjay", "%Welcome@123%")
                .WithCleanSession(false);
                //.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.Unknown);



            var _options = mqttSecure ? optionsBuilder.WithTls(new MqttClientOptionsBuilderTlsParameters()
            {
                AllowUntrustedCertificates = true,
                UseTls = true,
                SslProtocol = SslProtocols.Tls11,
                Certificates = new List<X509Certificate>(){
                                  new X509Certificate2(@"C:\Certs\client.crt")
                },
                CertificateValidationHandler = delegate { return true; },
                IgnoreCertificateChainErrors = false,
                IgnoreCertificateRevocationErrors = false
            }).Build() : optionsBuilder.Build();



            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(_options)
                .Build();

            _client = new MqttFactory().CreateManagedMqttClient();

            //actually connect
            _client.StartAsync(managedOptions).Wait();
           
        }

        public static async Task<MqttClientPublishResult> PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1)
        {
            var messageBuilder = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .WithRetainFlag(retainFlag)
                .Build();

            var publishResult = await _client.PublishAsync(messageBuilder);
            return publishResult;
        }

        public static async Task SubscribeAsync(string topic, int qos = 1)
        {
            var topicFilter = new MqttTopicFilterBuilder()
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .WithTopic(topic)
                .Build();

            await _client.SubscribeAsync(topicFilter);
        }
    }
}
