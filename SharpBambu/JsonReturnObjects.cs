using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambu
{
    public class MessageDto
    {
        [JsonProperty("print")]
        public PrinterMessage PrinterMessage { get; set; }
    }

    public class AmsRoot
    {
        [JsonProperty("ams")]
        public List<AmsEntry> AmsList { get; } = new List<AmsEntry>();

        [JsonProperty("ams_exist_bits")]
        public string AmsExistBits { get; set; }

        [JsonProperty("ams_new_detect_flag")]
        public bool? AmsNewDetectFlag { get; set; }

        [JsonProperty("insert_flag")]
        public bool? InsertFlag { get; set; }

        [JsonProperty("power_on_flag")]
        public bool? PowerOnFlag { get; set; }

        [JsonProperty("tray_exist_bits")]
        public string TrayExistBits { get; set; }

        [JsonProperty("tray_is_bbl_bits")]
        public string TrayIsBblBits { get; set; }

        [JsonProperty("tray_now")]
        public string TrayNow { get; set; }

        [JsonProperty("tray_read_done_bits")]
        public string TrayReadDoneBits { get; set; }

        [JsonProperty("tray_reading_bits")]
        public string TrayReadingBits { get; set; }

        [JsonProperty("tray_tar")]
        public string TrayTar { get; set; }

        [JsonProperty("version")]
        public int? Version { get; set; }
    }

    public class AmsEntry
    {
        [JsonProperty("humidity")]
        public string Humidity { get; set; }

        [JsonProperty("id")]
        public string AmsId { get; set; }

        [JsonProperty("temp")]
        public string Temp { get; set; }

        [JsonProperty("tray")]
        public List<Tray> TrayList { get; } = new List<Tray>();
    }
    public class Ipcam
    {
        [JsonProperty("ipcam_dev")]
        public string IpcamDev { get; set; }

        [JsonProperty("ipcam_record")]
        public string IpcamRecord { get; set; }

        [JsonProperty("timelapse")]
        public string Timelapse { get; set; }
    }

    public class LightsReport
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("node")]
        public string Node { get; set; }
    }

    public class Online
    {
        [JsonProperty("ahb")]
        public bool? Ahb { get; set; }

        [JsonProperty("rfid")]
        public bool? Rfid { get; set; }
    }

    public class PrinterMessage
    {
        [JsonProperty("ams")]
        public AmsRoot AmsRoot { get; set; }

        [JsonProperty("ams_rfid_status")]
        public int? AmsRfidStatus { get; set; }

        [JsonProperty("ams_status")]
        public double? AmsStatus { get; set; }

        [JsonProperty("bed_target_temper")]
        public double? BedTargetTemper { get; set; }

        [JsonProperty("bed_temper")]
        public double? BedTemper { get; set; }

        [JsonProperty("big_fan1_speed")]
        public string BigFan1Speed { get; set; }

        [JsonProperty("big_fan2_speed")]
        public string BigFan2Speed { get; set; }

        [JsonProperty("chamber_temper")]
        public double? ChamberTemper { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("cooling_fan_speed")]
        public string CoolingFanSpeed { get; set; }

        [JsonProperty("fail_reason")]
        public string FailReason { get; set; }

        [JsonProperty("force_upgrade")]
        public bool? ForceUpgrade { get; set; }

        [JsonProperty("gcode_file")]
        public string GcodeFile { get; set; }

        [JsonProperty("gcode_file_prepare_percent")]
        public string GcodeFilePreparePercent { get; set; }

        [JsonProperty("gcode_start_time")]
        public string GcodeStartTime { get; set; }

        [JsonProperty("gcode_state")]
        public string GcodeState { get; set; }

        [JsonProperty("heatbreak_fan_speed")]
        public string HeatbreakFanSpeed { get; set; }

        [JsonProperty("hms")]
        public List<object> Hms { get; } = new List<object>();

        [JsonProperty("home_flag")]
        public int? HomeFlag { get; set; }

        [JsonProperty("hw_switch_state")]
        public int? HwSwitchState { get; set; }

        [JsonProperty("ipcam")]
        public Ipcam Ipcam { get; set; }

        [JsonProperty("lifecycle")]
        public string Lifecycle { get; set; }

        [JsonProperty("lights_report")]
        public List<LightsReport> LightsReport { get; } = new List<LightsReport>();

        [JsonProperty("mc_percent")]
        public int? McPercent { get; set; }

        [JsonProperty("mc_print_error_code")]
        public string McPrintErrorCode { get; set; }

        [JsonProperty("mc_print_stage")]
        public string McPrintStage { get; set; }

        [JsonProperty("mc_print_sub_stage")]
        public int? McPrintSubStage { get; set; }

        [JsonProperty("mc_remaining_time")]
        public int? McRemainingTime { get; set; }

        [JsonProperty("mess_production_state")]
        public string MessProductionState { get; set; }

        [JsonProperty("msg")]
        public int? Msg { get; set; }

        [JsonProperty("nozzle_target_temper")]
        public double? NozzleTargetTemper { get; set; }

        [JsonProperty("nozzle_temper")]
        public double? NozzleTemper { get; set; }

        [JsonProperty("online")]
        public Online Online { get; set; }

        [JsonProperty("print_error")]
        public int? PrintError { get; set; }

        [JsonProperty("print_gcode_action")]
        public int? PrintGcodeAction { get; set; }

        [JsonProperty("print_real_action")]
        public int? PrintRealAction { get; set; }

        [JsonProperty("print_type")]
        public string PrintType { get; set; }

        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }

        [JsonProperty("project_id")]
        public string ProjectId { get; set; }

        [JsonProperty("sdcard")]
        public bool? Sdcard { get; set; }

        [JsonProperty("sequence_id")]
        public string SequenceId { get; set; }

        [JsonProperty("spd_lvl")]
        public int? SpdLvl { get; set; }

        [JsonProperty("spd_mag")]
        public int? SpdMag { get; set; }

        [JsonProperty("stg")]
        public List<object> Stg { get; } = new List<object>();

        [JsonProperty("stg_cur")]
        public int? StgCur { get; set; }

        [JsonProperty("subtask_id")]
        public string SubtaskId { get; set; }

        [JsonProperty("subtask_name")]
        public string SubtaskName { get; set; }

        [JsonProperty("task_id")]
        public string TaskId { get; set; }

        [JsonProperty("upgrade_state")]
        public UpgradeState UpgradeState { get; set; }

        [JsonProperty("upload")]
        public Upload Upload { get; set; }

        [JsonProperty("wifi_signal")]
        public string WifiSignal { get; set; }

        [JsonProperty("xcam")]
        public Xcam Xcam { get; set; }

        [JsonProperty("xcam_status")]
        public string XcamStatus { get; set; }

        [JsonProperty("param")]
        public string Param { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public class Tray
    {
        [JsonProperty("bed_temp")]
        public string BedTemp { get; set; }

        [JsonProperty("bed_temp_type")]
        public string BedTempType { get; set; }

        [JsonProperty("drying_temp")]
        public string DryingTemp { get; set; }

        [JsonProperty("drying_time")]
        public string DryingTime { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("nozzle_temp_max")]
        public string NozzleTempMax { get; set; }

        [JsonProperty("nozzle_temp_min")]
        public string NozzleTempMin { get; set; }

        [JsonProperty("tag_uid")]
        public string TagUid { get; set; }

        [JsonProperty("tray_color")]
        public string TrayColor { get; set; }

        [JsonProperty("tray_diameter")]
        public string TrayDiameter { get; set; }

        [JsonProperty("tray_id_name")]
        public string TrayIdName { get; set; }

        [JsonProperty("tray_info_idx")]
        public string TrayInfoIdx { get; set; }

        [JsonProperty("tray_sub_brands")]
        public string TraySubBrands { get; set; }

        [JsonProperty("tray_type")]
        public string TrayType { get; set; }

        [JsonProperty("tray_uuid")]
        public string TrayUuid { get; set; }

        [JsonProperty("tray_weight")]
        public string TrayWeight { get; set; }

        [JsonProperty("xcam_info")]
        public string XcamInfo { get; set; }
    }

    public class UpgradeState
    {
        [JsonProperty("ahb_new_version_number")]
        public string AhbNewVersionNumber { get; set; }

        [JsonProperty("ams_new_version_number")]
        public string AmsNewVersionNumber { get; set; }

        [JsonProperty("consistency_request")]
        public bool? ConsistencyRequest { get; set; }

        [JsonProperty("dis_state")]
        public int? DisState { get; set; }

        [JsonProperty("err_code")]
        public int? ErrCode { get; set; }

        [JsonProperty("force_upgrade")]
        public bool? ForceUpgrade { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("module")]
        public string Module { get; set; }

        [JsonProperty("new_version_state")]
        public int? NewVersionState { get; set; }

        [JsonProperty("ota_new_version_number")]
        public string OtaNewVersionNumber { get; set; }

        [JsonProperty("progress")]
        public string Progress { get; set; }

        [JsonProperty("sequence_id")]
        public int? SequenceId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class Upload
    {
        [JsonProperty("file_size")]
        public int? FileSize { get; set; }

        [JsonProperty("finish_size")]
        public int? FinishSize { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("oss_url")]
        public string OssUrl { get; set; }

        [JsonProperty("progress")]
        public int? Progress { get; set; }

        [JsonProperty("sequence_id")]
        public string SequenceId { get; set; }

        [JsonProperty("speed")]
        public int? Speed { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("task_id")]
        public string TaskId { get; set; }

        [JsonProperty("time_remaining")]
        public int? TimeRemaining { get; set; }

        [JsonProperty("trouble_id")]
        public string TroubleId { get; set; }
    }

    public class Xcam
    {
        [JsonProperty("first_layer_inspector")]
        public bool? FirstLayerInspector { get; set; }

        [JsonProperty("print_halt")]
        public bool? PrintHalt { get; set; }

        [JsonProperty("spaghetti_detector")]
        public bool? SpaghettiDetector { get; set; }
    }
}
