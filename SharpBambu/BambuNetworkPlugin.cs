using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SharpBambu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambu
{
    public class BambuNetworkPlugin : IDisposable
    {
        public static ILogger Log { get; private set; } = Serilog.Log.ForContext<BambuNetworkPlugin>();

        // Callback functions need to be scoped here
        private OnServerConnectedDelegate InstanceOnServerConnectedDelegate;
        private OnPrinterMessageDelegate InstanceOnCloudMessageDelegate;
        private OnPrinterMessageDelegate InstanceOnLocalMessageDelegate;
        private OnGetCameraUrlDelegate InstanceOnGetCameraUrlDelegate;
        private OnLocalConnectedDelegate InstanceOnLocalConnectedDelegate;
        //private OnLoginDelegate InstanceOnLoginDelegate;
        private OnUserLoginDelegate InstanceOnUserLoginDelegate;
        private OnSSDPMessageDelegate InstanceOnSSDPMessageDelegate;
        private OnGetCountryCodeDelegate InstanceOnGetCountryCodeDelegate;
        private OnHttpErrorDelegate InstanceOnHttpErrorDelegate;
        private OnProgressUpdatedDelegate InstanceOnProgressUpdatedDelegate;
        private OnResultDelegate InstanceOnResultDelegate;
        private OnPrinterConnectedDelegate InstanceOnPrinterConnectedDelegate;
        private WasCancelledDelegate InstanceWasCancelled_SendGcodeToSdCardDelegate;
        private OnUpdateStatusDelegate InstanceOnUpdateStatus_SendGcodeToSdCardDelegate;

        public BambuPrinter Printer { get; private set; } = new BambuPrinter();

        public BambuNetworkPlugin()
        {
            InstanceOnServerConnectedDelegate = OnServerConnectedEvent;
            InstanceOnCloudMessageDelegate = OnCloudMessageEvent;
            InstanceOnLocalMessageDelegate = OnLocalMessageEvent;
            InstanceOnGetCameraUrlDelegate = OnGetCameraUrlEvent;
            InstanceOnLocalConnectedDelegate = OnLocalConnectEvent;
            InstanceOnUserLoginDelegate = OnUserLoginEvent;
            InstanceOnSSDPMessageDelegate = OnSsdpMessageEvent;
            InstanceOnGetCountryCodeDelegate = OnGetCountryCodeEvent;
            InstanceOnHttpErrorDelegate = OnHttpErrorEvent;
            InstanceOnProgressUpdatedDelegate = OnProgressUpdatedEvent;
            InstanceOnResultDelegate = OnResultEvent;
            InstanceOnPrinterConnectedDelegate = OnPrinterConnectedEvent;
            InstanceWasCancelled_SendGcodeToSdCardDelegate = WasCancelled_SendGcodeToSdCardEvent;
            InstanceOnUpdateStatus_SendGcodeToSdCardDelegate = OnUpdateStatus_SendGcodeToSdCardEvent;
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
        private static extern int get_camera_url ([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder deviceId);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int send_message_to_printer([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder deviceId, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder jsonMessage, int qos);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern int send_message([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder deviceId, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder jsonMessage, int qos);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)] 
        private static extern bool is_user_login();

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)] 
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
        private static extern int connect_printer([In, MarshalAs(UnmanagedType.LPStr)] StringBuilder deviceId, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder ipAddress, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder username, [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder password);

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

        // https://stackoverflow.com/questions/15660722/why-are-cdecl-calls-often-mismatched-in-the-standard-p-invoke-convention
        // https://learn.microsoft.com/en-us/cpp/build/x64-calling-convention?view=msvc-170
        // https://www.codeproject.com/Articles/1187064/Nightmare-on-Overwh-Elm-Street-The-bit-Calling-Con
        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern BambuEnums.NetworkStatus start_send_gcode_to_sdcard(
            [In, MarshalAs(UnmanagedType.LPStr)] string printParamsJson,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnUpdateStatusDelegate update_fn, 
            [MarshalAs(UnmanagedType.FunctionPtr)] WasCancelledDelegate cancel_fn);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern BambuEnums.NetworkStatus start_print(
            [In, MarshalAs(UnmanagedType.LPStr)] string printParamsJson,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnUpdateStatusDelegate update_fn,
            [MarshalAs(UnmanagedType.FunctionPtr)] WasCancelledDelegate cancel_fn);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern BambuEnums.NetworkStatus start_local_print(
            [In, MarshalAs(UnmanagedType.LPStr)] string printParamsJson,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnUpdateStatusDelegate update_fn,
            [MarshalAs(UnmanagedType.FunctionPtr)] WasCancelledDelegate cancel_fn);

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)] 
        private static extern bool start_discovery([MarshalAs(UnmanagedType.U1)] bool start, [MarshalAs(UnmanagedType.U1)] bool sending);

        // events
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnSSDPMessageDelegate([MarshalAs(UnmanagedType.BStr)] string dev_info_json);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUserLoginDelegate(int onlineLogin, [MarshalAs(UnmanagedType.U1)] bool login);
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnPrinterConnectedDelegate([MarshalAs(UnmanagedType.BStr)] string topic);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnServerConnectedDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnHttpErrorDelegate(uint httpStatusCode, [MarshalAs(UnmanagedType.BStr)] string httpBody);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnPrinterMessageDelegate([MarshalAs(UnmanagedType.BStr)] string deviceId, [MarshalAs(UnmanagedType.BStr)] string message);

        //[UnmanagedFunctionPointer(CallingConvention.Winapi)]
        //private delegate void OnMessageArrivedDelegate([MarshalAs(UnmanagedType.BStr)] string deviceInfoJson);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate string OnGetCountryCodeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnLocalConnectedDelegate(BambuEnums.ConnectStatus status, [MarshalAs(UnmanagedType.BStr)] string deviceId, [MarshalAs(UnmanagedType.BStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnGetCameraUrlDelegate([MarshalAs(UnmanagedType.BStr)] string url);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUserLogin(int onlineLogin, [MarshalAs(UnmanagedType.U1)] bool login);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUpdateStatusDelegate(int status, int code, [MarshalAs(UnmanagedType.BStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.I1)] 
        private delegate bool WasCancelledDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnProgressUpdatedDelegate(int progress);

        //[UnmanagedFunctionPointer(CallingConvention.Winapi)]
        //private delegate void OnLoginDelegate(int retcode, [MarshalAs(UnmanagedType.BStr)] string info);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnResultDelegate(int retcode, [MarshalAs(UnmanagedType.BStr)] string info);


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

        [DllImport("NetworkPluginWrapper.dll", CharSet = CharSet.Ansi)]
        private static extern void set_get_camera_url_callback([MarshalAs(UnmanagedType.FunctionPtr)] OnGetCameraUrlDelegate callbackFunction);

        #endregion

        /// <summary>
        /// Create an instance of the Bambu Network Plugin; requires a local copy of bambu_networking.dll
        /// </summary>
        /// <param name="copyNetworkingDllFromBambuStudio">Copy the bambu_networking.dll from Bambu Studio folder when true</param>
        /// <exception cref="FileNotFoundException">Thrown when bambu_networking.dll is missing</exception>
        /// <exception cref="Exception">Thrown when NetworkPluginWrapper.dll fails to initialize</exception>
        public void InitializeNetworkPlugin(bool copyNetworkingDllFromBambuStudio = true)
        {
            ResolveBambuNetworkingDll(copyNetworkingDllFromBambuStudio);

            Log.Information("Setting data dir to {BambuStudioDataFolder}", BambuStudioDataFolder);
            set_data_dir(new StringBuilder(BambuStudioDataFolder));

            Log.Information("Setting dll dir to {BambuStudioPluginFolder}", BambuStudioPluginFolder);
            set_dll_dir(new StringBuilder(BambuStudioPluginFolder));

            Log.Information("Initialize network agent wrapper dll");
            var result = initialize_network_module();

            if (result != 0)
                throw new Exception("Bambu Network Plugin wrapper DLL failed to initialize");

            Log.Information("Creating network agent");
            Agent = bambu_network_create_agent();

            if (Agent == UIntPtr.Zero)
                throw new Exception("Bambu Network Plugin failed to initialize");

            Log.Information("Version {Version}", NetworkPluginVersion);
        }

        public void InitCallbacks()
        {
            var result = 0;

            Log.Debug("Setting up callbacks");

            result = set_on_user_login_fn(InstanceOnUserLoginDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_user_login_fn, result: {result}");

            result = set_on_local_connect_fn(InstanceOnLocalConnectedDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_local_connect_fn, result: {result}");

            result = set_on_http_error_fn(InstanceOnHttpErrorDelegate);
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

            result = set_get_country_code_fn(InstanceOnGetCountryCodeDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_get_country_code_fn, result: {result}");

            result = set_on_printer_connected_fn(InstanceOnPrinterConnectedDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_printer_connected_fn, result: {result}");

            result = set_on_ssdp_msg_fn(InstanceOnSSDPMessageDelegate);
            if (result != 0)
                throw new Exception($"Unable to initialize callback: set_on_ssdp_msg_fn, result: {result}");

            // returns void
            set_get_camera_url_callback(InstanceOnGetCameraUrlDelegate);
        }

        public void StartSSDPDiscovery()
        {
            Log.Information("Starting SSDP discovery on UDP port 2021");

            var result = start_discovery(true, false);

            if (!result)
                throw new Exception("Unable to start SSDP discovery");
        }
        public void StopSSDPDiscovery()
        {
            Log.Information("Stopping SSDP discovery on UDP port 2021");

            var result = start_discovery(false, false);

            if (!result)
                throw new Exception("Unable to stop SSDP discovery");
        }

        private void OnSsdpMessageEvent(string dev_info_json)
        {
            Log.Verbose("SSDP message, topic={dev_info_json}", dev_info_json);
        }

        private void OnPrinterConnectedEvent(string topic)
        {
            Log.Information("Printer connected, topic={topic}", topic);
        }

        private string OnGetCountryCodeEvent()
        {
            Log.Debug("Get Country Code");
            throw new NotImplementedException();
        }

        private void OnServerConnectedEvent()
        {
            Log.Information("Server connected");
        }

        private void OnCloudMessageEvent(string deviceId, string jsonMessage)
        {
            var message = JsonConvert.DeserializeObject<MessageDto>(jsonMessage);
            var printerMessage = message?.PrinterMessage;

            if (printerMessage == null)
                return;

            Printer.ProcessMessage(printerMessage);
        }

        private void OnLocalMessageEvent(string deviceId, string jsonMessage)
        {
            var message = JsonConvert.DeserializeObject<MessageDto>(jsonMessage);
            var printerMessage = message?.PrinterMessage;

            if (printerMessage == null)
                return;

            Printer.ProcessMessage(printerMessage);
        }
        
        private void OnHttpErrorEvent(uint httpStatusCode, string httpBody)
        {
            Log.Error("Http Error httpStatusCode={httpStatusCode}, string={httpBody}", httpStatusCode, httpBody);
            throw new NotImplementedException();
        }

        private void OnLocalConnectEvent(BambuEnums.ConnectStatus status, string deviceId, string message)
        {
            Log.Information("Printer {deviceId} connected locally with status {status}", deviceId, status);
            Log.Debug("Message: {message}", message);

            ConnectionStatus = status;
        }

        private void OnUserLoginEvent(int onlineLogin, bool login)
        {
            Log.Information("User login onlineLogin={onlineLogin}, login={login}", onlineLogin, login);
            throw new NotImplementedException();
        }

        private void OnGetCameraUrlEvent(string url)
        {
            Log.Debug("Camera URL response, URL={url}", url);

            Printer.CameraUrl = url;
        }

        private void OnResultEvent(int retcode, string info)
        {
            Log.Debug("Result returned, retcode={retcode}, info={info}", retcode, info);
            throw new NotImplementedException();
        }

        private void OnProgressUpdatedEvent(int progress)
        {
            Log.Debug("Progress update, progress={progress}", progress);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check for (and update) the local copy of bambu_networking.dll
        /// </summary>
        /// <param name="copyNetworkingDllFromBambuStudio">Copy the bambu_networking.dll from Bambu Studio folder when true</param>
        /// <exception cref="FileNotFoundException">Thrown when bambu_networking.dll is missing</exception>
        private void ResolveBambuNetworkingDll(bool copyNetworkingDllFromBambuStudio)
        {
            Log.Debug("Resolving bambu_networking.dll, looking in path {Path}", ApplicationFolder + "\\bambu_networking.dll");

            var dllDestinationPath = ApplicationFolder + "\\bambu_networking.dll";

            if (copyNetworkingDllFromBambuStudio && File.Exists(BambuStudioPluginFolder + "\\bambu_networking.dll"))
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
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = BambuNetworkPluginConfigFolder;
            }
            else
            {
                BambuNetworkPluginConfigFolder = folderPath;
            }

            Log.Debug("Setting config folder to {folderPath}", folderPath);

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
            Log.Information("Initializing Bambu Network Plugin log, should be at location {BambuNetworkPluginLogFolder}", BambuNetworkPluginLogFolder);

            var result = init_log();
            if (result != 0)
                throw new Exception($"Unable to initialize the Bambu Network Plugin log file; Check permissions for {BambuNetworkPluginLogFolder}");
        }

        public void ConnectServer()
        {
            LanMode = false;
            Log.Information("Connecting to Bambu Cloud");

            var result = connect_server();

            if (result != 0)
                throw new Exception($"Unable to connect to Bambu server; result code {result}");
        }

        public void ConnectPrinter(string deviceId, string ipAddress, string username, string password)
        {
            LanMode = true;

            // if the printer is not presently bound in your account, this may still work:
            SelectedMachineDeviceId = deviceId;

            Log.Information("Connecting to printer {deviceId} at {ipAddress} in Lan mode", deviceId, ipAddress);

            var result = connect_printer(new StringBuilder(deviceId), new StringBuilder(ipAddress), new StringBuilder(username), new StringBuilder(password));

            if (result != 0)
                throw new Exception($"Unable to connect to printer; result code {result}");
        }

        public void DisconnectPrinter()
        {
            Log.Information("Disconnecting from printer");

            var result = disconnect_printer();

            if (result != 0)
                throw new Exception($"Unable to disconnect printer; result code {result}");
        }

        public void UserLogout()
        {
            Log.Information("Logging out");

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
            Log.Debug("Build login cmd");

            return build_login_cmd();
        }

        public string BuildLogoutCmd()
        {
            Log.Debug("Build logout cmd");

            return build_logout_cmd();
        }

        public string BuildLoginInfo()
        {
            Log.Debug("Build login info");

            return build_login_info();
        }


        /// <summary>
        /// Closes the log file and updates the Bambu Network Plugin config.
        /// Finally, deallocates and frees the network agent instance from memory.
        /// </summary>
        public void Dispose()
        {
            Log.Debug("Dispose and deallocate Bambu Network Plugin");

            if (Agent != UIntPtr.Zero)
            {
                var result = bambu_network_destroy_agent();
                Agent = UIntPtr.Zero;
            }
        }

        public void Start()
        {
            Log.Information("Starting Bambu Networking");
            start();
        }

        public void SetCertFile(string certFolder, string certFilename)
        {
            Log.Debug("Set cert file {certFolder}\\{certFilename}", certFolder, certFilename);

            var result = set_cert_file(new StringBuilder(certFolder), new StringBuilder(certFilename));

            if (result != 0)
                throw new Exception($"Unable to set cert file, result: {result}");
        }

        public void SetCountryCode(string countryCode)
        {
            Log.Debug("Set country code {countryCode}", countryCode);

            int result = set_country_code(new StringBuilder(countryCode));

            if (result != 0)
                throw new Exception($"Unable to set country code, result: {result}");
        }

        public bool IsUserLoggedIn => is_user_login();

        public bool IsServerConnected => is_server_connected();

        private string? _selectedMachineDeviceID;

        public string SelectedMachineDeviceId
        {
            get => _selectedMachineDeviceID ?? get_user_selected_machine();
            set
            {
                Log.Debug("Selected machine was set to {value}", value);

                var result = set_user_selected_machine(new StringBuilder(value));

                if (result != 0)
                    throw new Exception($"Unable to set selected machine, result: {result}");

                _selectedMachineDeviceID = value;
            }
        }

        public bool LanMode { get; private set; } = false;
        public BambuEnums.ConnectStatus ConnectionStatus { get; private set; }

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
                Log.Verbose("Sending message (via lan) to printer with device id {SelectedMachineDeviceId} and qos {qos}", SelectedMachineDeviceId, qos);

                var result = send_message_to_printer(new StringBuilder(SelectedMachineDeviceId), new StringBuilder(json), qos);

                if (result != 0)
                    throw new Exception($"Unable to send message to printer via lan, result: {result}");
            }
            else
            {
                Log.Verbose("Sending message (via cloud) to printer with device id {SelectedMachineDeviceId} and qos {qos}", SelectedMachineDeviceId, qos);

                var result = send_message(new StringBuilder(SelectedMachineDeviceId), new StringBuilder(json), qos);

                if (result != 0)
                    throw new Exception($"Unable to send message to printer via cloud, result: {result}");
            }
        }

        public void SendGcode(string gcode)
        {
            Log.Verbose("Send gcode {gcode}", gcode);

            var payload = new
            {
                print = new
                {
                    command = "gcode_line",
                    param = gcode,
                    sequence_id = (Printer.GcodeSequenceNumber++).ToString(),
                    user_id = UserId
                }
            };

            JObject messageObject = JObject.FromObject(payload);

            SendMessageToPrinter(messageObject);
        }

        public void RefreshConnection()
        {
            Log.Debug("Refreshing connection to mqtt");

            var result = refresh_connection();

            if (result != 0)
                throw new Exception("Unable to connect to mqtt; first, please login and bind the printer with Bambu Studio");
        }

        public void Subscribe(string module = "app")
        {
            Log.Debug("Subscribing to Bambu Networking events for module/topic {module}", module);

            var result = start_subscribe(new StringBuilder(module));

            if (result != 0)
                throw new Exception($"Unable to subscribe to module {module}");
        }
        public void Unsubscribe(string module = "app")
        {
            Log.Debug("Unsubscribe from Bambu Networking events for module/topic {module}", module);

            var result = stop_subscribe(new StringBuilder(module));

            if (result != 0)
                throw new Exception($"Unable to unsubscribe from module {module}");
        }

        public void RefreshCameraUrl()
        {
            if (string.IsNullOrEmpty(SelectedMachineDeviceId))
                throw new Exception("DeviceId is not set; first, please login and bind the printer with Bambu Studio");

            var result = get_camera_url(new StringBuilder(SelectedMachineDeviceId));

            if (result != 0)
                throw new Exception($"Unable to refresh camera url, result: {result}");
        }

        public void SendGcodeToSdCard(string localGcodeFilePath, string destFileName, string deviceId, string ipAddress, string username, string password,
            int plateIndex, string projectName, string taskName, string presetName, string configFileName,
            string amsMapping, string amsMappingInfo, bool bedLeveling, bool flowCalibration, bool layerInspect, bool useAms, bool recordTimeLapse,
            bool vibrationCalibration, string connectionType = "lan")
        {
            SelectedMachineDeviceId = deviceId;

            Log.Information("Sending {localGcodeFilePath} to printer SD Card", localGcodeFilePath);

            if (!File.Exists(localGcodeFilePath))
                throw new Exception($"SendGcodeToSdCard: File does not exist: {localGcodeFilePath}");

            Log.Verbose("Calculating MD5 on {localGcodeFilePath}", localGcodeFilePath);

            using var md5 = MD5.Create();
            using var stream = File.OpenRead(localGcodeFilePath);
            var localGcodeFileMd5Bytes = md5.ComputeHash(stream);
            //var localGcodeFileMd5 = BitConverter.ToString(localGcodeFileMd5Bytes).Replace("-", "");
            var localGcodeFileMd5 = Convert.ToBase64String(localGcodeFileMd5Bytes);



            var printParams = new BambuStructs.PrintParams()
            {
                dev_id = deviceId,
                dev_ip = ipAddress,
                username = username,
                password = password,
                filename = localGcodeFilePath,
                ftp_file = destFileName,
                ftp_file_md5 = localGcodeFileMd5,
                plate_index = plateIndex,
                project_name = projectName,
                task_name = taskName,
                preset_name = presetName,
                connection_type = connectionType,
                config_filename = configFileName,
                task_bed_leveling = bedLeveling,
                task_flow_cali = flowCalibration,
                task_layer_inspect = layerInspect,
                task_use_ams = useAms,
                task_record_timelapse = recordTimeLapse,
                task_vibration_cali = vibrationCalibration,
                ams_mapping = amsMapping,
                ams_mapping_info = amsMappingInfo,
                comments = "",
                use_ssl = LanMode ? false : true,
                origin_model_id = "", // tbd
                origin_profile_id = 1 // tbd
            };

            var printParamsJson = JsonConvert.SerializeObject(printParams, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            });

            Log.Verbose(printParamsJson);

            var result = start_send_gcode_to_sdcard(printParamsJson, InstanceOnUpdateStatus_SendGcodeToSdCardDelegate, InstanceWasCancelled_SendGcodeToSdCardDelegate);
            //var result = start_print(printParamsJson, InstanceOnUpdateStatus_SendGcodeToSdCardDelegate, InstanceWasCancelled_SendGcodeToSdCardDelegate);
            //var result = start_local_print(printParamsJson, InstanceOnUpdateStatus_SendGcodeToSdCardDelegate, InstanceWasCancelled_SendGcodeToSdCardDelegate);

            Log.Debug("Send to SD Card submitted, result={result}", result);            
        }

        private void OnUpdateStatus_SendGcodeToSdCardEvent(int status, int code, string message)
        {
            Log.Debug("Send to SD, status={status}, progress={code} ({message})", status, code, message);
        }
        private bool WasCancelled_SendGcodeToSdCardEvent()
        {
            Log.Verbose("Not cancelled");

            return false; // was cancelled
        }

        public void WipeNozzle()
        {
            Log.Debug("Sending nozzle wipe sequence");

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