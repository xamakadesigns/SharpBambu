using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambu
{
    public class BambuAms
    {
        public static ILogger Log { get; private set; } = Serilog.Log.ForContext<BambuAms>();

        public double Humidity { get; private set; }

        internal BambuAms(string amsId)
        {
            AmsId = amsId;
        }

        public string AmsId { get; }

        internal void UpdateStatus(AmsEntry amsStatus)
        {
            double humidity = 0;
            
            if (double.TryParse(amsStatus.Humidity, out humidity))
                Humidity = humidity;
        }
    }
}
