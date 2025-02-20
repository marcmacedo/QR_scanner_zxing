using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QR_scanner_zxing.Models
{

    //internal class Record
    //{
    //    public string Timestamp { get; set; }
    //    public string Temperatura { get; set; }
    //    public string Count { get; set; }
    //}

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


        //public Dictionary<int, (long timestamp, double temp)> Records { get; set; }

        //public SensorData()
        //{
        //    Records = new Dictionary<int, (long, double)>();
        //}

        //public List<Record> Records { get; set; } = new List<Record>();
    }
}
