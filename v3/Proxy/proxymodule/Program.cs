namespace proxymodule
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
    using proxymodule.Models;

    class Program
    {
        static int counter;
        private static string _deviceid;
        private static List<string> _messages;
        
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
            
            _messages = new List<string>();
            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("inputP1", PipeMessage, ioTHubModuleClient);

            // Register callback to be called when a direct method message is received by the module
            await ioTHubModuleClient.SetMethodHandlerAsync("cd", PipeDirectMethod, ioTHubModuleClient);
            
        }
        
        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static  Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            Console.WriteLine("Method invoke: PipeMessage");
            int counterValue = Interlocked.Increment(ref counter);

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);

            Console.WriteLine("Storing List" + messageString);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Added successfully");
            _messages.Add(messageString);
            Console.ResetColor();

            //if (!string.IsNullOrEmpty(messageString))
            //{
            //    Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");
            //    var device = JsonConvert.DeserializeObject<Device>(messageString);
            //    //{"Deviceid":"01","Ambient":{"Temperature":"22","Humidity":"23"}} {"deviceid":"01"}
            //    if (_deviceid != null && device.Deviceid == _deviceid)
            //    {
            //        using (var pipeMessage = new Message(messageBytes))
            //        {
            //            foreach (var prop in message.Properties)
            //            {
            //                pipeMessage.Properties.Add(prop.Key, prop.Value);
            //            }
            //            await moduleClient.SendEventAsync("outputP2", pipeMessage);
            //            Console.ForegroundColor = ConsoleColor.Green;
            //            Console.WriteLine("Received message sent to Cloud");
            //            Console.ResetColor();
            //        }
            //    }
            //}
            
            return Task.FromResult(MessageResponse.Completed);
        }

        static async Task<MethodResponse> PipeDirectMethod(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Method invoke: PipeDirectMethod");
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            if (!string.IsNullOrEmpty(data))
            {
                var directMethodRequest = JsonConvert.DeserializeObject<JObject>(methodRequest.DataAsJson);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Received Device id: {directMethodRequest["deviceid"]}");
                Console.WriteLine("Direct method instruction assigned");
                _deviceid = directMethodRequest["deviceid"].ToString();
                Console.ResetColor();
                var moduleClient = userContext as ModuleClient;
                if (moduleClient == null)
                {
                    Console.WriteLine("client null");
                    throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
                }
                int counterValue = Interlocked.Increment(ref counter);

                Console.WriteLine($"Message count: {_messages.ToList().Count}");

                foreach (var messageString in _messages)
                {
                    Console.WriteLine(messageString);
                    if (!string.IsNullOrEmpty(messageString))
                    {
                        Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");
                        var device = JsonConvert.DeserializeObject<Device>(messageString);
                        //{"Deviceid":"01","Ambient":{"Temperature":"22","Humidity":"23"}} {"deviceid":"01"}
                        if (_deviceid != null && device.Deviceid == _deviceid)
                        {
                            using (var pipeMessage = new Message(Encoding.UTF8.GetBytes(messageString)))
                            {
                                await moduleClient.SendEventAsync("outputP2", pipeMessage);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Received message sent to Cloud");
                                Console.ResetColor();
                            }
                        }
                    }
                    else 
                    {
                    Console.WriteLine("Message empty");
                    }
                }
               
                //await moduleClient.SetInputMessageHandlerAsync("inputP1", PipeMessage, moduleClient);
                //using (var pipeMessage = new Message(methodRequest.Data))
                //{
                //    await moduleClient.SendEventAsync("outputP1", pipeMessage);
                //    Console.WriteLine("Received message sent");
                //}

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return new MethodResponse(Encoding.UTF8.GetBytes(result), 400);
            }
        }
    }
}
