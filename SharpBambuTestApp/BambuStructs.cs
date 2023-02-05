using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambuTestApp
{
    internal class BambuStructs
    {
        /* print job*/
        public struct PrintParams
        {
            /* basic info */
            string dev_id;
            string task_name;
            string project_name;
            string preset_name;
            string filename;
            string config_filename;
            int plate_index;
            string ftp_file;
            string ftp_file_md5;
            string ams_mapping;
            string ams_mapping_info;
            string connection_type;
            string comments;

            /* access options */
            string dev_ip;
            string username;
            string password;

            /*user options */
            bool task_bed_leveling;      /* bed leveling of task */
            bool task_flow_cali;         /* flow calibration of task */
            bool task_vibration_cali;    /* vibration calibration of task */
            bool task_layer_inspect;     /* first layer inspection of task */
            bool task_record_timelapse;  /* record timelapse of task */
            bool task_use_ams;
        };

        public struct PublishParams
        {
            string project_name;
            string project_3mf_file;
            string preset_name;
            string project_model_id;
            string design_id;
            string config_filename;
        };

    }
}
