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
        // Callback functions need to be scoped here
        private OnServerConnectedDelegate InstanceOnServerConnectedDelegate;
        private OnPrinterMessageDelegate InstanceOnCloudMessageDelegate;
        private OnPrinterMessageDelegate InstanceOnLocalMessageDelegate;
        private OnGetCameraUrlDelegate InstanceOnGetCameraUrlDelegate;
        private OnLocalConnectedDelegate InstanceOnLocalConnectedDelegate;
        //private OnLoginDelegate InstanceOnLoginDelegate;
        private OnUserLoginDelegate InstanceOnUserLoginDelegate;
        private OnSSDPMessageDelegate InstanceOnSSDPMessageDelegate;
        private OnCancelDelegate InstanceOnCancelDelegate;
        private OnGetCountryCodeDelegate InstanceOnGetCountryCodeDelegate;
        private OnHttpErrorDelegate InstanceOnHttpErrorDelegate;
        private OnProgressUpdatedDelegate InstanceOnProgressUpdatedDelegate;
        private OnResultDelegate InstanceOnResultDelegate;
        private OnUpdateStatusDelegate InstanceOnUpdateStatusDelegate;
        private OnWasCanceledDelegate InstanceOnWasCanceledDelegate;
        private OnPrinterConnectedDelegate InstanceOnPrinterConnectedDelegate;

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
            InstanceOnCancelDelegate = OnCancelEvent;
            InstanceOnGetCountryCodeDelegate = OnGetCountryCodeEvent;
            InstanceOnHttpErrorDelegate = OnHttpErrorEvent;
            InstanceOnProgressUpdatedDelegate = OnProgressUpdatedEvent;
            InstanceOnResultDelegate = OnResultEvent;
            InstanceOnUpdateStatusDelegate = OnUpdateStatusEvent;
            InstanceOnWasCanceledDelegate = OnWasCanceledEvent;
            InstanceOnPrinterConnectedDelegate = OnPrinterConnectedEvent;
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



        // events
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnSSDPMessageDelegate([MarshalAs(UnmanagedType.BStr)] string topic);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUserLoginDelegate(int onlineLogin, bool login);
        
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
        private delegate void OnUserLogin(int onlineLogin, bool login);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void OnUpdateStatusDelegate(int status, int code, [MarshalAs(UnmanagedType.BStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool OnWasCanceledDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool OnCancelDelegate();

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
        /// <param name="autoUpdateDll">Copy the bambu_networking.dll from Bambu Studio folder when true</param>
        /// <exception cref="FileNotFoundException">Thrown when bambu_networking.dll is missing</exception>
        /// <exception cref="Exception">Thrown when NetworkPluginWrapper.dll fails to initialize</exception>
        public void InitializeNetworkPlugin(bool autoUpdateDll = true)
        {
            ResolveBambuNetworkingDll(autoUpdateDll);

            Console.WriteLine($"Setting data dir to {BambuStudioDataFolder}");
            set_data_dir(new StringBuilder(BambuStudioDataFolder));

            Console.WriteLine($"Setting dll dir to {BambuStudioPluginFolder}");
            set_dll_dir(new StringBuilder(BambuStudioPluginFolder));

            Console.WriteLine("Initialize network agent wrapper dll");
            var result = initialize_network_module();

            if (result != 0)
                throw new Exception("Bambu Network Plugin wrapper DLL failed to initialize");

            Console.WriteLine("Creating network agent");
            Agent = bambu_network_create_agent();

            if (Agent == UIntPtr.Zero)
                throw new Exception("Bambu Network Plugin failed to initialize");
        }

        public void InitCallbacks()
        {
            var result = 0;

            Console.WriteLine("Setting up callbacks");

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

        private void OnSsdpMessageEvent(string topic)
        {
            Console.WriteLine($"OnSsdpMessageEvent: topic={topic}");
            throw new NotImplementedException();
        }

        private void OnPrinterConnectedEvent(string topic)
        {
            Console.WriteLine($"OnPrinterConnectedEvent: topic={topic}");
        }

        private string OnGetCountryCodeEvent()
        {
            Console.WriteLine("OnGetCountryCodeEvent");
            throw new NotImplementedException();
        }

        private void OnServerConnectedEvent()
        {
            Console.WriteLine("OnServerConnectedEvent");
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
            Console.WriteLine($"OnHttpErrorEvent: httpStatusCode={httpStatusCode}, string={httpBody}");
            throw new NotImplementedException();
        }

        private void OnLocalConnectEvent(BambuEnums.ConnectStatus status, string deviceId, string message)
        {
            Console.WriteLine($"OnLocalConnectEvent: Printer {deviceId} is locally connected with status {status}; message:");
            Console.WriteLine(message);

            ConnectionStatus = status;
        }

        private void OnUserLoginEvent(int onlineLogin, bool login)
        {
            Console.WriteLine($"OnUserLoginEvent: onlineLogin={onlineLogin}, login={login}");
            throw new NotImplementedException();
        }

        private void OnGetCameraUrlEvent(string url)
        {
            Console.WriteLine($"OnGetCameraUrlEvent: Camera URL={url}");

            Printer.CameraUrl = url;
        }

        private bool OnWasCanceledEvent()
        {
            Console.WriteLine($"OnWasCanceledEvent");
            throw new NotImplementedException();
        }

        private void OnUpdateStatusEvent(int status, int code, string message)
        {
            Console.WriteLine($"OnUpdateStatusEvent: status={status}, code={code}, message:");
            Console.WriteLine(message);
            throw new NotImplementedException();
        }

        private void OnResultEvent(int retcode, string info)
        {
            Console.WriteLine($"OnResultEvent: onlineLogin={retcode}, login={info}");
            throw new NotImplementedException();
        }

        private void OnProgressUpdatedEvent(int progress)
        {
            Console.WriteLine($"OnProgressUpdatedEvent: progress={progress}");
            throw new NotImplementedException();
        }

        private bool OnCancelEvent()
        {
            Console.WriteLine($"OnCancelEvent");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check for (and update) the local copy of bambu_networking.dll
        /// </summary>
        /// <param name="autoUpdateDll">Copy the bambu_networking.dll from Bambu Studio folder when true</param>
        /// <exception cref="FileNotFoundException">Thrown when bambu_networking.dll is missing</exception>
        private void ResolveBambuNetworkingDll(bool autoUpdateDll)
        {
            Console.WriteLine("Resolving bambu_networking.dll");

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
            Console.WriteLine("Setting config folder");

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
            Console.WriteLine("Initializing Bambu Network Plugin log");

            var result = init_log();
            if (result != 0)
                throw new Exception($"Unable to initialize the Bambu Network Plugin log file; Check permissions for {BambuNetworkPluginLogFolder}");
        }

        public void ConnectServer()
        {
            LanMode = false;
            Console.WriteLine("Connecting to Bambu server");

            var result = connect_server();

            if (result != 0)
                throw new Exception($"Unable to connect to Bambu server; result code {result}");
        }

        public void ConnectPrinter(string deviceId, string ipAddress, string username, string password)
        {
            LanMode = true;

            // if the printer is not presently bound in your account, this may still work:
            SelectedMachineDeviceId = deviceId;

            Console.WriteLine($"Connecting to printer {deviceId} at {ipAddress}");

            var result = connect_printer(new StringBuilder(deviceId), new StringBuilder(ipAddress), new StringBuilder(username), new StringBuilder(password));

            if (result != 0)
                throw new Exception($"Unable to connect to printer; result code {result}");
        }

        public void DisconnectPrinter()
        {
            Console.WriteLine("Disconnecting printer");

            var result = disconnect_printer();

            if (result != 0)
                throw new Exception($"Unable to disconnect printer; result code {result}");
        }

        public void UserLogout()
        {
            Console.WriteLine("User logout");

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
            Console.WriteLine("Build login cmd");

            return build_login_cmd();
        }

        public string BuildLogoutCmd()
        {
            Console.WriteLine("Build logout cmd");

            return build_logout_cmd();
        }

        public string BuildLoginInfo()
        {
            Console.WriteLine("Build login info");

            return build_login_info();
        }


        /// <summary>
        /// Closes the log file and updates the Bambu Network Plugin config.
        /// Finally, deallocates and frees the network agent instance from memory.
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("Dispose and deallocate Bambu Network Plugin");

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
            Console.WriteLine($"Set cert file {certFolder}\\{certFilename}");

            var result = set_cert_file(new StringBuilder(certFolder), new StringBuilder(certFilename));

            if (result != 0)
                throw new Exception($"Unable to set cert file, result: {result}");
        }

        public void SetCountryCode(string countryCode)
        {
            Console.WriteLine($"Set country code {countryCode}");

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
                var result = set_user_selected_machine(new StringBuilder(value));

                if (result != 0)
                    throw new Exception($"Unable to set selected machine, result: {result}");

                _selectedMachineDeviceID = value;
            }
        }

        public bool LanMode { get; private set; } = false;
        public int GcodeSequenceNumber { get; private set; } = 20000;
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
                Console.WriteLine($"Sending message (via lan) to printer with device id {SelectedMachineDeviceId} and qos {qos}");

                var result = send_message_to_printer(new StringBuilder(SelectedMachineDeviceId), new StringBuilder(json), qos);

                if (result != 0)
                    throw new Exception($"Unable to send message to printer via lan, result: {result}");
            }
            else
            {
                Console.WriteLine($"Sending message (via cloud) to printer with device id {SelectedMachineDeviceId} and qos {qos}");

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
            Console.WriteLine("RefreshConnection (connect to mqtt)");

            var result = refresh_connection();

            if (result != 0)
                throw new Exception("Unable to connect to mqtt; first, please login and bind the printer with Bambu Studio");
        }

        public void Subscribe(string module = "app")
        {
            Console.WriteLine($"Subscribe to module {module}");

            var result = start_subscribe(new StringBuilder(module));

            if (result != 0)
                throw new Exception($"Unable to subscribe to module {module}");
        }
        public void Unsubscribe(string module = "app")
        {
            Console.WriteLine($"Unsubscribe from module {module}");

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