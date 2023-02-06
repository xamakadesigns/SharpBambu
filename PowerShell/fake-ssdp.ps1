# Use Bambu Studio with your X1 or X1C in LAN mode over a routed network
# Printing is possible without the printer contacting the Internet as long as the printer has communicated with Bambu servers at some time in the past
#
# Script below will send SSDP advertisements to any local IP address it finds listening on UDP port 2021
# The purpose is to advertise your printer to Bambu Studio when located on a separate routable subnet (ie. vlan) with fake SSDP packets
# Bambu Studio will see the advertisement packet every 5 seconds, and the printer should show up and allow you to connect to it
#
param (
    [parameter(Mandatory)]$printerIp,  # ip from wifi screen
    [parameter(Mandatory)]$deviceId,   # length 15 uppercase and numeric 0-9
    [parameter(Mandatory)]$deviceName, # in the format of 3DP-###-###
    $printerType = "3DPrinter-X1-Carbon"
)

function Send-Udp($IP, $Port, $Message)
{
    $ByteArray = $([System.Text.Encoding]::ASCII).GetBytes($Message)
    $UdpClient = New-Object System.Net.Sockets.UdpClient($IP, $Port)
    $UdpClient.Send($ByteArray, $ByteArray.Length) | Out-Null
    $UdpClient.Close()
} 

$message = @"
NOTIFY * HTTP/1.1
Host: 239.255.255.250:1990
Server: Buildroot/2018.02-rc3 UPnP/1.0 ssdpd/1.8
Location: $printerIp
NT: urn:bambulab-com:device:3dprinter:1
NTS: ssdp:alive
USN: $deviceId
Cache-Control: max-age=1800
DevModel.bambu.com: $printerType
DevName.bambu.com: $deviceName
DevSignal.bambu.com: -42
DevConnect.bambu.com: lan
DevBind.bambu.com: free


"@ -replace "`n", "`r`n"

while ($TRUE)
{
    foreach($ipAddress in (Get-NetUDPEndpoint -LocalPort 2021).LocalAddress)
    {
        Write-Host "Sending SSDP packet to ${ipAddress}:2021"
        Send-Udp -IP $ipAddress -Port 2021 -Message $message
    }

    Start-Sleep -Seconds 5
}

# Example usage:
# .\fake-ssdp.ps1 -printerIp "192.168.X.Y" -deviceId "123456789012345" -deviceName "3DP-123-456" -printerType "3DPrinter-X1-Carbon"