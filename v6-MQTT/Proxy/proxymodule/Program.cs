namespace proxymodule
{
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Devices.Client;
    using System.Threading.Tasks;
    using System.Runtime.Loader;
    using System.Threading;
    using Newtonsoft.Json;
    using System.Text;
    using System;
    using MQTTnet;
    using MQTTnet.Client;

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
            //await ioTHubModuleClient.SetMethodHandlerAsync("GetDeviceIdFromDirectMethod", DelegateDirectMethod, ioTHubModuleClient);

            // Register callback to be called when a message is received by the module
            //await ioTHubModuleClient.SetInputMessageHandlerAsync("MessageFromConverter", DelegateMessageEvents, ioTHubModuleClient);

            // Read the Threshold value from the module twin's desired properties
            //var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            //await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, ioTHubModuleClient);

            // Attach a callback for updates to the module twin's desired properties.
            //await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            MqttProvider mqtt = new MqttProvider();
            var mqttClient = mqtt.CreateClient();
            mqttClient.UseConnectedHandler(e =>
            {
                Console.WriteLine("Connected successfully with MQTT Brokers.");
            });
            mqttClient.UseDisconnectedHandler(e =>
            {
                Console.WriteLine("Disconnected from MQTT Brokers.");
            });

            mqttClient.UseApplicationMessageReceivedHandler(DelegateMessageEvents);
            await ioTHubModuleClient.SetMethodHandlerAsync("GetDeviceIdFromDirectMethod", DelegateDirectMethod, mqtt);

        }

        /// <summary>
        /// This method is called whenever the Converter module is sent a message from the EdgeHub. 
        /// It will sent all the incoming messages to Iot Hub.
        /// </summary>
        static Task DelegateMessageEvents(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            Console.WriteLine("DelegateMessageEvents initialized");

            try
            {
                int counterValue = Interlocked.Increment(ref counter);
                string topic = eventArgs.ApplicationMessage.Topic;
                if (string.IsNullOrWhiteSpace(topic) == false)
                {
                    string payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
                    Console.WriteLine($"Received message: {counterValue},Topic: {topic} Body: [{payload}]");
                    if (!string.IsNullOrEmpty(payload))
                    {
                        using (var pipeMessage = new Message(Encoding.UTF8.GetBytes(payload)))
                        {
                            //await moduleClient.SendEventAsync("MessageToHub", pipeMessage);

                            Console.WriteLine("Received message sent");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex);
            }
            return Task.CompletedTask;
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
                if (!(userContext is MqttProvider mqttClient))
                {
                    throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
                }

                Console.WriteLine($"Received Direct method: {methodRequest.Name} sent to Converter module");
                ForegroundColorSuccess(methodRequest.DataAsJson);
                await mqttClient.SendEventsAsync("Module/Proxy/MessageToConverter", methodRequest.DataAsJson);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="desiredProperties"></param>
        /// <param name="userContext"></param>
        /// <returns></returns>
        private static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
            return Task.CompletedTask;
        }
        private static void ForegroundColorSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
