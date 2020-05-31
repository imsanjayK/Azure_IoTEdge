namespace proxymodule
{
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Client;
    using System.Threading.Tasks;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Text;
    using System;
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
            Console.WriteLine("Proxy module client initialized.");

            // Register callback to be called when a direct method message is received by the module
            await ioTHubModuleClient.SetMethodHandlerAsync("GetDeviceIdFromDirectMethod", DelegateDirectMethod, ioTHubModuleClient);

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("MessageFromConverter", DelegateMessageEvents, ioTHubModuleClient);
        }

        /// <summary>
        /// This method is called whenever the Converter module is sent a message from the EdgeHub. 
        /// It will sent all the incoming messages to Iot Hub.
        /// </summary>
        static async Task<MessageResponse> DelegateMessageEvents(Message message, object userContext)
        {
            Console.WriteLine("DelegateMessageEvents initialized");
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                using (var pipeMessage = new Message(messageBytes))
                {
                    foreach (var prop in message.Properties)
                    {
                        pipeMessage.Properties.Add(prop.Key, prop.Value);
                    }
                    await moduleClient.SendEventAsync("MessageToHub", pipeMessage);

                    Console.WriteLine("Received message sent");
                }
            }
            return MessageResponse.Completed;
        }

        /// <summary>
        /// This method is called whenever the Cloud-end service sent a message using Direct method. 
        /// It will pipe the incoming message to converter module.
        /// </summary>
        static async Task<MethodResponse> DelegateDirectMethod(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"Direct method invoked");
            
            if (!string.IsNullOrEmpty(methodRequest.DataAsJson))
            {   
                if (!(userContext is ModuleClient moduleClient))
                {
                    throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
                }

                using (var pipeMessage = new Message(methodRequest.Data))
                {
                    Console.WriteLine($"Received Direct method: {methodRequest.Name} sent to Converter module");
                    ForegroundColorSuccess(methodRequest.DataAsJson);
                    await moduleClient.SendEventAsync("MessageToConverter", pipeMessage);
                }

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

        private static void ForegroundColorSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
