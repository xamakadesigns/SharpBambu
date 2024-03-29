#include "pch.h"

#include <comutil.h>
#include <oleauto.h>
#include <wtypes.h>
#include <iostream>
#include <stdio.h>
#include <stdlib.h>
#if defined(_MSC_VER) || defined(_WIN32)
#include <Windows.h>
#else
#include <dlfcn.h>
#endif

#pragma comment(lib, "comsuppw.lib")
#pragma comment(lib, "kernel32.lib")

#include "NetworkAgent.hpp"
#include "nlohmann/json.hpp"

using json = nlohmann::json;
using namespace std;
using namespace BBL;

namespace Slic3r {

void* network_agent{ nullptr };

#if defined(_MSC_VER) || defined(_WIN32)
HMODULE netwoking_module = NULL;
HMODULE source_module = NULL;
#else
void* netwoking_module = NULL;
void* source_module = NULL;
#endif

std::string g_dll_dir;

__declspec(dllexport) void set_dll_dir(char *dir)
{
    g_dll_dir = dir;
}

const std::string& dll_dir()
{
    return g_dll_dir;
}

std::string g_data_dir;

__declspec(dllexport) void set_data_dir(char* dir)
{
    g_data_dir = dir;
}

const std::string& data_dir()
{
    return g_data_dir;
}

__declspec(dllexport) void* bambu_network_create_agent()
{
    if (create_agent_ptr) {
        network_agent = create_agent_ptr();
    }
    BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(", this %1%, network_agent=%2%, create_agent_ptr=%3%")%0 %network_agent %create_agent_ptr;

    return network_agent;
}

__declspec(dllexport) int bambu_network_destroy_agent()
{
    int ret = 0;
    if (network_agent && destroy_agent_ptr) {
        ret = destroy_agent_ptr(network_agent);
    }
    BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(", this %1%, network_agent=%2%, destroy_agent_ptr=%3%, ret %4%")%0 %network_agent %destroy_agent_ptr %ret;

    return ret;
}

__declspec(dllexport) int initialize_network_module()
{
    //std::string data_dir_str = dll_dir();
    //BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" using data dir %1%") % data_dir();
    boost::filesystem::path data_dir_path(data_dir());
    //auto plugin_folder = data_dir_path / "plugins";

    //first load the library
#if defined(_MSC_VER) || defined(_WIN32)
    std::string library = dll_dir() + "\\bambu_networking.dll";
    
    BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" loading %1%") % library;

    wchar_t lib_wstr[128];
    memset(lib_wstr, 0, sizeof(lib_wstr));
    ::MultiByteToWideChar(CP_UTF8, NULL, library.c_str(), strlen(library.c_str())+1, lib_wstr, sizeof(lib_wstr) / sizeof(lib_wstr[0]));
    netwoking_module = LoadLibrary(lib_wstr);
    /*if (!netwoking_module) {
        library = std::string(BAMBU_NETWORK_LIBRARY) + ".dll";
        memset(lib_wstr, 0, sizeof(lib_wstr));
        ::MultiByteToWideChar(CP_UTF8, NULL, library.c_str(), strlen(library.c_str()) + 1, lib_wstr, sizeof(lib_wstr) / sizeof(lib_wstr[0]));
        netwoking_module = LoadLibrary(lib_wstr);
    }*/
#else
    #if defined(__WXMAC__)
    library = plugin_folder.string() + "/" + std::string("lib") + std::string(BAMBU_NETWORK_LIBRARY) + ".dylib";
    #else
    library = plugin_folder.string() + "/" + std::string("lib") + std::string(BAMBU_NETWORK_LIBRARY) + ".so";
    #endif
    printf("loading network module at %s\n", library.c_str());
    netwoking_module = dlopen( library.c_str(), RTLD_LAZY);
    if (!netwoking_module) {
        /*#if defined(__WXMAC__)
        library = std::string("lib") + BAMBU_NETWORK_LIBRARY + ".dylib";
        #else
        library = std::string("lib") + BAMBU_NETWORK_LIBRARY + ".so";
        #endif*/
        //netwoking_module = dlopen( library.c_str(), RTLD_LAZY);
        char* dll_error = dlerror();
        printf("error, dlerror is %s\n", dll_error);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(", error, dlerror is %1%")%dll_error;
    }
    printf("after dlopen, network_module is %p\n", netwoking_module);
#endif

    if (!netwoking_module) {
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(", Bambu Network Wrapper cannot load library %1%")%library;
        return -1;
    }
    BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(", successfully loaded library %1%, module %2%")%library %netwoking_module;

