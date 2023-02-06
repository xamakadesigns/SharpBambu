#ifndef __BAMBU_NETWORKING_HPP__
#define __BAMBU_NETWORKING_HPP__

#define BOOST_USE_WINAPI_VERSION 0x0600

#include <string>
#include <functional>
#include <boost/log/trivial.hpp>
#include <boost/format.hpp>
#include <boost/filesystem.hpp>
#include <vector> 
#include <map> 

#include "nlohmann/json.hpp"
using json = nlohmann::json;

namespace BBL {

#define BAMBU_NETWORK_SUCCESS                           0
#define BAMBU_NETWORK_ERR_INVALID_HANDLE                -1
#define BAMBU_NETWORK_ERR_CONNECT_FAILED                -2
#define BAMBU_NETWORK_ERR_DISCONNECT_FAILED             -3
#define BAMBU_NETWORK_ERR_SEND_MSG_FAILED               -4
#define BAMBU_NETWORK_ERR_BIND_FAILED                   -5
#define BAMBU_NETWORK_ERR_UNBIND_FAILED                 -6
#define BAMBU_NETWORK_ERR_PRINT_FAILED                  -7
#define BAMBU_NETWORK_ERR_LOCAL_PRINT_FAILED            -8
#define BAMBU_NETWORK_ERR_REQUEST_SETTING_FAILED        -9
#define BAMBU_NETWORK_ERR_PUT_SETTING_FAILED            -10
#define BAMBU_NETWORK_ERR_GET_SETTING_LIST_FAILED       -11
#define BAMBU_NETWORK_ERR_DEL_SETTING_FAILED            -12
#define BAMBU_NETWORK_ERR_GET_USER_PRINTINFO_FAILED     -13
#define BAMBU_NETWORK_ERR_GET_PRINTER_FIRMWARE_FAILED   -14
#define BAMBU_NETWORK_ERR_QUERY_BIND_INFO_FAILED        -15
#define BAMBU_NETWORK_ERR_MODIFY_PRINTER_NAME_FAILED    -16
#define BAMBU_NETWORK_ERR_FILE_NOT_EXIST                -17
#define BAMBU_NETWORK_ERR_FILE_OVER_SIZE                -18
#define BAMBU_NETWORK_ERR_CHECK_MD5_FAILED              -19
#define BAMBU_NETWORK_ERR_TIMEOUT                       -20
#define BAMBU_NETWORK_ERR_CANCELED                      -21
#define BAMBU_NETWORK_ERR_INVALID_PARAMS                -22
#define BAMBU_NETWORK_ERR_INVALID_RESULT                -23
#define BAMBU_NETWORK_ERR_FTP_UPLOAD_FAILED             -24
#define BAMBU_NETWORK_ERR_FTP_LOGIN_DENIED              -25


//#define BAMBU_NETWORK_LIBRARY               "bambu_networking"
#define BAMBU_NETWORK_AGENT_NAME            "bambu_network_agent"
#define BAMBU_NETWORK_AGENT_VERSION         "01.03.00.02"


//iot preset type strings
#define IOT_PRINTER_TYPE_STRING     "printer"
#define IOT_FILAMENT_STRING         "filament"
#define IOT_PRINT_TYPE_STRING       "print"

#define IOT_JSON_KEY_VERSION            "version"
#define IOT_JSON_KEY_NAME               "name"
#define IOT_JSON_KEY_TYPE               "type"
#define IOT_JSON_KEY_UPDATE_TIME        "updated_time"
#define IOT_JSON_KEY_BASE_ID            "base_id"
#define IOT_JSON_KEY_SETTING_ID         "setting_id"
#define IOT_JSON_KEY_FILAMENT_ID        "filament_id"
#define IOT_JSON_KEY_USER_ID            "user_id"


// https://stackoverflow.com/questions/19044556/passing-managed-function-to-unmanaged-function-that-uses-stdfunction

    // OnUserLoginFn
    // OnPrinterConnectedFn
    // OnLocalConnectedFn
typedef void (*OnServerConnectedFnCS)(void);
typedef void (*OnLocalConnectedFnCS)(int status, BSTR dev_id, BSTR msg);
typedef void (*OnMessageFnCS)(BSTR dev_id, BSTR msg);
typedef void (*OnGetCameraUrlCS)(BSTR cameraUrl);
typedef void (*OnPrinterConnectedFnCS)(BSTR topic_str);
typedef void (*OnMsgArrivedFnCS)(BSTR dev_info_json_str);
typedef void (*OnUpdateStatusFnCS)(int status, int code, BSTR msg);
typedef bool (*WasCancelledFnCS)();
typedef bool (*OnCancelFnCS)();

// OnMessageFn
// OnHttpErrorFn
// GetCountryCodeFn
// OnUpdateStatusFn
// WasCancelledFn
// OnMsgArrivedFn
// ProgressFn
// LoginFn
// ResultFn
// CancelFn

// user callbacks
typedef std::function<void(int online_login, bool login)> OnUserLoginFn;
// printer callbacks
typedef std::function<void(std::string topic_str)>  OnPrinterConnectedFn;
typedef std::function<void(int status, std::string dev_id, std::string msg)> OnLocalConnectedFn;
typedef std::function<void()>                       OnServerConnectedFn;
typedef std::function<void(std::string dev_id, std::string msg)> OnMessageFn;
// http callbacks
typedef std::function<void(unsigned http_code, std::string http_body)> OnHttpErrorFn;
typedef std::function<std::string()>                GetCountryCodeFn;
// print callbacks
typedef std::function<void(int status, int code, std::string msg)> OnUpdateStatusFn;
typedef std::function<bool()>                       WasCancelledFn;
// local callbacks
typedef std::function<void(std::string dev_info_json_str)> OnMsgArrivedFn;

typedef std::function<void(int progress)> ProgressFn;
typedef std::function<void(int retcode, std::string info)> LoginFn;
typedef std::function<void(int result, std::string info)> ResultFn;
typedef std::function<bool()> CancelFn;

enum SendingPrintJobStage {
    PrintingStageCreate = 0,
    PrintingStageUpload = 1,
    PrintingStageWaiting = 2,
    PrintingStageSending = 3,
    PrintingStageRecord  = 4,
    PrintingStageFinished = 5,
};

enum PublishingStage {
    PublishingCreate    = 0,
    PublishingUpload    = 1,
    PublishingWaiting   = 2,
    PublishingJumpUrl   = 3,
};

enum BindJobStage {
    LoginStageConnect = 0,
    LoginStageLogin = 1,
    LoginStageWaitForLogin = 2,
    LoginStageGetIdentify = 3,
    LoginStageWaitAuth = 4,
    LoginStageFinished = 5,
};

enum ConnectStatus {
    ConnectStatusOk = 0,
    ConnectStatusFailed = 1,
    ConnectStatusLost = 2,
};


/* print job*/
struct PrintParams {
    /* basic info */
    std::string     dev_id;
    std::string     task_name;
    std::string     project_name;
    std::string     preset_name;
    std::string     filename;
    std::string     config_filename;
    int             plate_index;
    std::string     ftp_file;
    std::string     ftp_file_md5;
    std::string     ams_mapping;
    std::string     ams_mapping_info;
    std::string     connection_type;
    std::string     comments;
    //int             origin_profile_id = 0;
    //std::string     origin_model_id;

