using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QR_scanner_zxing.Models
{

    public class Record
    {
        public string Timestamp { get; set; }
        public string Temperatura { get; set; }
    }

    public class SensorData
    {
        public string Model { get; set; }
        public string Address { get; set; }
        public string Battery { get; set; }
        public string StartDate { get; set; }
        public string CurrentDate { get; set; }
        public string CurrentTemp { get; set; }


        private Dictionary<int, (long timestamp, double temp)> _recordsDict = new Dictionary<int, (long, double)>();

        public List<Record> Records => _recordsDict
            .Select(kvp => new Record
            {
                Timestamp = kvp.Value.timestamp.ToString(),
                Temperatura = kvp.Value.temp.ToString("F2")
            })
            .ToList();


        public void AddRecord(int index, long timestamp, double temp)
        {
            if (!_recordsDict.ContainsKey(index))
            {
                _recordsDict[index] = (timestamp, temp);
            }
            else
            {
                Console.WriteLine($"Registro duplicado ignorado: Index {index}, Timestamp {timestamp}");
            }
        }

        public List<object> ToTagoFormat(int lastSentIndex = 0)
        {
            var data = new List<object>
            {
                new { variable = "device_name", value = this.Model },
                new { variable = "device_address", value = this.Address },
                new { variable = "battery", value = this.Battery, unit = "%" },
                new { variable = "start_date", value = this.StartDate },
                new { variable = "current_date", value = this.CurrentDate },
                new { variable = "current_temperature", value = this.CurrentTemp, unit = "C" },
            };

            var newData = _recordsDict
                .Skip(lastSentIndex)
                .ToList();

            foreach (var kvp in newData)
            {

                long timestamp = kvp.Value.timestamp;

                string formattedTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-ddTHH:mm:ssZ");

                data.Add(new
                {
                    variable = "Temperature",
                    value = kvp.Value.temp,
                    unit = "C",
                    time = formattedTime
                });
            }
            return data;
        }
    }
}
