using Frameset.Core.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Frameset.Core.Hardware
{
    public static class MachineUtils
    {
        private static readonly string DUBS_PATH = "/var/lib/dbus/machine-id";
        private static readonly string DUBS_PATH_EXT = "/etc/machine-id";
        private static readonly string[] EXEC_DARWIN = { "ioreg", "-rd1", "-c", "IOPlatformExpertDevice" };
        private static readonly string NOTSPEICIFY = "Not Specified";
        public static string GetMachineId()
        {
            string machineGuid = null;
            if (IsRunningOnWindows())
            {
                RegistryKey registry = Registry.LocalMachine;
                RegistryKey software = registry.OpenSubKey("Software");
                object machineGuidObj = software.OpenSubKey("Microsoft").OpenSubKey("Cryptography").GetValue("MachineGuid");
                if (machineGuidObj != null)
                {
                    machineGuid = machineGuidObj.ToString();
                }
            }
            else if (IsRunningOnLinux())
            {
                string readFile = DUBS_PATH;
                if (!File.Exists(DUBS_PATH))
                {
                    readFile = DUBS_PATH_EXT;
                }
                using (var stream = new StreamReader(File.OpenRead(readFile)))
                {
                    machineGuid = stream.ReadLine();
                }
            }
            else if (IsRunningOnMacOs())
            {
                string output = CommandExecutor.ExcuteCommand(EXEC_DARWIN);
                using (var reader = new StringReader(output))
                {
                    string lineStr = null;
                    while ((lineStr = reader.ReadLine()) != null)
                    {
                        int pos = lineStr.IndexOf("IOPlatformUUID");
                        if (pos != -1)
                        {
                            pos = lineStr.IndexOf("\" = \"");
                            machineGuid = lineStr.Substring(pos + 5, lineStr.Length - pos - 5).Trim();
                        }
                    }
                }
            }
            return machineGuid;
        }
        public static string GetCPUSerial()
        {
            string serialNo = null;
            if (IsRunningOnWindows())
            {
                serialNo = CommandExecutor.ExecuteCommandReturnAfterRow(["powershell.exe", "Get-WmiObject", "-Class", "Win32_Processor", "|", "Select-Object", "ProcessorId"], 2);
            }
            else if (IsRunningOnLinux())
            {
                serialNo = CommandExecutor.ExecuteCommandMeetSpecifyKey(["dmidecode", "-t", "4", "|", "grep", "\"ID\""], "ID:");
                if (string.Equals(NOTSPEICIFY, serialNo, StringComparison.OrdinalIgnoreCase))
                {
                    serialNo = string.Empty;
                }
            }
            else if (IsRunningOnMacOs())
            {
                serialNo = CommandExecutor.ExecuteCommandMeetSpecifyKey(["system_profiler", "SPHardwareDataType"], "Serial Number (system):");
                if (string.Equals(NOTSPEICIFY, serialNo, StringComparison.OrdinalIgnoreCase))
                {
                    serialNo = string.Empty;
                }
            }
            return serialNo;
        }
        public static string GetSystemSerial()
        {
            string serialNo = null;
            if (IsRunningOnWindows())
            {
                serialNo = CommandExecutor.ExecuteCommandReturnAfterRow(["powershell.exe", "Get-WmiObject", "-class", "win32_bios", "|", "Select-Object", "SerialNumber"], 2);
            }
            else if (IsRunningOnLinux())
            {
                serialNo = CommandExecutor.ExecuteCommandMeetSpecifyKey(["sudo", "dmidecode", "-t", "system"], "Serial Number:");
                if (string.IsNullOrWhiteSpace(serialNo) || string.Equals("Not Specified", serialNo, StringComparison.OrdinalIgnoreCase))
                {
                    serialNo = CommandExecutor.ExecuteCommandMeetSpecifyKey(["sudo", "dmidecode", "-t", "system"], "UUID:");
                }
            }
            else if (IsRunningOnMacOs())
            {
                serialNo = CommandExecutor.ExecuteCommandMeetSpecifyKey(["system_profiler", "SPHardwareDataType"], "Serial Number (system):");
            }
            if (string.Equals("To be filled by O.E.M.", serialNo, StringComparison.OrdinalIgnoreCase))
            {
                serialNo = string.Empty;
            }
            return serialNo;
        }
        public static string GetStorageSerial()
        {
            string serialNo = string.Empty;
            if (IsRunningOnWindows())
            {
                serialNo = CommandExecutor.ExecuteCommandReturnAfterRow(["powershell.exe", "Get-WmiObject", "-Class", "Win32_DiskDrive", "|", "Select-Object", "SerialNumber,DeviceId"], 2);
                using StringReader reader = new StringReader(serialNo);
                string lineStr = null;
                while ((lineStr = reader.ReadLine()) != null)
                {
                    string[] arr = lineStr.Split(' ');
                    serialNo = arr[0].Trim();
                }
            }
            else if (IsRunningOnLinux())
            {
                serialNo = CommandExecutor.ExecuteCommandReturnAfterRow(["bash", "-c", "sudo lsblk -o SERIAL"], 1);
            }
            else
            {
                serialNo = CommandExecutor.ExecuteCommandMeetSpecifyKey(["bash", "-c", "sudo system_profiler SPStorageDataType"], "Volume UUID");
            }
            return serialNo;
        }
        public static bool IsRunningOnWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
        public static bool IsRunningOnLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }
        public static bool IsRunningOnMacOs()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }
        public static bool IsRunningOnFreeBSD()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        }
        public static string GetSystemTag()
        {
            StringBuilder builder = new StringBuilder();

            String machineId = GetMachineId();
            if (!string.IsNullOrWhiteSpace(machineId))
            {
                builder.Append("MID_" + machineId);
            }
            String systemSerial = GetCPUSerial();
            if (!string.IsNullOrWhiteSpace(machineId))
            {
                builder.Append("_CPU_" + systemSerial);
            }
            String hardDsSerial = GetStorageSerial();
            if (!string.IsNullOrWhiteSpace(machineId))
            {
                builder.Append("_DISK_" + hardDsSerial);
            }
            builder.Append("_SYS_" + GetOsName());
            return builder.ToString();
        }
        public static string GetOsName()
        {
            PlatformID platformID = Environment.OSVersion.Platform;
            return platformID.ToString();
        }



        public static string GetRealLocalIP()
        {
            var allInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var candidates = new List<(IPAddress Ip, int Score)>();
            foreach (var ni in allInterfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                string desc = ni.Description.ToLower();
                string name = ni.Name.ToLower();
                if (desc.Contains("wintun") ||
                    desc.Contains("clash") ||
                    desc.Contains("virtual") ||
                    desc.Contains("vmware") ||
                    desc.Contains("vbox") ||
                    desc.Contains("hyper-v") ||
                    desc.Contains("zerotier") ||
                    desc.Contains("tailscale") ||
                    desc.Contains("wireguard") ||
                    desc.Contains("docker") ||
                    name.Contains("vethernet") ||
                    name.Contains("wsl"))
                    continue;

                var ipProps = ni.GetIPProperties();
                var ipv4Addrs = ipProps.UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                foreach (var addr in ipv4Addrs)
                {
                    string ipStr = addr.Address.ToString();
                    if (ipStr.StartsWith("169.254")) continue;
                    if (ipStr.StartsWith("198.18.")) continue;
                    if (ipStr.StartsWith("172."))
                    {
                        var parts = ipStr.Split('.');
                        if (parts.Length == 4 && int.TryParse(parts[1], out int secondOctet))
                        {
                            if (secondOctet >= 16 && secondOctet <= 31) continue;
                        }
                    }

                    int score = 0;
                    if (ipProps.GatewayAddresses.Any(g => !g.Address.ToString().Equals("0.0.0.0")))
                        score += 100;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        score += 50;
                    if (desc.Contains("intel") || desc.Contains("realtek") || desc.Contains("atheros") || desc.Contains("broadcom"))
                        score += 30;

                    candidates.Add((addr.Address, score));
                }
            }

            if (candidates.Count == 0)
                return "192.168.1.100";

            return candidates.OrderByDescending(c => c.Score).First().Ip.ToString();
        }


    }
}
