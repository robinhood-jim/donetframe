using Frameset.Core.Utils;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

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
        public static string GetCpuSerial()
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
                if (string.Equals("To be filled by O.E.M.", serialNo, StringComparison.OrdinalIgnoreCase))
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


    }
}
