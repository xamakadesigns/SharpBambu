// https://blog.elmah.io/debugging-system-accessviolationexception/
// https://limbioliong.wordpress.com/2011/06/16/returning-strings-from-a-c-api/
// See https://aka.ms/new-console-template for more information
using Test;


var networkPlugin = new BambuNetworkPlugin();

Console.WriteLine("Initialize Bambu Network Plugin");

networkPlugin.InitializeNetworkPlugin();

Console.WriteLine($"Version {networkPlugin.NetworkPluginVersion}");

networkPlugin.SetConfigFolder(networkPlugin.BambuNetworkPluginConfigFolder);
networkPlugin.SetCertFile("C:\\Program Files\\Bambu Studio\\resources\\cert", "slicer_base64.cer"); // todo fix path
networkPlugin.InitializeNetworkAgentLog();
networkPlugin.InitCallbacks();
networkPlugin.SetCountryCode("US"); // todo dont hard code this

networkPlugin.Start();
networkPlugin.ConnectServer();

try
{
    //Console.Clear();
    //Console.WriteLine($"User ID: {networkPlugin.UserId}");
    //Console.WriteLine($"User Name: {networkPlugin.UserName}");
    //Console.WriteLine($"User Nickname: {networkPlugin.UserNickname}");
    Console.WriteLine($"User Avatar: {networkPlugin.UserAvatar}");
    Console.WriteLine($"User Logged in: {networkPlugin.IsUserLoggedIn}");
    Console.WriteLine($"Server Connected: {networkPlugin.IsServerConnected}");
    //Console.WriteLine($"Selected Machine: {networkPlugin.CurrentMachineDeviceId}");

    while(!networkPlugin.IsServerConnected)
    {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting for cloud connection ...");
    }

    networkPlugin.Subscribe();
    Console.WriteLine($"Server Connected: {networkPlugin.IsServerConnected}");

    Console.WriteLine("Setting up MQTT connection ...");

    networkPlugin.RefreshConnection();

    while (true)
    {
        Console.Write("> ");
        var gcode = Console.ReadLine();

        if (string.IsNullOrEmpty(gcode))
            break;

        switch (gcode)
        {
            case "wipe":
                Console.WriteLine("Sending wipe nozzle sequence as copied from Bambu Studio source.. do this at your own risk!");
                networkPlugin.WipeNozzle();
                break;
            default:
                networkPlugin.SendGcode(gcode + "\n");
                break;
        }
    }
}
catch (Exception ex)
{
    // catch and print the exception
    Console.WriteLine("Exception:");
    Console.WriteLine(ex);
}
finally
{
    Console.WriteLine("Disposing Bambu Network Plugin");

    // dispose so that the log is flushed and we can read network plugin details later
    networkPlugin.Dispose();
}

