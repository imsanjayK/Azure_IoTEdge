using System;
using System.Collections.Generic;
using System.Text;

namespace MqttNetConverter
{
    public class CPMBaseFormat
    {
        public string Model { get; set; }
        public string TypeId { get; set; }
        public string Version { get; set; }
        public string Property { get; set; }
        public string DataTypeExt { get; set; }
        public string Type { get; set; }
    }
}
