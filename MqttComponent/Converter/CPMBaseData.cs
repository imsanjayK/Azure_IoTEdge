using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MqttNetConverter
{
    public class CPMBaseData
    {
        public void EquipmentDetails()
        {
            EquipmentModel Result = EquipmentFilteredResults();
            //string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
            var test=  JsonConvert.SerializeObject(Result);
        }

        public EquipmentModel EquipmentFilteredResults()
        {
            EquipmentModel equipmentList = new EquipmentModel();
            List<CPMBaseFormat> baseList = EquipmentsBaseData();
            List<EquipmentProperties> propertiesList = new List<EquipmentProperties>();
            equipmentList.Model = "abb.ability.metadata";
            equipmentList.TypeId = "typeId";
            equipmentList.Version = "1.0.0";
            foreach (var listItem in baseList)
            {
                EquipmentProperties equipmentProperties = new EquipmentProperties();
                 EquipmentTypeExt equipmentTypeExt = new EquipmentTypeExt();
                equipmentTypeExt.DataType = listItem.DataTypeExt.Split("(")[0];
                if (listItem.DataTypeExt.Split("(").Length > 1)
                {
                    int Pos1 = listItem.DataTypeExt.IndexOf("(") + "(".Length;
                    int Pos2 = listItem.DataTypeExt.IndexOf(")");
                    equipmentTypeExt.DataTypeExt = listItem.DataTypeExt.Substring(Pos1, Pos2 - Pos1);
                }
                else
                {
                    equipmentTypeExt.DataTypeExt = string.Empty;
                }
                equipmentProperties.PropertyName = listItem.Property;
                equipmentTypeExt.Type = listItem.Type;
                equipmentProperties.Property = equipmentTypeExt;
                propertiesList.Add(equipmentProperties);
            }
            equipmentList.Properties = propertiesList;
            return equipmentList;
        }
        public List<CPMBaseFormat> EquipmentsBaseData()
        {

            DataTable dt = Tabulate();
            List<CPMBaseFormat> baseList = new List<CPMBaseFormat>();
            foreach (DataRow row in dt.Rows)
            {
                CPMBaseFormat baseObj = new CPMBaseFormat();
                baseObj.Property = row["Property"].ToString();
                baseObj.Type = row["Type"].ToString();
                baseObj.DataTypeExt = row["DataTypeExt"].ToString();
                baseList.Add(baseObj);
            }
            return baseList;
            //string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
            //var test=  JsonConvert.DeserializeObject<List<EquipmentTypeExt>>(json);
        }

        public static DataTable Tabulate()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Property", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("DataTypeExt", typeof(string));
            //dt.Columns.Add("DataTypeExtension", typeof(string));
            dt.Rows.Add("DetectorEmptyPipe2.value", "PM", "Unsigned integer (64bit)");
            dt.Rows.Add("UserSystemSpanRatio.value", "PM", "Integer (64bit)");
            dt.Rows.Add("HardwareVersion", "PM", "String");
            dt.Rows.Add("DetectorEmptyPipe2.unitsCode", "PM", "Integer (64bit)");

            return dt;

        }


    }
}