    //load the functions
    check_debug_consistent_ptr        =  reinterpret_cast<func_check_debug_consistent>(get_network_function("bambu_network_check_debug_consistent"));
    get_version_ptr                   =  reinterpret_cast<func_get_version>(get_network_function("bambu_network_get_version"));
    create_agent_ptr                  =  reinterpret_cast<func_create_agent>(get_network_function("bambu_network_create_agent"));
    destroy_agent_ptr                 =  reinterpret_cast<func_destroy_agent>(get_network_function("bambu_network_destroy_agent"));
    init_log_ptr                      =  reinterpret_cast<func_init_log>(get_network_function("bambu_network_init_log"));
    set_config_dir_ptr                =  reinterpret_cast<func_set_config_dir>(get_network_function("bambu_network_set_config_dir"));
    set_cert_file_ptr                 =  reinterpret_cast<func_set_cert_file>(get_network_function("bambu_network_set_cert_file"));
    set_country_code_ptr              =  reinterpret_cast<func_set_country_code>(get_network_function("bambu_network_set_country_code"));
    start_ptr                         =  reinterpret_cast<func_start>(get_network_function("bambu_network_start"));
    set_on_ssdp_msg_fn_ptr            =  reinterpret_cast<func_set_on_ssdp_msg_fn>(get_network_function("bambu_network_set_on_ssdp_msg_fn"));
    set_on_user_login_fn_ptr          =  reinterpret_cast<func_set_on_user_login_fn>(get_network_function("bambu_network_set_on_user_login_fn"));
    set_on_printer_connected_fn_ptr   =  reinterpret_cast<func_set_on_printer_connected_fn>(get_network_function("bambu_network_set_on_printer_connected_fn"));
    set_on_server_connected_fn_ptr    =  reinterpret_cast<func_set_on_server_connected_fn>(get_network_function("bambu_network_set_on_server_connected_fn"));
    set_on_http_error_fn_ptr          =  reinterpret_cast<func_set_on_http_error_fn>(get_network_function("bambu_network_set_on_http_error_fn"));
    set_get_country_code_fn_ptr       =  reinterpret_cast<func_set_get_country_code_fn>(get_network_function("bambu_network_set_get_country_code_fn"));
    set_on_message_fn_ptr             =  reinterpret_cast<func_set_on_message_fn>(get_network_function("bambu_network_set_on_message_fn"));
    set_on_local_connect_fn_ptr       =  reinterpret_cast<func_set_on_local_connect_fn>(get_network_function("bambu_network_set_on_local_connect_fn"));
    set_on_local_message_fn_ptr       =  reinterpret_cast<func_set_on_local_message_fn>(get_network_function("bambu_network_set_on_local_message_fn"));
    connect_server_ptr                =  reinterpret_cast<func_connect_server>(get_network_function("bambu_network_connect_server"));
    is_server_connected_ptr           =  reinterpret_cast<func_is_server_connected>(get_network_function("bambu_network_is_server_connected"));
    refresh_connection_ptr            =  reinterpret_cast<func_refresh_connection>(get_network_function("bambu_network_refresh_connection"));
    start_subscribe_ptr               =  reinterpret_cast<func_start_subscribe>(get_network_function("bambu_network_start_subscribe"));
    stop_subscribe_ptr                =  reinterpret_cast<func_stop_subscribe>(get_network_function("bambu_network_stop_subscribe"));
    send_message_ptr                  =  reinterpret_cast<func_send_message>(get_network_function("bambu_network_send_message"));
    connect_printer_ptr               =  reinterpret_cast<func_connect_printer>(get_network_function("bambu_network_connect_printer"));
    disconnect_printer_ptr            =  reinterpret_cast<func_disconnect_printer>(get_network_function("bambu_network_disconnect_printer"));
    send_message_to_printer_ptr       =  reinterpret_cast<func_send_message_to_printer>(get_network_function("bambu_network_send_message_to_printer"));
    start_discovery_ptr               =  reinterpret_cast<func_start_discovery>(get_network_function("bambu_network_start_discovery"));
    change_user_ptr                   =  reinterpret_cast<func_change_user>(get_network_function("bambu_network_change_user"));
    is_user_login_ptr                 =  reinterpret_cast<func_is_user_login>(get_network_function("bambu_network_is_user_login"));
    user_logout_ptr                   =  reinterpret_cast<func_user_logout>(get_network_function("bambu_network_user_logout"));
    get_user_id_ptr                   =  reinterpret_cast<func_get_user_id>(get_network_function("bambu_network_get_user_id"));
    get_user_name_ptr                 =  reinterpret_cast<func_get_user_name>(get_network_function("bambu_network_get_user_name"));
    get_user_avatar_ptr               =  reinterpret_cast<func_get_user_avatar>(get_network_function("bambu_network_get_user_avatar"));
    get_user_nickanme_ptr             =  reinterpret_cast<func_get_user_nickanme>(get_network_function("bambu_network_get_user_nickanme"));
    build_login_cmd_ptr               =  reinterpret_cast<func_build_login_cmd>(get_network_function("bambu_network_build_login_cmd"));
    build_logout_cmd_ptr              =  reinterpret_cast<func_build_logout_cmd>(get_network_function("bambu_network_build_logout_cmd"));
    build_login_info_ptr              =  reinterpret_cast<func_build_login_info>(get_network_function("bambu_network_build_login_info"));
    bind_ptr                          =  reinterpret_cast<func_bind>(get_network_function("bambu_network_bind"));
    unbind_ptr                        =  reinterpret_cast<func_unbind>(get_network_function("bambu_network_unbind"));
    get_bambulab_host_ptr             =  reinterpret_cast<func_get_bambulab_host>(get_network_function("bambu_network_get_bambulab_host"));
    get_user_selected_machine_ptr     =  reinterpret_cast<func_get_user_selected_machine>(get_network_function("bambu_network_get_user_selected_machine"));
    set_user_selected_machine_ptr     =  reinterpret_cast<func_set_user_selected_machine>(get_network_function("bambu_network_set_user_selected_machine"));
    start_print_ptr                   =  reinterpret_cast<func_start_print>(get_network_function("bambu_network_start_print"));
    start_local_print_with_record_ptr =  reinterpret_cast<func_start_local_print_with_record>(get_network_function("bambu_network_start_local_print_with_record"));
    start_send_gcode_to_sdcard_ptr    =  reinterpret_cast<func_start_send_gcode_to_sdcard>(get_network_function("bambu_network_start_send_gcode_to_sdcard"));
    start_local_print_ptr             =  reinterpret_cast<func_start_local_print>(get_network_function("bambu_network_start_local_print"));
    get_user_presets_ptr              =  reinterpret_cast<func_get_user_presets>(get_network_function("bambu_network_get_user_presets"));
    request_setting_id_ptr            =  reinterpret_cast<func_request_setting_id>(get_network_function("bambu_network_request_setting_id"));
    put_setting_ptr                   =  reinterpret_cast<func_put_setting>(get_network_function("bambu_network_put_setting"));
    get_setting_list_ptr              =  reinterpret_cast<func_get_setting_list>(get_network_function("bambu_network_get_setting_list"));
    delete_setting_ptr                =  reinterpret_cast<func_delete_setting>(get_network_function("bambu_network_delete_setting"));
    get_studio_info_url_ptr           =  reinterpret_cast<func_get_studio_info_url>(get_network_function("bambu_network_get_studio_info_url"));
    set_extra_http_header_ptr         =  reinterpret_cast<func_set_extra_http_header>(get_network_function("bambu_network_set_extra_http_header"));
    get_my_message_ptr                =  reinterpret_cast<func_get_my_message>(get_network_function("bambu_network_get_my_message"));
    check_user_task_report_ptr        =  reinterpret_cast<func_check_user_task_report>(get_network_function("bambu_network_check_user_task_report"));
    get_user_print_info_ptr           =  reinterpret_cast<func_get_user_print_info>(get_network_function("bambu_network_get_user_print_info"));
    get_printer_firmware_ptr          =  reinterpret_cast<func_get_printer_firmware>(get_network_function("bambu_network_get_printer_firmware"));
    get_task_plate_index_ptr          =  reinterpret_cast<func_get_task_plate_index>(get_network_function("bambu_network_get_task_plate_index"));
    get_slice_info_ptr                =  reinterpret_cast<func_get_slice_info>(get_network_function("bambu_network_get_slice_info"));
    query_bind_status_ptr             =  reinterpret_cast<func_query_bind_status>(get_network_function("bambu_network_query_bind_status"));
    modify_printer_name_ptr           =  reinterpret_cast<func_modify_printer_name>(get_network_function("bambu_network_modify_printer_name"));
    get_camera_url_ptr                =  reinterpret_cast<func_get_camera_url>(get_network_function("bambu_network_get_camera_url"));
    start_publish_ptr                 =  reinterpret_cast<func_start_pubilsh>(get_network_function("bambu_network_start_publish"));

