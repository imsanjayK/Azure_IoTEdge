using System;
using System.Collections.Generic;
using System.Text;

namespace MqttNetConverter
{
    public class EquipmentModel
    {
        public string Model { get; set; }
        public string TypeId { get; set; }
        public string Version { get; set; }
        
        public List<EquipmentProperties> Properties { get; set; }
    }
}
