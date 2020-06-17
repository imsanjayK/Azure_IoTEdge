using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;


namespace MqttNetConverter
{
    public class EquipmentProperties
    {
        public string PropertyName { get; set; }
        public EquipmentTypeExt Property { get; set; }        
    }

    public class EquipmentTypeExt
    {
        public string DataType { get; set; }
        public string DataTypeExt { get; set; }
        public string Type { get; set; }
       
    }

}