    return 0;
}

__declspec(dllexport) int unload_network_module()
{
    BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(", network module %1%")%netwoking_module;
#if defined(_MSC_VER) || defined(_WIN32)
    if (netwoking_module) {
        FreeLibrary(netwoking_module);
        netwoking_module = NULL;
    }
    if (source_module) {
        FreeLibrary(source_module);
        source_module = NULL;
    }
#else
    if (netwoking_module) {
        dlclose(netwoking_module);
        netwoking_module = NULL;
    }
    if (source_module) {
        dlclose(source_module);
        source_module = NULL;
    }
#endif

    check_debug_consistent_ptr        =  nullptr;
    get_version_ptr                   =  nullptr;
    create_agent_ptr                  =  nullptr;
    destroy_agent_ptr                 =  nullptr;
    init_log_ptr                      =  nullptr;
    set_config_dir_ptr                =  nullptr;
    set_cert_file_ptr                 =  nullptr;
    set_country_code_ptr              =  nullptr;
    start_ptr                         =  nullptr;
    set_on_ssdp_msg_fn_ptr            =  nullptr;
    set_on_user_login_fn_ptr          =  nullptr;
    set_on_printer_connected_fn_ptr   =  nullptr;
    set_on_server_connected_fn_ptr    =  nullptr;
    set_on_http_error_fn_ptr          =  nullptr;
    set_get_country_code_fn_ptr       =  nullptr;
    set_on_message_fn_ptr             =  nullptr;
    set_on_local_connect_fn_ptr       =  nullptr;
    set_on_local_message_fn_ptr       =  nullptr;
    connect_server_ptr                =  nullptr;
    is_server_connected_ptr           =  nullptr;
    refresh_connection_ptr            =  nullptr;
    start_subscribe_ptr               =  nullptr;
    stop_subscribe_ptr                =  nullptr;
    send_message_ptr                  =  nullptr;
    connect_printer_ptr               =  nullptr;
    disconnect_printer_ptr            =  nullptr;
    send_message_to_printer_ptr       =  nullptr;
    start_discovery_ptr               =  nullptr;
    change_user_ptr                   =  nullptr;
    is_user_login_ptr                 =  nullptr;
    user_logout_ptr                   =  nullptr;
    get_user_id_ptr                   =  nullptr;
    get_user_name_ptr                 =  nullptr;
    get_user_avatar_ptr               =  nullptr;
    get_user_nickanme_ptr             =  nullptr;
    build_login_cmd_ptr               =  nullptr;
    build_logout_cmd_ptr              =  nullptr;
    build_login_info_ptr              =  nullptr;
    bind_ptr                          =  nullptr;
    unbind_ptr                        =  nullptr;
    get_bambulab_host_ptr             =  nullptr;
    get_user_selected_machine_ptr     =  nullptr;
    set_user_selected_machine_ptr     =  nullptr;
    start_print_ptr                   =  nullptr;
    start_local_print_with_record_ptr =  nullptr;
    start_send_gcode_to_sdcard_ptr    =  nullptr;
    start_local_print_ptr             =  nullptr;
    get_user_presets_ptr              =  nullptr;
    request_setting_id_ptr            =  nullptr;
    put_setting_ptr                   =  nullptr;
    get_setting_list_ptr              =  nullptr;
    delete_setting_ptr                =  nullptr;
    get_studio_info_url_ptr           =  nullptr;
    set_extra_http_header_ptr         =  nullptr;
    get_my_message_ptr                =  nullptr;
    check_user_task_report_ptr        =  nullptr;
    get_user_print_info_ptr           =  nullptr;
    get_printer_firmware_ptr          =  nullptr;
    get_task_plate_index_ptr          =  nullptr;
    get_slice_info_ptr                =  nullptr;
    query_bind_status_ptr             =  nullptr;
    modify_printer_name_ptr           =  nullptr;
    get_camera_url_ptr                =  nullptr;
    start_publish_ptr                 =  nullptr;

    return 0;
}

