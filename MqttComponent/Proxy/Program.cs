namespace MqttNetProxy
{
    using MQTTnet.Extensions.ManagedClient;
    using Microsoft.Azure.Devices.Client;
    using MqttNetProxy.MqttHubService;
    using System.Threading.Tasks;
    using System.Threading;
    using MQTTnet.Client;
    using System.Text;
    using MQTTnet;
    using System;
    using MqttNetProxy.IoTHub.DeviceService;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;

    class Program
    {
        static void Main(string[] args)
        {
            Init().Wait();
        }
        static async Task Init()
        {
            Console.WriteLine("Starting Proxy");
            MqttHub mqtt = new MqttHub();
            var _client = mqtt.Client;
            //Handlers
            _client.UseConnectedHandler(e =>
            {
                Console.WriteLine("Connected successfully with MQTT Brokers.");

                //Subscribe to topic
                MqttHub.SubscribeAsync("MessageToProxy").Wait();
           
            });
            string deviceConnectionString = "HostName=iot-abb-dev.azure-devices.net;DeviceId=abb-Device1;SharedAccessKey=WGVpiMpu5noKqH19Ru4xKv75oslkD21sI2wOSzIUq8s=";

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt_Tcp_Only);
                
            var sample = new Device(deviceClient);
            //while (true)
            //{
            //    Console.WriteLine("message");
            //    sample.SendEventAsync("message").Wait();
            //}
           
            await sample.RunSampleAsync().ConfigureAwait(false);
            sample.DeviceClient.SetConnectionStatusChangesHandler(sample.ConnectionStatusChangeHandler);

            _client.UseDisconnectedHandler(e =>
            {
                Console.WriteLine("Disconnected from MQTT Brokers.");
            });
            _client.UseApplicationMessageReceivedHandler(e =>
            {
                Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                Console.WriteLine();
                Console.WriteLine(e.ApplicationMessage.ConvertPayloadToString());
                //Task.Run(() => _client.PublishAsync("hello/world"));

                var payloadIn = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                payloadIn = payloadIn.Substring(payloadIn.IndexOf('{'));
                var jsonData = JsonConvert.DeserializeObject<DeviceTelemetry>(payloadIn);
                Console.WriteLine("message send to cloud");
                sample.SendEventAsync(JsonConvert.SerializeObject(jsonData)).Wait();
            });

            Console.WriteLine("Press key to exit");
            Console.ReadLine();

            Task.Run(() => Thread.Sleep(Timeout.Infinite)).Wait();
            //await _client.StopAsync();
        }

        //private Task MqttMessageReceivedCallbackAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        //{
        //    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
        //    Console.WriteLine($"+ Topic = {eventArgs.ApplicationMessage.Topic}");
        //    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
        //    Console.WriteLine($"+ QoS = {eventArgs.ApplicationMessage.QualityOfServiceLevel}");
        //    Console.WriteLine($"+ Retain = {eventArgs.ApplicationMessage.Retain}");
        //    Console.WriteLine();
        //    Console.WriteLine(eventArgs.ApplicationMessage.ConvertPayloadToString());

        //    var payloadIn = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
        //    payloadIn = payloadIn.Substring(payloadIn.IndexOf('{'));
        //    var jsonData = JsonConvert.DeserializeObject<DeviceTelemetry>(payloadIn);
        //    Console.WriteLine("message send to cloud");
        //    sample.SendEventAsync(JsonConvert.SerializeObject(jsonData)).Wait();
           
        //    return Task.CompletedTask;
        //}
    }
    class DeviceTelemetry
    {
        public string Deviceid { get; set; }
        public Ambient Ambient { get; set; }
    }
    public class Ambient
    {
        public string Temperature { get; set; }
        public string Humidity { get; set; }
    }
}
