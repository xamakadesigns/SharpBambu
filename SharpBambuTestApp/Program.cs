// https://blog.elmah.io/debugging-system-accessviolationexception/
// https://limbioliong.wordpress.com/2011/06/16/returning-strings-from-a-c-api/
// See https://aka.ms/new-console-template for more information
using Test;


var networkPlugin = new BambuNetworkPlugin();

Console.WriteLine("Initialize Bambu Network Plugin");

networkPlugin.InitializeNetworkPlugin();

Console.WriteLine($"Version {networkPlugin.GetNetworkPluginVersion()}");

//networkPlugin.SetConfigFolder(networkPlugin.BambuNetworkPluginConfigFolder);
// m_agent->set_cert_file(resources_dir() + "/cert", "slicer_base64.cer");


if (networkPlugin.InitializeNetworkAgentLog() == 0)
{
    Console.WriteLine($"Bambu Network Plugin log initialized; should be found at location: {networkPlugin.BambuNetworkPluginLogFolder}");
    Console.WriteLine($"Bambu Network Plugin BambuNetworkEngine.conf file should be located at: {networkPlugin.BambuNetworkPluginConfigFolder}");
}
else
{
    throw new Exception($"Unable to initialize the Bambu Network Plugin log file; Check permissions for {networkPlugin.BambuNetworkPluginLogFolder}");
}

// init_networking_callbacks
//m_agent->set_country_code(country_code);

networkPlugin.Start();

//networkPlugin.ConnectServer();

networkPlugin.SetConfigFolder(networkPlugin.ApplicationFolder);

try
{
    //Console.WriteLine($"User ID: {networkPlugin.GetUserId()}");
    //Console.WriteLine($"User Name: {networkPlugin.GetUserName()}");
    //Console.WriteLine($"User Nickname: {networkPlugin.GetUserNickname()}");
    //Console.WriteLine($"User Avatar: {networkPlugin.GetUserAvatar()}");
    //Console.WriteLine($"Build Login CMD: {networkPlugin.BuildLoginCmd()}");

    networkPlugin.DisconnectPrinter();
}
catch(Exception ex)
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