#if defined(_MSC_VER) || defined(_WIN32)
HMODULE get_bambu_source_entry()
#else
void* get_bambu_source_entry()
#endif
{
    if ((source_module) || (!netwoking_module))
        return source_module;

    //int ret = -1;
    std::string library;
    //std::string data_dir_str = data_dir();
    boost::filesystem::path data_dir_path(dll_dir());
    //auto plugin_folder = data_dir_path / "plugins";
#if defined(_MSC_VER) || defined(_WIN32)
    wchar_t lib_wstr[128];

    //goto load bambu source
    library = dll_dir() + "/BambuSource.dll";
    memset(lib_wstr, 0, sizeof(lib_wstr));
    ::MultiByteToWideChar(CP_UTF8, NULL, library.c_str(), strlen(library.c_str())+1, lib_wstr, sizeof(lib_wstr) / sizeof(lib_wstr[0]));
    source_module = LoadLibrary(lib_wstr);
    /*if (!source_module) {
        library = std::string(BAMBU_SOURCE_LIBRARY) + ".dll";
        memset(lib_wstr, 0, sizeof(lib_wstr));
        ::MultiByteToWideChar(CP_UTF8, NULL, library.c_str(), strlen(library.c_str()) + 1, lib_wstr, sizeof(lib_wstr) / sizeof(lib_wstr[0]));
        source_module = LoadLibrary(lib_wstr);
    }*/
#else
#if defined(__WXMAC__)
    library = plugin_folder.string() + "/" + std::string("lib") + std::string(BAMBU_SOURCE_LIBRARY) + ".dylib";
#else
    library = plugin_folder.string() + "/" + std::string("lib") + std::string(BAMBU_SOURCE_LIBRARY) + ".so";
#endif
    source_module = dlopen( library.c_str(), RTLD_LAZY);
    /*if (!source_module) {
#if defined(__WXMAC__)
        library = std::string("lib") + BAMBU_SOURCE_LIBRARY + ".dylib";
#else
        library = std::string("lib") + BAMBU_SOURCE_LIBRARY + ".so";
#endif
        source_module = dlopen( library.c_str(), RTLD_LAZY);
    }*/
#endif

    return source_module;
}

void* get_network_function(const char* name)
{
    void* function = nullptr;

    if (!netwoking_module)
        return function;

#if defined(_MSC_VER) || defined(_WIN32)
    function = GetProcAddress(netwoking_module, name);
#else
    function = dlsym(netwoking_module, name);
#endif

    if (!function) {
        BOOST_LOG_TRIVIAL(warning) << __FUNCTION__ << boost::format(", can not find function %1%")%name;
    }
    else
    {
        BOOST_LOG_TRIVIAL(warning) << __FUNCTION__ << boost::format(", found %1% at %2%") % name %function;
    }
    return function;
}

__declspec(dllexport) BSTR get_version()
{
    bool consistent = true;
    std::string ret = "";

    //check the debug consistent first
    if (check_debug_consistent_ptr) {
#if defined(NDEBUG)
        consistent = check_debug_consistent_ptr(false);
#else
        consistent = check_debug_consistent_ptr(true);
#endif
    }
    if (!consistent) {
        BOOST_LOG_TRIVIAL(warning) << __FUNCTION__ << boost::format(", inconsistent library,return 00.00.00.00!");
        ret = "00.00.00.00";
    }
    else if (get_version_ptr) {
        ret = get_version_ptr();
    }
    else
    {
        BOOST_LOG_TRIVIAL(warning) << __FUNCTION__ << boost::format(", get_version not supported,return 00.00.00.00!");
        ret = "00.00.00.00";
    }
    
    _bstr_t bstr = ret.c_str();
    return bstr.Detach();
}

