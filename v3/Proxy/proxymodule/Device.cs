namespace proxymodule.Models
{
    public class Device
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

