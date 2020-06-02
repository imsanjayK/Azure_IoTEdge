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
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            //await ioTHubModuleClient.SetInputMessageHandlerAsync("inputC1", PipeMessage, ioTHubModuleClient);
            await PipeMessage(ioTHubModuleClient);
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        //static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        static async Task<MessageResponse> PipeMessage(object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            var deviceCollection = JsonReader();
            foreach (var device in deviceCollection)
            {
                var messageString = JsonConvert.SerializeObject(device, Formatting.None);

                if (!string.IsNullOrEmpty(messageString))
                {
                    using (var pipeMessage = new Message(Encoding.UTF8.GetBytes(messageString)))
                    {
                        Console.WriteLine($"Sending message: {counterValue}, Body: [{messageString}]");
                        await moduleClient.SendEventAsync("outputC1", pipeMessage);
                    }
                }
            }
            
            return MessageResponse.Completed;
        }

        static List<Device> JsonReader()
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
    }
}