    /* access options */
    std::string     dev_ip;
    bool            use_ssl;
    std::string     username;
    std::string     password;

    /*user options */
    bool            task_bed_leveling;      /* bed leveling of task */
    bool            task_flow_cali;         /* flow calibration of task */
    bool            task_vibration_cali;    /* vibration calibration of task */
    bool            task_layer_inspect;     /* first layer inspection of task */
    bool            task_record_timelapse;  /* record timelapse of task */
    bool            task_use_ams;
};

void to_json(json& j, const PrintParams& p) {
    j = json{
        {"dev_id", p.dev_id},
        {"task_name", p.task_name},
        {"project_name", p.project_name},
        {"preset_name", p.preset_name},
        {"filename", p.filename},
        {"config_filename", p.config_filename},
        {"plate_index", p.plate_index},
        {"ftp_file", p.ftp_file},
        {"ftp_file_md5", p.ftp_file_md5},
        {"ams_mapping", p.ams_mapping},
        {"ams_mapping_info", p.ams_mapping_info},
        {"connection_type", p.connection_type},
        {"comments", p.comments},
        //{"origin_profile_id", p.origin_profile_id},
        //{"origin_model_id", p.origin_model_id},
        {"dev_ip", p.dev_ip},
        {"use_ssl", p.use_ssl},
        {"username", p.username},
        {"password", p.password},
        {"task_bed_leveling", p.task_bed_leveling},
        {"task_flow_cali", p.task_flow_cali},
        {"task_vibration_cali", p.task_vibration_cali},
        {"task_layer_inspect", p.task_layer_inspect},
        {"task_record_timelapse", p.task_record_timelapse},
        {"task_use_ams", p.task_use_ams},
    };
}

void from_json(const json& j, PrintParams& p) {
    j.at("dev_id").get_to(p.dev_id);
    j.at("task_name").get_to(p.task_name);
    j.at("project_name").get_to(p.project_name);
    j.at("preset_name").get_to(p.preset_name);
    j.at("filename").get_to(p.filename);
    j.at("config_filename").get_to(p.config_filename);
    j.at("plate_index").get_to(p.plate_index);
    j.at("ftp_file").get_to(p.ftp_file);
    j.at("ftp_file_md5").get_to(p.ftp_file_md5);
    j.at("ams_mapping").get_to(p.ams_mapping);
    j.at("connection_type").get_to(p.connection_type);
    j.at("comments").get_to(p.comments);
    //j.at("origin_profile_id").get_to(p.origin_profile_id);
    //j.at("origin_model_id").get_to(p.origin_model_id);
    j.at("dev_ip").get_to(p.dev_ip);
    j.at("use_ssl").get_to(p.use_ssl);
    j.at("username").get_to(p.username);
    j.at("password").get_to(p.password);
    j.at("task_bed_leveling").get_to(p.task_bed_leveling);
    j.at("task_flow_cali").get_to(p.task_flow_cali);
    j.at("task_vibration_cali").get_to(p.task_vibration_cali);
    j.at("task_layer_inspect").get_to(p.task_layer_inspect);
    j.at("task_record_timelapse").get_to(p.task_record_timelapse);
    j.at("task_use_ams").get_to(p.task_use_ams);
}

struct PublishParams {
    std::string     project_name;
    std::string     project_3mf_file;
    std::string     preset_name;
    std::string     project_model_id;
    std::string     design_id;
    std::string     config_filename;
};

void to_json(json& j, const PublishParams& p) {
    j = json{
        {"project_name", p.project_name},
        {"project_3mf_file", p.project_3mf_file},
        {"preset_name", p.preset_name},
        {"project_model_id", p.project_model_id},
        {"design_id", p.design_id},
        {"config_filename", p.config_filename},
    };
}

void from_json(const json& j, PublishParams& p) {
    j.at("project_name").get_to(p.project_name);
    j.at("project_3mf_file").get_to(p.project_3mf_file);
    j.at("preset_name").get_to(p.preset_name);
    j.at("project_model_id").get_to(p.project_model_id);
    j.at("design_id").get_to(p.design_id);
    j.at("config_filename").get_to(p.config_filename);
}

}

#endif
