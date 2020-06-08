using Converter;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Converter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    class Program
    {
        static int counter;
        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("Converter module client initialized.");

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("MessageFromProxy", DelegateMessageEvents, ioTHubModuleClient);

        }

        /// <summary>
        /// This method is called whenever the proxy module is sent a message from the EdgeHub. 
        /// It just filter out require device telemetry and pipe the messages to Proxy.
        /// </summary>
        static async Task<MessageResponse> DelegateMessageEvents(Message message, object userContext)
        {
            Console.WriteLine("DelegateMessageEvents inittialized");
            int counterValue = Interlocked.Increment(ref counter);

            if (!(userContext is ModuleClient moduleClient))
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }
            
            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message from {message.ConnectionModuleId}: {counterValue}, Body: [{messageString}]");

            var desireDeviceId = JsonConvert.DeserializeObject<JObject>(messageString)["deviceid"].ToString();
            Console.WriteLine($"Received Device id: {desireDeviceId}");

            var deviceCollection = JsonFileReader();

            foreach (var device in deviceCollection)
            {
                if (device.Deviceid == desireDeviceId)
                {
                    var filterMessage = JsonConvert.SerializeObject(device);
                    Console.WriteLine(filterMessage);
                    var pipeMessage = new Message(Encoding.UTF8.GetBytes(filterMessage));
                    
                    await moduleClient.SendEventAsync("MessageToProxy", pipeMessage);
                    ForegroundColorSuccess($"Sending message: {counterValue}, Body: [{filterMessage}]");
                    break;
                }
            }

            return MessageResponse.Completed;
        }

        /// <summary>
        /// This method is use to get Data. 
        /// </summary>
        private static List<Device> JsonFileReader()
        {
            //using (var stream = new StreamReader(jsonFile))
            //{
            //    devices = JsonConvert.DeserializeObject<List<Device>>(stream.ReadToEnd());
            //}
            var jsonString = "[" +
                   "{\"deviceid\": \"01\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                   "{\"deviceid\": \"02\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                   "{\"deviceid\": \"03\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                   "{\"deviceid\": \"04\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }," +
                   "{\"deviceid\": \"05\",\"ambient\": {\"temperature\": \"22\",\"humidity\": \"23\"} }" +
                   "]";
            //var jsonFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"CpmDB.json"));

            List<Device> devices = JsonConvert.DeserializeObject<List<Device>>(jsonString);

            return devices;
        }

        private static void ForegroundColorSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}