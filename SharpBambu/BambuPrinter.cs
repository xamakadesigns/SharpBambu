using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambu
{
    public class BambuPrinter
    {
        public static ILogger Log { get; private set; } = Serilog.Log.ForContext<BambuPrinter>();

        internal BambuPrinter()
        {
            AmsList.Add(new BambuAms("0"));
            AmsList.Add(new BambuAms("1"));
            AmsList.Add(new BambuAms("2"));
            AmsList.Add(new BambuAms("3"));
        }

        public double ChamberTemperature { get; private set; }
        public double BedTemperature { get; private set; }
        public double NozzleTemperature { get; private set; }
        public double BedTargetTemperature { get; private set; }
        public double NozzleTargetTemperature { get; private set; }
        public List <BambuAms> AmsList { get; } = new List<BambuAms>();
        public string CameraUrl { get; internal set; }
        public int GcodeSequenceNumber { get; internal set; } = 20000;

        internal void ProcessMessage(PrinterMessage message)
        {
            if (message == null)
                return;

            switch (message.Command)
            {
                case "push_status":

                    ChamberTemperature = message.ChamberTemper ?? ChamberTemperature;
                    BedTemperature = message.BedTemper ?? BedTemperature;
                    BedTargetTemperature = message.BedTargetTemper ?? BedTargetTemperature;
                    NozzleTemperature = message.NozzleTemper ?? NozzleTemperature;
                    NozzleTargetTemperature = message.NozzleTargetTemper ?? NozzleTargetTemperature;

                    if (message.AmsRoot != null)
                    {
                        foreach (var amsStatus in message.AmsRoot.AmsList)
                        {
                            var amsUnit = AmsList[int.Parse(amsStatus.AmsId)];
                            amsUnit.UpdateStatus(amsStatus);
                        }
                    }

                    Log.Verbose("Bed Temp {BedTemperature}/{BedTargetTemperature}, " +
                        "Extruder Temp {NozzleTemperature}/{NozzleTargetTemperature}, " +
                        "Chamber Temp {ChamberTemperature}, AMS0 Humidity {Ams0_Humidity}", BedTemperature, BedTargetTemperature, NozzleTemperature, NozzleTargetTemperature, ChamberTemperature, AmsList[0].Humidity);

                    break;

                case "gcode_line":
                    Log.Verbose("Ack: Gcode seq {SequenceId} result {Result}, reason {Reason}", message.SequenceId, message.Result, message.Reason);
                    break;

                default:
                    break;

            }
        }
    }
}
