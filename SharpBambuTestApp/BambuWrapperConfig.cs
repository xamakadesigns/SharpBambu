using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambuTestApp
{
    internal class BambuWrapperConfig
    {
        internal BambuWrapperConfig Load()
        {
            var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.secret.json", optional: true)
                    .Build()
                    .AsEnumerable()
                    .ToDictionary(e => e.Key, e => e.Value);

            Username = config["network:username"] ?? "";
            Password = config["network:password"] ?? "";
            IpAddress = config["network:ipAddress"] ?? "";
            DeviceId = config["network:deviceId"] ?? "";

            var result = bool.TryParse(config["network:autoUpdateDll"], out bool autoUpdateDll);
            AutoUpdateDll = autoUpdateDll;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(IpAddress))
                throw new Exception("Please create a copy of 'appsttings.json' and name it 'appsettings.secret.json'. " +
                    "Enter the IP & password for your printer from the Lan mode screen.");

            return this;
        }

        public string Username { get; private set; } = "";
        public string Password { get; private set; } = "";
        public string IpAddress { get; private set; } = "";
        public string DeviceId { get; private set; } = "";
        public bool AutoUpdateDll { get; private set; }
    }
}
