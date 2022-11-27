using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpBambuTestApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class BambuNetworkPlugin : IDisposable
    {
        private OnServerConnectedDelegate InstanceOnServerConnectedDelegate;
        private OnPrinterMessageDelegate InstanceOnCloudMessageDelegate;
        private OnPrinterMessageDelegate InstanceOnLocalMessageDelegate;

        public Dictionary<string, BambuPrinter> Printers { get; } = new Dictionary<string, BambuPrinter>();

        public BambuNetworkPlugin()
        {
            InstanceOnServerConnectedDelegate = OnServerConnectedEvent;
            InstanceOnCloudMessageDelegate = OnCloudMessageEvent;
            InstanceOnLocalMessageDelegate = OnLocalMessageEvent;
        }

        /// <summary>
        /// Example: C:\Users\yourname\AppData\Roaming\BambuStudio
        /// </summary>
        public string BambuStudioDataFolder => Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BambuStudio");

        /// <summary>
        /// Example: C:\Users\yourname\AppData\Roaming\BambuStudio\plugins
        /// </summary>
        public string BambuStudioPluginFolder => Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BambuStudio\\plugins");

        /// <summary>
        /// Example: C:\Users\yourname\AppData\Roaming\BambuStudio
        /// </summary>
        public string BambuNetworkPluginConfigFolder { get; private set; } = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BambuStudio");

        /// <summary>
        /// Example: C:\Users\yourname\Desktop\SharpBambu
        /// </summary>
        public string ApplicationFolder => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Created by bambu_networking.dll in the local path. Example: C:\Users\yourname\Desktop\SharpBambu\log
        /// </summary>
        public string BambuNetworkPluginLogFolder => Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\log");

        /// <summary>
        /// Instance of the network agent returned by the bambu_networking.dll and used by several functions
        /// </summary>
        private UIntPtr Agent { get; set; }

        #region DLL Imports
        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int initialize_network_module();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern void set_dll_dir([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder dllDir);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern void set_data_dir([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder dataDir);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern UIntPtr bambu_network_create_agent();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_cert_file([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder folder, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder filename); 
        
        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_country_code([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder countryCode);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int send_message_to_printer([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder deviceId, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder jsonMessage, int qos);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int send_message([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder deviceId, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder jsonMessage, int qos);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern bool is_user_login();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern bool is_server_connected();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int start_subscribe([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder module);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int stop_subscribe([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder module);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int bambu_network_destroy_agent();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int init_log();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_config_dir([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder path);


        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string get_version();


        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int connect_server();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int connect_printer();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int disconnect_printer();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string get_user_selected_machine();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_user_selected_machine([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder deviceId);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int start();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string build_login_info();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string build_logout_cmd();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string build_login_cmd();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string get_user_id();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string get_user_name();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string get_user_nickanme();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string get_user_avatar();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int refresh_connection();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int user_logout();



        // events
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnSSDPMessageDelegate(string topic);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUserLoginDelegate(int onlineLogin, bool login);
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnPrinterConnectedDelegate(string topic);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnServerConnectedDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnHttpErrorDelegate(uint httpStatusCode, string httpBody);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnPrinterMessageDelegate([MarshalAs(UnmanagedType.BStr)] string deviceId, [MarshalAs(UnmanagedType.BStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnMessageArrivedDelegate(string deviceInfoJson);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate string OnGetCountryCodeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnLocalConnectedDelegate(int status, string deviceId, string message);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUserLogin(int onlineLogin, bool login);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUpdateStatusDelegate(int status, int code, string message);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool OnWasCanceledDelegate(); // todo

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool OnCancelDelegate(); // todo

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnProgressUpdatedDelegate(int progress); // todo

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnLoginDelegate(int retcode, string info); // todo

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnResultDelegate(int retcode, string info); // todo


        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_ssdp_msg_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnSSDPMessageDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_user_login_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnUserLoginDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_printer_connected_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnPrinterConnectedDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_server_connected_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnServerConnectedDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_http_error_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnHttpErrorDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_get_country_code_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnGetCountryCodeDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_message_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnPrinterMessageDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_local_connect_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnLocalConnectedDelegate callbackFunction);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int set_on_local_message_fn([MarshalAs(UnmanagedType.FunctionPtr)] OnPrinterMessageDelegate callbackFunction);

        // enums
        public enum SendingPrintJobStage
        {
            PrintingStageCreate = 0,
            PrintingStageUpload = 1,
            PrintingStageWaiting = 2,
            PrintingStageSending = 3,
            PrintingStageRecord = 4,
            PrintingStageFinished = 5,
        };

        public enum PublishingStage
        {
            PublishingCreate = 0,
            PublishingUpload = 1,
            PublishingWaiting = 2,
            PublishingJumpUrl = 3,
        };

        public enum BindJobStage
        {
            LoginStageConnect = 0,
            LoginStageLogin = 1,
            LoginStageWaitForLogin = 2,
            LoginStageGetIdentify = 3,
            LoginStageWaitAuth = 4,
            LoginStageFinished = 5,
        };

        public enum ConnectStatus
        {
            ConnectStatusOk = 0,
            ConnectStatusFailed = 1,
            ConnectStatusLost = 2,
        };

        // constants
        public const int BAMBU_NETWORK_SUCCESS = 0;
        public const int BAMBU_NETWORK_ERR_INVALID_HANDLE = -1;
        public const int BAMBU_NETWORK_ERR_CONNECT_FAILED = -2;
        public const int BAMBU_NETWORK_ERR_DISCONNECT_FAILED = -3;
        public const int BAMBU_NETWORK_ERR_SEND_MSG_FAILED = -4;
        public const int BAMBU_NETWORK_ERR_BIND_FAILED = -5;
        public const int BAMBU_NETWORK_ERR_UNBIND_FAILED = -6;
        public const int BAMBU_NETWORK_ERR_PRINT_FAILED = -7;
        public const int BAMBU_NETWORK_ERR_LOCAL_PRINT_FAILED = -8;
        public const int BAMBU_NETWORK_ERR_REQUEST_SETTING_FAILED = -9;
        public const int BAMBU_NETWORK_ERR_PUT_SETTING_FAILED = -10;
        public const int BAMBU_NETWORK_ERR_GET_SETTING_LIST_FAILED = -11;
        public const int BAMBU_NETWORK_ERR_DEL_SETTING_FAILED = -12;
        public const int BAMBU_NETWORK_ERR_GET_USER_PRINTINFO_FAILED = -13;
        public const int BAMBU_NETWORK_ERR_GET_PRINTER_FIRMWARE_FAILED = -14;
        public const int BAMBU_NETWORK_ERR_QUERY_BIND_INFO_FAILED = -15;
        public const int BAMBU_NETWORK_ERR_MODIFY_PRINTER_NAME_FAILED = -16;
        public const int BAMBU_NETWORK_ERR_FILE_NOT_EXIST = -17;
        public const int BAMBU_NETWORK_ERR_FILE_OVER_SIZE = -18;
        public const int BAMBU_NETWORK_ERR_CHECK_MD5_FAILED = -19;
        public const int BAMBU_NETWORK_ERR_TIMEOUT = -20;
        public const int BAMBU_NETWORK_ERR_CANCELED = -21;
        public const int BAMBU_NETWORK_ERR_INVALID_PARAMS = -22;
        public const int BAMBU_NETWORK_ERR_INVALID_RESULT = -23;
        public const int BAMBU_NETWORK_ERR_FTP_UPLOAD_FAILED = -24;
        public const int BAMBU_NETWORK_ERR_FTP_LOGIN_DENIED = -25;

        // structs

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

        #endregion

        /// <summary>
        /// Create an instance of the Bambu Network Plugin; requires a local copy of bambu_networking.dll
        /// </summary>
        /// <param name="autoUpdateDll">Copy the bambu_networking.dll from Bambu Studio folder when true</param>
        /// <exception cref="FileNotFoundException">Thrown when bambu_networking.dll is missing</exception>
        /// <exception cref="Exception">Thrown when NetworkPluginWrapper.dll fails to initialize</exception>
        public void InitializeNetworkPlugin(bool autoUpdateDll = true)
        {
            ResolveBambuNetworkingDll(autoUpdateDll);

            Debug.Print($"Setting data dir to {BambuStudioDataFolder}");
            set_data_dir(new StringBuilder(BambuStudioDataFolder));

            Debug.Print($"Setting dll dir to {BambuStudioPluginFolder}");
            set_dll_dir(new StringBuilder(BambuStudioPluginFolder));

            Debug.Print("Initialize network agent wrapper dll");
            var result = initialize_network_module();

            if (result != 0)
                throw new Exception("Bambu Network Plugin wrapper DLL failed to initialize");

            Debug.Print("Creating network agent");
            Agent = bambu_network_create_agent();

            if (Agent == UIntPtr.Zero)
                throw new Exception("Bambu Network Plugin failed to initialize");
        }

        public void InitCallbacks()
        {
            var result = 0;

            Debug.Print("Setting up callbacks");

            result = set_on_user_login_fn(new OnUserLoginDelegate(OnUserLoginEvent));
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_user_login_fn, result: {result}");

            result = set_on_local_connect_fn(new OnLocalConnectedDelegate(OnLocalConnectEvent));
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_local_connect_fn, result: {result}");

            result = set_on_http_error_fn(new OnHttpErrorDelegate(OnHttpErrorEvent));
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_http_error_fn, result: {result}");

            result = set_on_local_message_fn(InstanceOnLocalMessageDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_local_message_fn, result: {result}");

            result = set_on_message_fn(InstanceOnCloudMessageDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_message_fn, result: {result}");

            result = set_on_server_connected_fn(InstanceOnServerConnectedDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_server_connected_fn, result: {result}");

            result = set_get_country_code_fn(new OnGetCountryCodeDelegate(OnGetCountryCodeEvent));
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_get_country_code_fn, result: {result}");

            result = set_on_printer_connected_fn(new OnPrinterConnectedDelegate(OnPrinterConnectedEvent));
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_printer_connected_fn, result: {result}");

            result = set_on_ssdp_msg_fn(new OnSSDPMessageDelegate(OnSsdpMessageEvent));
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_ssdp_msg_fn, result: {result}");

        }

        private void OnSsdpMessageEvent(string topic)
        {
            Console.WriteLine("OnSsdpMessageEvent");
            throw new NotImplementedException();
        }

        private void OnPrinterConnectedEvent(string topic)
        {
            Console.WriteLine("OnPrinterConnectedEvent");
            throw new NotImplementedException();
        }

        private string OnGetCountryCodeEvent()
        {
            Console.WriteLine("OnGetCountryCodeEvent");
            throw new NotImplementedException();
        }

        private void OnServerConnectedEvent()
        {
            Debug.Print("Server connected");

            // Copying what Bambu Slicer does .. not sure why this is needed
            SelectedMachineDeviceId = SelectedMachineDeviceId;
        }

        private void OnCloudMessageEvent(string deviceId, string jsonMessage)
        {
            var message = JsonConvert.DeserializeObject<MessageDto>(jsonMessage);
            var printerMessage = message?.PrinterMessage;

            if (!Printers.ContainsKey(deviceId))
                Printers.Add(deviceId, new BambuPrinter(deviceId));
            
            var printer = Printers[deviceId];
            printer.ProcessMessage(printerMessage);
        }

        private void OnLocalMessageEvent(string deviceId, string jsonMessage)
        {
            var message = JsonConvert.DeserializeObject<MessageDto>(jsonMessage);
            var printerMessage = message?.PrinterMessage;

            if (!Printers.ContainsKey(deviceId))
                Printers.Add(deviceId, new BambuPrinter(deviceId));

            var printer = Printers[deviceId];
            printer.ProcessMessage(printerMessage);
        }
        
        private void OnHttpErrorEvent(uint httpStatusCode, string httpBody)
        {
            Console.WriteLine("OnHttpErrorEvent");
            throw new NotImplementedException();
        }

        private void OnLocalConnectEvent(int status, string deviceId, string message)
        {
            Console.WriteLine("OnLocalConnectEvent");
            throw new NotImplementedException();
        }

        private void OnUserLoginEvent(int onlineLogin, bool login)
        {
            Console.WriteLine("OnUserLoginEvent");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check for (and update) the local copy of bambu_networking.dll
        /// </summary>
        /// <param name="autoUpdateDll">Copy the bambu_networking.dll from Bambu Studio folder when true</param>
        /// <exception cref="FileNotFoundException">Thrown when bambu_networking.dll is missing</exception>
        private void ResolveBambuNetworkingDll(bool autoUpdateDll)
        {
            Debug.Print("Resolving bambu_networking.dll");

            var dllDestinationPath = ApplicationFolder + "\\bambu_networking.dll";

            if (autoUpdateDll && File.Exists(BambuStudioPluginFolder + "\\bambu_networking.dll"))
            {
                try
                {
                    // try to update the local copy with the currently installed version
                    File.Copy(BambuStudioPluginFolder + "\\bambu_networking.dll", dllDestinationPath, true);
                }
                catch
                {
                    // ignore exception if the DLL is in use or unable to write due to permissions etc
                }
            }

            if (!File.Exists(dllDestinationPath))
            {
                throw new FileNotFoundException($"Unable to find bambu_networking.dll in {BambuStudioPluginFolder} or {ApplicationFolder}. " +
                    $"Please install Bambu Studio, update the Bambu Network Plugin, and/or copy the dll yourself to this application's folder. " +
                    $"Also confirm the application has write permissions to its local folder, and/or check anti-virus logs to see if it was blocked from copying the dll.");
            }
        }

        /// <summary>
        /// Determines the location where bambu_networking.dll will load and save configurable settings and authentication token etc.
        /// We can share the existing config in Bambu Studio; however this may result in conflicts if both are running at the same time.
        /// </summary>
        /// <param name="folderPath">Use BambuNetworkEngine.conf in the supplied path. If not supplied, path will default to Bambu Studio location</param>
        /// <exception cref="Exception">Thrown when bambu_networking.dll is unable to set config folder path to the desired location</exception>
        public void SetConfigFolder(string folderPath = "")
        {
            Debug.Print("Setting config folder");

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = BambuNetworkPluginConfigFolder;
            }
            else
            {
                BambuNetworkPluginConfigFolder = folderPath;
            }

            var result = set_config_dir(new StringBuilder(folderPath));

            if (result != 0)
            {
                throw new Exception($"Unable to set Bambu Network Plugin config folder to {folderPath}");
            }
        }

        /// <summary>
        /// Should return the current Bambu Network Plugin dll version
        /// </summary>
        /// <returns>Version number or 00.00.00.00 if unable to resolve.</returns>
        public string NetworkPluginVersion => get_version();

        /// <summary>
        /// Creates a new subfolder 'log' and records activity happening in the Bambu Network Plugin dll
        /// </summary>
        public void InitializeNetworkAgentLog()
        {
            Debug.Print("Initializing Bambu Network Plugin log");

            var result = init_log();
            if (result != 0)
                throw new Exception($"Unable to initialize the Bambu Network Plugin log file; Check permissions for {BambuNetworkPluginLogFolder}");
        }

        public void ConnectServer()
        {
            Debug.Print("Connecting to Bambu server");

            var result = connect_server();

            if (result != 0)
                throw new Exception($"Unable to connect to Bambu server; result code {result}");
        }

        public void ConnectPrinter()
        {
            Debug.Print("Connecting to printer");

            var result = connect_printer();

            if (result != 0)
                throw new Exception($"Unable to connect to printer; result code {result}");
        }

        public void DisconnectPrinter()
        {
            Debug.Print("Disconnecting printer");

            var result = disconnect_printer();

            if (result != 0)
                throw new Exception($"Unable to disconnect printer; result code {result}");
        }

        public void UserLogout()
        {
            Debug.Print("User logout");

            var result = user_logout();

            if (result != 0)
                throw new Exception($"Unable to log out; result code {result}");
        }

        public string UserName => get_user_name();
        
        public string UserAvatar => get_user_avatar();
        
        public string UserNickname => get_user_nickanme();

        public string UserId => get_user_id();

        public string BuildLoginCmd()
        {
            Debug.Print("Build login cmd");

            return build_login_cmd();
        }

        public string BuildLogoutCmd()
        {
            Debug.Print("Build logout cmd");

            return build_logout_cmd();
        }

        public string BuildLoginInfo()
        {
            Debug.Print("Build login info");

            return build_login_info();
        }


        /// <summary>
        /// Closes the log file and updates the Bambu Network Plugin config.
        /// Finally, deallocates and frees the network agent instance from memory.
        /// </summary>
        public void Dispose()
        {
            Debug.Print("Dispose and deallocate Bambu Network Plugin");

            if (Agent != UIntPtr.Zero)
            {
                var result = bambu_network_destroy_agent();
                Agent = UIntPtr.Zero;
            }
        }

        public void Start()
        {
            start();
        }

        public void SetCertFile(string certFolder, string certFilename)
        {
            Debug.Print($"Set cert file {certFolder}\\{certFilename}");

            var result = set_cert_file(new StringBuilder(certFolder), new StringBuilder(certFilename));

            if (result != 0)
                throw new Exception($"Unable to set cert file, result: {result}");
        }

        public void SetCountryCode(string countryCode)
        {
            Debug.Print($"Set country code {countryCode}");

            int result = set_country_code(new StringBuilder(countryCode));

            if (result != 0)
                throw new Exception($"Unable to set country code, result: {result}");
        }

        public bool IsUserLoggedIn => is_user_login();

        public bool IsServerConnected => is_server_connected();

        public string SelectedMachineDeviceId
        {
            get => get_user_selected_machine();
            set
            {
                var result = set_user_selected_machine(new StringBuilder(value));

                if (result != 0)
                    throw new Exception($"Unable to set selected machine, result: {result}");
            }
        }

        public bool LanMode { get; private set; } = false;
        public int GcodeSequenceNumber { get; private set; } = 20000;
        public void SendMessageToPrinter(JObject jsonMessageObject, int qos = 0)
        {
            if (string.IsNullOrEmpty(SelectedMachineDeviceId))
                throw new Exception("DeviceId is not set; first, please login and bind the printer with Bambu Studio");

            if (string.IsNullOrEmpty(UserId))
                throw new Exception("UserId is not set; first, please login and bind the printer with Bambu Studio");
            
            var json = JsonConvert.SerializeObject(jsonMessageObject, Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            if (LanMode)
            {
                Debug.Print($"Sending message (via lan) to printer with device id {SelectedMachineDeviceId} and qos {qos}");

                var result = send_message_to_printer(new StringBuilder(SelectedMachineDeviceId), new StringBuilder(json), qos);

                if (result != 0)
                    throw new Exception($"Unable to send message to printer via lan, result: {result}");
            }
            else
            {
                Debug.Print($"Sending message (via cloud) to printer with device id {SelectedMachineDeviceId} and qos {qos}");

                var result = send_message(new StringBuilder(SelectedMachineDeviceId), new StringBuilder(json), qos);

                if (result != 0)
                    throw new Exception($"Unable to send message to printer via cloud, result: {result}");
            }
        }

        public void SetFan()
        {
            //std::string gcode = (boost::format("M106 P%1% S%2% \n") % (int)fan_type % (on_off ? 255 : 0)).str();
        }

        public void SendGcode(string gcode)
        {
            var payload = new
            {
                print = new
                {
                    command = "gcode_line",
                    param = gcode,
                    sequence_id = (GcodeSequenceNumber++).ToString(),
                    user_id = UserId
                }
            };

            JObject messageObject = JObject.FromObject(payload);

            SendMessageToPrinter(messageObject);
        }

        public void RefreshConnection()
        {
            Debug.Print("RefreshConnection (connect to mqtt)");

            var result = refresh_connection();

            if (result != 0)
                throw new Exception("Unable to connect to mqtt; first, please login and bind the printer with Bambu Studio");
        }

        public void Subscribe(string module = "app")
        {
            Debug.Print($"Subscribe to module {module}");

            var result = start_subscribe(new StringBuilder(module));

            if (result != 0)
                throw new Exception($"Unable to subscribe to module {module}");
        }
        public void Unsubscribe(string module = "app")
        {
            Debug.Print($"Unsubscribe from module {module}");

            var result = stop_subscribe(new StringBuilder(module));

            if (result != 0)
                throw new Exception($"Unable to unsubscribe from module {module}");
        }

        public void WipeNozzle()
        {
            // test - do at your own risk
            // (below code intentionally not indented)
            SendGcode(@"
G92 E0
G1 E-0.5 F300
G1 X70 Y265 F9000
G1 X76 F15000
G1 X65 F15000
G1 X76 F15000
G1 X65 F15000; shake to put down garbage
G1 X80 F6000
G1 X95 F15000
G1 X80 F15000
G1 X165 F15000; wipe and shake
");
        }
    }
}