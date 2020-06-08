namespace MqttNetProxy.IoTHub.DeviceService
{
    using Microsoft.Azure.Devices.Client;
    using MqttNetProxy.MqttHubService;
    using System.Threading.Tasks;
    using System.Text;
    using System;
    using System.IO;
    using MQTTnet;

    public class Device
    {
        private readonly DeviceClient _client;
        public DeviceClient DeviceClient
        {
            get
            {
                return _client;
            }
        }
        public Device (DeviceClient deviceClient)
        {

            _client = deviceClient;
            _client.OpenAsync();
        }


        public async Task SendEventAsync(string messageString)
        {
            using(var ms = new MemoryStream())
            using (var message = new Message(Encoding.UTF8.GetBytes(messageString)))
            {
                await _client.SendEventAsync(message);
            }
        }
        public async Task RunSampleAsync()
        {
            _client.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            await _client.SetMethodHandlerAsync("GetDesireDeviceId", SendMessageToConverter, null).ConfigureAwait(false);
           
            //Console.WriteLine("Enter");
            //var res = SendMessage(Console.ReadLine()).Result;
           

            //await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        }
        public void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine();
            Console.WriteLine($"Connection status changed to {status}.");
            Console.WriteLine($"Connection status changed reason is {reason}.");
            Console.WriteLine();
        }
       
        private async Task<MethodResponse> SendMessageToConverter(MethodRequest methodRequest, object userContext)
        {
            if (!string.IsNullOrEmpty(methodRequest.DataAsJson))
            {
                //if (!(userContext is MqttHub mqttClient))
                //{
                //    throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
                //}

                Console.WriteLine($"Received Direct method: {methodRequest.Name} sent to Converter module");
                ForegroundColorSuccess(methodRequest.DataAsJson);
                await MqttHub.PublishAsync("MessageToConverter", methodRequest.DataAsJson);

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
