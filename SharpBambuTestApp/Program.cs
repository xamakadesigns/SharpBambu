// https://blog.elmah.io/debugging-system-accessviolationexception/
// https://limbioliong.wordpress.com/2011/06/16/returning-strings-from-a-c-api/
// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpBambu;
using SharpBambuTestApp;

public class Program
{
    public static ILogger Log { get; private set; } = Serilog.Log.ForContext<Program>();
    private static BambuNetworkPlugin NetworkPlugin { get; set; } = new BambuNetworkPlugin();
    private static BambuWrapperConfig Config { get; set; } = new BambuWrapperConfig();

    public static void Main(string[] args)
    {
        Config.Load();
        
        Log = Serilog.Log.ForContext<Program>();

        NetworkPlugin.InitializeNetworkPlugin(Config.copyNetworkingDllFromBambuStudio);

        NetworkPlugin.SetConfigFolder(NetworkPlugin.BambuNetworkPluginConfigFolder);
        NetworkPlugin.SetCertFile("C:\\Program Files\\Bambu Studio\\resources\\cert", "slicer_base64.cer"); // todo fix path
        NetworkPlugin.InitializeNetworkAgentLog();
        NetworkPlugin.InitCallbacks();
        NetworkPlugin.SetCountryCode("US"); // todo dont hard code this

        NetworkPlugin.Start();
        NetworkPlugin.ConnectServer();

        // kinda pointless but it works
        //networkPlugin.StartSSDPDiscovery();
        //networkPlugin.StopSSDPDiscovery();

        NetworkPlugin.ConnectPrinter(Config.DeviceId, Config.IpAddress, Config.Username, Config.Password);

        try
        {
            Log.Debug("User ID: {UserId}", NetworkPlugin.UserId);
            Log.Debug("User Name: {UserName}", NetworkPlugin.UserName);
            Log.Debug("User Nickname: {UserNickName}", NetworkPlugin.UserNickname);
            Log.Debug("User Avatar: {UserAvatar}", NetworkPlugin.UserAvatar);
            Log.Debug("User Logged in: {IsUserLoggedIn}", NetworkPlugin.IsUserLoggedIn);
            Log.Debug("Cloud Server Connected: {IsServerConnected}", NetworkPlugin.IsServerConnected);
            Log.Debug("Selected Machine: {SelectedMachineDeviceId}", NetworkPlugin.SelectedMachineDeviceId);

            Log.Information("Waiting for cloud connection ...");

            while (!NetworkPlugin.IsServerConnected)
            {
                Thread.Sleep(1000);
                Log.Debug("Waiting ...");
            }

            NetworkPlugin.RefreshConnection();
            // networkPlugin.Subscribe();

            if (!NetworkPlugin.LanMode)
                NetworkPlugin.RefreshCameraUrl();

            InputLoop();
        }
        catch (Exception ex)
        {
            // catch and print the exception
            Log.Error(ex, "Unhandled exception");
        }
        finally
        {
            // dispose so that the log is flushed and we can read network plugin details later
            NetworkPlugin.Dispose();
        }
    }

    static void InputLoop()
    {
        while (true)
        {
            try
            {
                Console.Write("> ");
                var gcode = Console.ReadLine();

                switch (gcode)
                {
                    case "wipe":
                        Log.Warning("Sending wipe nozzle sequence as copied from Bambu Studio source.. do this at your own risk!");
                        NetworkPlugin.WipeNozzle();
                        break;

                    case "send-sd":
                        Log.Information("Testing send gcode");
                        File.WriteAllText("c:\\temp\\test.gcode", "G28\n");

                        //networkPlugin.SendGcodeToSdCard("c:\\temp\\test.gcode", "test.gcode", config.DeviceId, config.IpAddress, config.Username, config.Password, 0, "project name", "task name", "preset name", "config file name", "", "", false, false, false, false, false, false, "lan");
                        //networkPlugin.SendGcodeToSdCard("c:\\temp\\test.gcode", "test.gcode", config.DeviceId, config.IpAddress, config.Username, config.Password, 0, "project_name.gcode", "taskname", "Generic PETG", "configfilename", "F(01) -> A(02)", "F(01) -> A(02)", true, false, false, true, false, false, "lan");
                        //networkPlugin.SendGcodeToSdCard("c:\\temp\\test.gcode", "test1234.gcode", config.DeviceId, config.IpAddress, config.Username, config.Password, 0, "project_name", "", "", "", "", "", false, false, false, false, false, false, "lan");

                        NetworkPlugin.SendGcodeToSdCard("f:\\temp2\\nut.gcode", "test.gcode", Config.DeviceId, Config.IpAddress, Config.Username, Config.Password, 0, "nut2.gcode", "taskname", "Generic PETG", "configfilename", "F(01) -> A(02)", "F(01) -> A(02)", true, false, false, true, false, false, "lan");

                        break;

                    case "q":
                    case "quit":
                    case "exit":
                        return;

                    default:
                        NetworkPlugin.SendGcode(gcode + "\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                // catch and print the exception
                Log.Error(ex, "Something went wrong ...");
            }
        }
    }
}