using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum SignalType { none, connect, data, close }

    [Serializable]
    public class Trans
    {
        public SignalType Signal { get; set; }
        public DateTime Time { get; set; }
        public TimeSpan Interval { get; set; }
        public string Data { get; set; }
        public Trans() { }
        public Trans( SignalType signal )
        {
            Signal = signal;
            Time = DateTime.Now;
        }
        public Trans( SignalType signal, string data ) : this( signal )
        {
            Data = data;
        }
    }
}
