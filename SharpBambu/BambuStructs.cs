using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambu
{
    internal class BambuStructs
    {
        /* print job*/
        public struct PrintParams
        {
            /* basic info */
            public string dev_id;
            public string task_name;
            public string project_name;
            public string preset_name;
            public string filename;
            public string config_filename;
            public int plate_index;
            public string ftp_file;
            public string ftp_file_md5;
            public string ams_mapping;
            public string ams_mapping_info;
            public string connection_type;
            public string comments;
            public int origin_profile_id = 0;
            public string origin_model_id;

            /* access options */
            public string dev_ip;
            public bool use_ssl;

            public string username;
            public string password;

            /*user options */
            public bool task_bed_leveling;      /* bed leveling of task */
            public bool task_flow_cali;         /* flow calibration of task */
            public bool task_vibration_cali;    /* vibration calibration of task */
            public bool task_layer_inspect;     /* first layer inspection of task */
            public bool task_record_timelapse;  /* record timelapse of task */
            public bool task_use_ams;
        }
        public struct PublishParams
        {
            public string project_name;
            public string project_3mf_file;
            public string preset_name;
            public string project_model_id;
            public string design_id;
            public string config_filename;
        };

    }
}