__declspec(dllexport) int init_log()
{
    int ret = 0;
    if (network_agent && init_log_ptr) {
        ret = init_log_ptr(network_agent);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int set_config_dir(char* config_dir)
{
    int ret = 0;
    if (network_agent && set_config_dir_ptr) {
        ret = set_config_dir_ptr(network_agent, config_dir);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, config_dir=%3%")%network_agent %ret %config_dir ;
    }
    return ret;
}

__declspec(dllexport) int set_cert_file(char* folder, char* filename)
{
    int ret = 0;
    if (network_agent && set_cert_file_ptr) {
        ret = set_cert_file_ptr(network_agent, folder, filename);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, folder=%3%, filename=%4%")%network_agent %ret %folder %filename;
    }
    return ret;
}

__declspec(dllexport) int set_country_code(char* country_code)
{
    int ret = 0;
    if (network_agent && set_country_code_ptr) {
        ret = set_country_code_ptr(network_agent, country_code);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, country_code=%3%")%network_agent %ret %country_code ;
    }
    return ret;
}

__declspec(dllexport) int start()
{
    int ret = 0;
    if (network_agent && start_ptr) {
        ret = start_ptr(network_agent);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

OnMsgArrivedFnCS OnMsgArrivedFnCS_ptr = nullptr;

void OnMsgArrivedFnWrapper(std::string dev_info_json_str)
{
    // sddp message (sent as an SSDP UDP packet, comes out of the DLL as json)
    BSTR dev_info_json_str_bstr = ::_com_util::ConvertStringToBSTR(dev_info_json_str.c_str());

    if (OnMsgArrivedFnCS_ptr != nullptr)
        OnMsgArrivedFnCS_ptr(dev_info_json_str_bstr);
}

__declspec(dllexport) int set_on_ssdp_msg_fn(OnMsgArrivedFnCS fn)
{
    OnMsgArrivedFnCS_ptr = fn;

    int ret = 0;
    if (network_agent && set_on_ssdp_msg_fn_ptr) {
        ret = set_on_ssdp_msg_fn_ptr(network_agent, OnMsgArrivedFnWrapper);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int set_on_user_login_fn(OnUserLoginFn fn)
{
    //BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" info: set_on_user_login_fn=%1%") % (& fn);
    int ret = 0;
    if (network_agent && set_on_user_login_fn_ptr) {
        ret = set_on_user_login_fn_ptr(network_agent, fn);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}


OnPrinterConnectedFnCS OnPrinterConnectedFnCS_ptr = nullptr;

void OnPrinterConnectedFnWrapper(std::string topic_str)
{
    BSTR topic_str_bstr = ::_com_util::ConvertStringToBSTR(topic_str.c_str());

    if (OnPrinterConnectedFnCS_ptr != nullptr)
        OnPrinterConnectedFnCS_ptr(topic_str_bstr);
}


__declspec(dllexport) int set_on_printer_connected_fn(OnPrinterConnectedFnCS fn)
{
    OnPrinterConnectedFnCS_ptr = fn;

    int ret = 0;
    if (network_agent && set_on_printer_connected_fn_ptr) {
        ret = set_on_printer_connected_fn_ptr(network_agent, OnPrinterConnectedFnWrapper);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

OnServerConnectedFnCS OnServerConnectedFnCS_ptr = nullptr;

void OnServerConnectedFnWrapper()
{
    if (OnServerConnectedFnCS_ptr != nullptr)
        OnServerConnectedFnCS_ptr();
}

__declspec(dllexport) int set_on_server_connected_fn(OnServerConnectedFnCS fn)
{
    OnServerConnectedFnCS_ptr = fn;
    int ret = 0;
    if (network_agent && set_on_server_connected_fn_ptr) {
        ret = set_on_server_connected_fn_ptr(network_agent, OnServerConnectedFnWrapper);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int set_on_http_error_fn(OnHttpErrorFn fn)
{
    int ret = 0;
    if (network_agent && set_on_http_error_fn_ptr) {
        ret = set_on_http_error_fn_ptr(network_agent, fn);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int set_get_country_code_fn(GetCountryCodeFn fn)
{
    int ret = 0;
    if (network_agent && set_get_country_code_fn_ptr) {
        ret = set_get_country_code_fn_ptr(network_agent, fn);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

OnMessageFnCS OnMessageFnCS_ptr = nullptr;

void OnMessageFnWrapper(std::string dev_id, std::string msg)
{
    BSTR dev_id_bstr = ::_com_util::ConvertStringToBSTR(dev_id.c_str());
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnMessageFnCS_ptr != nullptr)
        OnMessageFnCS_ptr(dev_id_bstr, msg_bstr);
}

__declspec(dllexport) int set_on_message_fn(OnMessageFnCS fn)
{
    OnMessageFnCS_ptr = fn;

    int ret = 0;
    if (network_agent && set_on_message_fn_ptr) {
        ret = set_on_message_fn_ptr(network_agent, OnMessageFnWrapper);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}


OnLocalConnectedFnCS OnLocalConnectedFnCS_ptr = nullptr;

void OnLocalConnectedFnWrapper(int status, std::string dev_id, std::string msg)
{
    BSTR dev_id_bstr = ::_com_util::ConvertStringToBSTR(dev_id.c_str());
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnLocalConnectedFnCS_ptr != nullptr)
        OnLocalConnectedFnCS_ptr(status, dev_id_bstr, msg_bstr);
}

__declspec(dllexport) int set_on_local_connect_fn(OnLocalConnectedFnCS fn)
{
    OnLocalConnectedFnCS_ptr = fn;

    int ret = 0;
    if (network_agent && set_on_local_connect_fn_ptr) {
        ret = set_on_local_connect_fn_ptr(network_agent, OnLocalConnectedFnWrapper);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

OnMessageFnCS OnLocalMessageFnCS_ptr = nullptr;

void OnLocalMessageFnWrapper(std::string dev_id, std::string msg)
{
    BSTR dev_id_bstr = ::_com_util::ConvertStringToBSTR(dev_id.c_str());
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnLocalMessageFnCS_ptr != nullptr)
        OnLocalMessageFnCS_ptr(dev_id_bstr, msg_bstr);
}


__declspec(dllexport) int set_on_local_message_fn(OnMessageFnCS fn)
{
    OnLocalMessageFnCS_ptr = fn;

    int ret = 0;
    if (network_agent && set_on_local_message_fn_ptr) {
        ret = set_on_local_message_fn_ptr(network_agent, OnLocalMessageFnWrapper);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int connect_server()
{
    int ret = 0;
    if (network_agent && connect_server_ptr) {
        ret = connect_server_ptr(network_agent);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) bool is_server_connected()
{
    bool ret = false;
    if (network_agent && is_server_connected_ptr) {
        ret = is_server_connected_ptr(network_agent);
        //BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int refresh_connection()
{
    int ret = 0;
    if (network_agent && refresh_connection_ptr) {
        ret = refresh_connection_ptr(network_agent);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int start_subscribe(char* module)
{
    int ret = 0;
    if (network_agent && start_subscribe_ptr) {
        ret = start_subscribe_ptr(network_agent, module);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, module=%3%")%network_agent %ret %module ;
    }
    return ret;
}

__declspec(dllexport) int stop_subscribe(char* module)
{
    int ret = 0;
    if (network_agent && stop_subscribe_ptr) {
        ret = stop_subscribe_ptr(network_agent, module);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, module=%3%")%network_agent %ret %module ;
    }
    return ret;
}

__declspec(dllexport) int send_message(char* dev_id, char* json_str, int qos)
{
    int ret = 0;
    if (network_agent && send_message_ptr) {
        ret = send_message_ptr(network_agent, dev_id, json_str, qos);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, dev_id=%3%, json_str=%4%, qos=%5%")%network_agent %ret %dev_id %json_str %qos;
    }
    return ret;
}

__declspec(dllexport) int connect_printer(char* dev_id, char* dev_ip, char* username, char* password)
{
    int ret = 0;
    if (network_agent && connect_printer_ptr) {
        ret = connect_printer_ptr(network_agent, dev_id, dev_ip, username, password);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, dev_id=%3%, dev_ip=%4%, username=%5%, password=%6%")
                %network_agent %ret %dev_id %dev_ip %username %password;
    }
    return ret;
}

__declspec(dllexport) int disconnect_printer()
{
    int ret = 0;
    if (network_agent && disconnect_printer_ptr) {
        ret = disconnect_printer_ptr(network_agent);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) int send_message_to_printer(char* dev_id, char* json_str, int qos)
{
    int ret = 0;
    if (network_agent && send_message_to_printer_ptr) {
        ret = send_message_to_printer_ptr(network_agent, dev_id, json_str, qos);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, dev_id=%3%, json_str=%4%, qos=%5%")
                %network_agent %ret %dev_id %json_str %qos;
    }
    return ret;
}

__declspec(dllexport) bool start_discovery(bool start, bool sending)
{
    bool ret = false;
    if (network_agent && start_discovery_ptr) {
        ret = start_discovery_ptr(network_agent, start, sending);
        //BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, start=%3%, sending=%4%")%network_agent %ret %start %sending;
    }
    return ret;
}

__declspec(dllexport) int change_user(char* user_info)
{
    int ret = 0;
    if (network_agent && change_user_ptr) {
        ret = change_user_ptr(network_agent, user_info);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, user_info=%3%")%network_agent %ret %user_info ;
    }
    return ret;
}

__declspec(dllexport) bool is_user_login()
{
    bool ret = false;
    if (network_agent && is_user_login_ptr) {
        ret = is_user_login_ptr(network_agent);
    }
    return ret;
}

__declspec(dllexport) int  user_logout()
{
    int ret = 0;
    if (network_agent && user_logout_ptr) {
        ret = user_logout_ptr(network_agent);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%")%network_agent %ret;
    }
    return ret;
}

__declspec(dllexport) BSTR get_user_id()
{
    std::string ret;
    if (network_agent && get_user_id_ptr) {
        ret = get_user_id_ptr(network_agent);
    }

    _bstr_t bstr = ret.c_str();
    return ::SysAllocString (bstr);
}

__declspec(dllexport) BSTR get_user_name()
{
    std::string ret;
    if (network_agent && get_user_name_ptr) {
        ret = get_user_name_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) BSTR get_user_avatar()
{
    std::string ret;
    if (network_agent && get_user_avatar_ptr) {
        ret = get_user_avatar_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) BSTR get_user_nickanme()
{
    std::string ret;
    if (network_agent && get_user_nickanme_ptr) {
        ret = get_user_nickanme_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) BSTR build_login_cmd()
{
    std::string ret = "";
    if (network_agent && build_login_cmd_ptr) {
        ret = build_login_cmd_ptr(network_agent);
    }

    _bstr_t bstr = ret.c_str();
    return bstr.Detach();
}

__declspec(dllexport) BSTR build_logout_cmd()
{
    std::string ret;
    if (network_agent && build_logout_cmd_ptr) {
        ret = build_logout_cmd_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) BSTR build_login_info()
{
    std::string ret;
    if (network_agent && build_login_info_ptr) {
        ret = build_login_info_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}


OnUpdateStatusFnCS OnUpdateStatusFnCS_ptr_bind = nullptr;

void OnMsgArrivedFnWrapper_bind(int status, int code, std::string msg)
{
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnUpdateStatusFnCS_ptr_bind != nullptr)
        OnUpdateStatusFnCS_ptr_bind(status, code, msg_bstr);
}

__declspec(dllexport) int bind(char* dev_ip, char* timezone, OnUpdateStatusFnCS update_fn)
{
    OnUpdateStatusFnCS_ptr_bind = update_fn;

    int ret = 0;
    if (network_agent && bind_ptr) {
        ret = bind_ptr(network_agent, dev_ip, timezone, OnMsgArrivedFnWrapper_bind);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, dev_ip=%3%, timezone=%4%")
                %network_agent %ret %dev_ip %timezone;
    }
    return ret;
}

__declspec(dllexport) int unbind(char* dev_id)
{
    int ret = 0;
    if (network_agent && unbind_ptr) {
        ret = unbind_ptr(network_agent, dev_id);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, user_info=%3%")%network_agent %ret %dev_id ;
    }
    return ret;
}

__declspec(dllexport) BSTR get_bambulab_host()
{
    std::string ret;
    if (network_agent && get_bambulab_host_ptr) {
        ret = get_bambulab_host_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) BSTR get_user_selected_machine()
{
    std::string ret;
    if (network_agent && get_user_selected_machine_ptr) {
        ret = get_user_selected_machine_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) int set_user_selected_machine(char* dev_id)
{
    int ret = 0;
    if (network_agent && set_user_selected_machine_ptr) {
        ret = set_user_selected_machine_ptr(network_agent, dev_id);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, user_info=%3%")%network_agent %ret %dev_id ;
    }
    return ret;
}

OnUpdateStatusFnCS OnUpdateStatusFnCS_ptr_start_print = nullptr;

void OnMsgArrivedFnWrapper_start_print(int status, int code, std::string msg)
{
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnUpdateStatusFnCS_ptr_start_print != nullptr)
        OnUpdateStatusFnCS_ptr_start_print(status, code, msg_bstr);
}

__declspec(dllexport) int start_print(PrintParams params, OnUpdateStatusFnCS update_fn, WasCancelledFnCS cancel_fn)
{
    OnUpdateStatusFnCS_ptr_start_print = update_fn;

    int ret = 0;
    if (network_agent && start_print_ptr) {
        ret = start_print_ptr(network_agent, params, OnMsgArrivedFnWrapper_start_print, cancel_fn);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, ret=%2%, dev_id=%3%, task_name=%4%, project_name=%5%")
                %network_agent %ret %params.dev_id %params.task_name %params.project_name;
    }
    return ret;
}

OnUpdateStatusFnCS OnUpdateStatusFnCS_ptr_start_print_with_record = nullptr;

void OnMsgArrivedFnWrapper_start_print_with_record(int status, int code, std::string msg)
{
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnUpdateStatusFnCS_ptr_start_print_with_record != nullptr)
        OnUpdateStatusFnCS_ptr_start_print_with_record(status, code, msg_bstr);
}

__declspec(dllexport) int start_local_print_with_record(PrintParams params, OnUpdateStatusFnCS update_fn, WasCancelledFnCS cancel_fn)
{
    OnUpdateStatusFnCS_ptr_start_print_with_record = update_fn;

    int ret = 0;
    if (network_agent && start_local_print_with_record_ptr) {
        ret = start_local_print_with_record_ptr(network_agent, params, OnMsgArrivedFnWrapper_start_print_with_record, cancel_fn);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, ret=%2%, dev_id=%3%, task_name=%4%, project_name=%5%")
                %network_agent %ret %params.dev_id %params.task_name %params.project_name;
    }
    return ret;
}

OnUpdateStatusFnCS OnUpdateStatusFnCS_ptr_send_gcode_to_sdcard = nullptr;

void OnMsgArrivedFnWrapper_send_gcode_to_sdcard(int status, int code, std::string msg)
{
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnUpdateStatusFnCS_ptr_send_gcode_to_sdcard != nullptr)
        OnUpdateStatusFnCS_ptr_send_gcode_to_sdcard(status, code, msg_bstr);
}

WasCancelledFnCS WasCancelledFnCS_ptr_start_send_gcode_to_sdcard = nullptr;

bool WasCancelledFnWrapper_start_send_gcode_to_sdcard()
{
    if (WasCancelledFnCS_ptr_start_send_gcode_to_sdcard != nullptr)
        return WasCancelledFnCS_ptr_start_send_gcode_to_sdcard();
    else
        return false;
}

__declspec(dllexport) int start_send_gcode_to_sdcard(char* params_json, OnUpdateStatusFnCS update_fn, WasCancelledFnCS cancel_fn)
{
    OnUpdateStatusFnCS_ptr_send_gcode_to_sdcard = update_fn;
    WasCancelledFnCS_ptr_start_send_gcode_to_sdcard = cancel_fn;
    
    std::string str_json = params_json;
    json j = json::parse(str_json);
    PrintParams params = j.get<PrintParams>();
    
    int ret = 0;
	if (network_agent && start_send_gcode_to_sdcard_ptr) {
		ret = start_send_gcode_to_sdcard_ptr(network_agent, params, OnMsgArrivedFnWrapper_send_gcode_to_sdcard, WasCancelledFnWrapper_start_send_gcode_to_sdcard);
        //BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : json: OK6");
		BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, ret=%2%, dev_id=%3%, task_name=%4%, project_name=%5%")
			% network_agent % ret % params.dev_id % params.task_name % params.project_name;
	}
	return ret;
}

OnUpdateStatusFnCS OnUpdateStatusFnCS_ptr_start_local_print = nullptr;

void OnMsgArrivedFnWrapper_start_local_print(int status, int code, std::string msg)
{
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnUpdateStatusFnCS_ptr_start_local_print != nullptr)
        OnUpdateStatusFnCS_ptr_start_local_print(status, code, msg_bstr);
}

WasCancelledFnCS WasCancelledFnCS_ptr_start_local_print = nullptr;

bool WasCancelledFnWrapper_start_local_print()
{
    if (WasCancelledFnCS_ptr_start_local_print != nullptr)
        return WasCancelledFnCS_ptr_start_local_print();
    else
        return false;
}

__declspec(dllexport) int start_local_print(char* params_json, OnUpdateStatusFnCS update_fn, WasCancelledFnCS cancel_fn)
{
    OnUpdateStatusFnCS_ptr_start_local_print = update_fn;
    WasCancelledFnCS_ptr_start_local_print = cancel_fn;

    std::string str_json = params_json;
    json j = json::parse(str_json);
    PrintParams params = j.get<PrintParams>();

    int ret = 0;
    if (network_agent && start_local_print_ptr) {
        ret = start_local_print_ptr(network_agent, params, OnMsgArrivedFnWrapper_start_local_print, WasCancelledFnWrapper_start_local_print);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, ret=%2%, dev_id=%3%, task_name=%4%, project_name=%5%")
                %network_agent %ret %params.dev_id %params.task_name %params.project_name;
    }
    return ret;
}

__declspec(dllexport) int get_user_presets(std::map<std::string, std::map<std::string, std::string>>* user_presets)
{
    int ret = 0;
    if (network_agent && get_user_presets_ptr) {
        ret = get_user_presets_ptr(network_agent, user_presets);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, ret=%2%, setting_id count=%3%")%network_agent %ret %user_presets->size() ;
    }
    return ret;
}

__declspec(dllexport) BSTR request_setting_id(std::string name, std::map<std::string, std::string>* values_map, unsigned int* http_code)
{
    std::string ret;
    if (network_agent && request_setting_id_ptr) {
        ret = request_setting_id_ptr(network_agent, name, values_map, http_code);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, name=%2%, http_code=%3%, ret.setting_id=%4%")
                %network_agent %name %(*http_code) %ret;
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) int put_setting(std::string setting_id, std::string name, std::map<std::string, std::string>* values_map, unsigned int* http_code)
{
    int ret = 0;
    if (network_agent && put_setting_ptr) {
        ret = put_setting_ptr(network_agent, setting_id, name, values_map, http_code);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, setting_id=%2%, name=%3%, http_code=%4%, ret=%5%")
                %network_agent %setting_id %name %(*http_code) %ret;
    }
    return ret;
}

__declspec(dllexport) int get_setting_list(std::string bundle_version, ProgressFn pro_fn, WasCancelledFnCS cancel_fn)
{
    int ret = 0;
    if (network_agent && get_setting_list_ptr) {
        ret = get_setting_list_ptr(network_agent, bundle_version, pro_fn, cancel_fn);
        if (ret)
            BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, bundle_version=%3%")%network_agent %ret %bundle_version ;
    }
    return ret;
}

__declspec(dllexport) int delete_setting(char* setting_id)
{
    int ret = 0;
    if (network_agent && delete_setting_ptr) {
        ret = delete_setting_ptr(network_agent, setting_id);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, setting_id=%3%")%network_agent %ret %setting_id ;
    }
    return ret;
}

__declspec(dllexport) BSTR get_studio_info_url()
{
    std::string ret;
    if (network_agent && get_studio_info_url_ptr) {
        ret = get_studio_info_url_ptr(network_agent);
    }
    return ::_com_util::ConvertStringToBSTR(ret.c_str());
}

__declspec(dllexport) int set_extra_http_header(std::map<std::string, std::string> extra_headers)
{
    int ret = 0;
    if (network_agent && set_extra_http_header_ptr) {
        ret = set_extra_http_header_ptr(network_agent, extra_headers);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, extra_headers count=%3%")%network_agent %ret %extra_headers.size() ;
    }
    return ret;
}

__declspec(dllexport) int get_my_message(int type, int after, int limit, unsigned int* http_code, std::string* http_body)
{
    int ret = 0;
    if (network_agent && get_my_message_ptr) {
        ret = get_my_message_ptr(network_agent, type, after, limit, http_code, http_body);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%") % network_agent % ret;
    }
    return ret;
}

__declspec(dllexport) int check_user_task_report(int* task_id, bool* printable)
{
    int ret = 0;
    if (network_agent && check_user_task_report_ptr) {
        ret = check_user_task_report_ptr(network_agent, task_id, printable);
        BOOST_LOG_TRIVIAL(debug) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, task_id=%3%, printable=%4%")%network_agent %ret %(*task_id) %(*printable);
    }
    return ret;
}

__declspec(dllexport) int get_user_print_info(unsigned int* http_code, std::string* http_body)
{
    int ret = 0;
    if (network_agent && get_user_print_info_ptr) {
        ret = get_user_print_info_ptr(network_agent, http_code, http_body);
        BOOST_LOG_TRIVIAL(debug) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, http_code=%3%, http_body=%4%")%network_agent %ret %(*http_code) %(*http_body);
    }
    return ret;
}

__declspec(dllexport) int get_printer_firmware(std::string dev_id, unsigned* http_code, std::string* http_body)
{
    int ret = 0;
    if (network_agent && get_printer_firmware_ptr) {
        ret = get_printer_firmware_ptr(network_agent, dev_id, http_code, http_body);
        BOOST_LOG_TRIVIAL(debug) << __FUNCTION__ << boost::format(" : network_agent=%1%, ret=%2%, dev_id=%3%, http_code=%4%, http_body=%5%")
                %network_agent %ret %dev_id %(*http_code) %(*http_body);
    }
    return ret;
}

__declspec(dllexport) int get_task_plate_index(std::string task_id, int* plate_index)
{
    int ret = 0;
    if (network_agent && get_task_plate_index_ptr) {
        ret = get_task_plate_index_ptr(network_agent, task_id, plate_index);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, task_id=%3%")%network_agent %ret %task_id;
    }
    return ret;
}

__declspec(dllexport) int get_slice_info(std::string project_id, std::string profile_id, int plate_index, std::string* slice_json)
{
    int ret = 0;
    if (network_agent && get_slice_info_ptr) {
        ret = get_slice_info_ptr(network_agent, project_id, profile_id, plate_index, slice_json);
        BOOST_LOG_TRIVIAL(debug) << __FUNCTION__ << boost::format(" : network_agent=%1%, project_id=%2%, profile_id=%3%, plate_index=%4%, slice_json=%5%")
                %network_agent %project_id %profile_id %plate_index %(*slice_json);
    }
    return ret;
}

__declspec(dllexport) int query_bind_status(std::vector<std::string> query_list, unsigned int* http_code, std::string* http_body)
{
    int ret = 0;
    if (network_agent && query_bind_status_ptr) {
        ret = query_bind_status_ptr(network_agent, query_list, http_code, http_body);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, http_code=%3%, http_body=%4%")
                %network_agent %ret%(*http_code) %(*http_body);
    }
    return ret;
}

__declspec(dllexport) int modify_printer_name(char* dev_id, char* dev_name)
{
    int ret = 0;
    if (network_agent && modify_printer_name_ptr) {
        ret = modify_printer_name_ptr(network_agent, dev_id, dev_name);
        BOOST_LOG_TRIVIAL(info) << __FUNCTION__ << boost::format(" : network_agent=%1%, ret=%2%, dev_id=%3%, dev_name=%4%")%network_agent %ret %dev_id %dev_name;
    }
    return ret;
}

OnGetCameraUrlCS OnGetCameraUrlCS_ptr = nullptr;

void GetCameraUrlCallbackWrapper(std::string url)
{
    BSTR url_bstr = ::_com_util::ConvertStringToBSTR(url.c_str());
 
    if (OnGetCameraUrlCS_ptr != nullptr)
        OnGetCameraUrlCS_ptr(url_bstr);
}

__declspec(dllexport) void set_get_camera_url_callback(OnGetCameraUrlCS callback)
{
    OnGetCameraUrlCS_ptr = callback;
}

__declspec(dllexport) int get_camera_url(char* dev_id)
{
    int ret = 0;
    if (network_agent && get_camera_url_ptr) {
        ret = get_camera_url_ptr(network_agent, dev_id, GetCameraUrlCallbackWrapper);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%, dev_id=%3%")%network_agent %ret %dev_id;
    }
    return ret;
}


OnUpdateStatusFnCS OnUpdateStatusFnCS_ptr_start_publish = nullptr;

void OnMsgArrivedFnWrapper_start_publish(int status, int code, std::string msg)
{
    BSTR msg_bstr = ::_com_util::ConvertStringToBSTR(msg.c_str());

    if (OnUpdateStatusFnCS_ptr_start_publish != nullptr)
        OnUpdateStatusFnCS_ptr_start_publish(status, code, msg_bstr);
}

__declspec(dllexport) int start_publish(PublishParams params, OnUpdateStatusFnCS update_fn, WasCancelledFnCS cancel_fn, std::string *out)
{
    OnUpdateStatusFnCS_ptr_start_publish = update_fn;

    int ret = 0;
    if (network_agent && start_publish_ptr) {
        ret = start_publish_ptr(network_agent, params, OnMsgArrivedFnWrapper_start_publish, cancel_fn, out);
        if (ret)
            BOOST_LOG_TRIVIAL(error) << __FUNCTION__ << boost::format(" error: network_agent=%1%, ret=%2%") % network_agent % ret;
    }
    return ret;
}


} //namespace
