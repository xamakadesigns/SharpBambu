using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambu
{
    public class BambuPrinter
    {
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

                    Console.WriteLine($"Bed Temp {BedTemperature}/{BedTargetTemperature}, Extruder Temp {NozzleTemperature}/{NozzleTargetTemperature}, Chamber Temp {ChamberTemperature}, AMS0 Humidity {AmsList[0].Humidity}");
                    break;

                case "gcode_line":
                    Console.WriteLine($"Gcode seq {message.SequenceId} result {message.Result}, reason {message.Reason}");
                    break;

                default:
                    break;

            }

            Console.Write("> ");
        }
    }
}
