namespace MqttNetConverter
{
    using MQTTnet.Extensions.ManagedClient;
    using MqttNetConverter.MqttHubService;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using System.Threading;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Text;
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Converter....");
            try
            {
                MqttHub mqtt = new MqttHub();
                var _client = mqtt.Client;

                //Handlers
                _client.UseConnectedHandler(e =>
                {
                    Console.WriteLine("Connected successfully with MQTT Brokers.");

                    //Subscribe to topic

                    MqttHub.SubscribeAsync("MessageToConverter").Wait();
                });
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

                    var payloadIn = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    payloadIn = payloadIn.Substring(payloadIn.IndexOf('{'));
                       
                    var desireDeviceId = JsonConvert.DeserializeObject<JObject>(payloadIn)["deviceid"]
                                                    .ToString();

                    Console.WriteLine($"Received Device id: {desireDeviceId}");

                    var desireDeviceInfo = GetData(desireDeviceId);
                    var payloadOut = JsonConvert.SerializeObject(desireDeviceInfo);

                    var r = MqttHub.PublishAsync("MessageToProxy", payloadOut, false).Result;

                });

                Console.WriteLine("Press key to exit");
                Console.ReadLine();

                Task.Run(() => Thread.Sleep(Timeout.Infinite)).Wait();
                 //_client.StopAsync();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static DeviceTelemetry GetData(string id)
        {
            var jsonString = "[" +
                  "{\"deviceid\": \"01\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                  "{\"deviceid\": \"02\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                  "{\"deviceid\": \"03\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                  "{\"deviceid\": \"04\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                  "{\"deviceid\": \"05\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }" +
                  "]";
            //var jsonFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"CpmDB.json"));

            List<DeviceTelemetry> devices = JsonConvert.DeserializeObject<List<DeviceTelemetry>>(jsonString);

            return devices.Where(d=> d.Deviceid == id).First();
        }   
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
