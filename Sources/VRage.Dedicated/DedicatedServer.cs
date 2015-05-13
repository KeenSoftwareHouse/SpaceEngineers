using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Dedicated
{
    public static class DedicatedServer
    {
        static RegistryKey TryOpenKey(this RegistryKey currentKey, string subkey)
        {
            if (currentKey != null)
            {
                return currentKey.OpenSubKey(subkey);
            }
            return null;
        }

        public static bool AddDateToLog
        {
            get
            {
                try
                {
                    RegistryKey baseKey = Registry.LocalMachine.TryOpenKey("Software");
                    RegistryKey key = (Environment.Is64BitProcess)
                        ? baseKey.TryOpenKey("Wow6432Node").TryOpenKey("KeenSoftwareHouse").TryOpenKey("MedievalEngineersDedicatedServer")
                        : baseKey.TryOpenKey("KeenSoftwareHouse").TryOpenKey("MedievalEngineersDedicatedServer");

                    if (key != null && key.GetValue("AddDateToLog") != null)
                        return Convert.ToBoolean(key.GetValue("AddDateToLog"));
                    else
                        return false;
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
                using (var is64b = (Environment.Is64BitProcess) ? key.OpenSubKey("Wow6432Node", true) : key)
                using (var subKey = is64b.CreateSubKey("KeenSoftwareHouse", RegistryKeyPermissionCheck.ReadWriteSubTree))
                using (var subSubKey = subKey.CreateSubKey("MedievalEngineersDedicatedServer", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    subSubKey.SetValue("AddDateToLog", value);
                }
            }
        }

        public static bool SendLogToKeen
        {
            get
            {
                try
                {
                    RegistryKey baseKey = Registry.LocalMachine.TryOpenKey("Software");
                    RegistryKey key = (Environment.Is64BitProcess)
                        ? baseKey.TryOpenKey("Wow6432Node").TryOpenKey("KeenSoftwareHouse").TryOpenKey("MedievalEngineersDedicatedServer")
                        : baseKey.TryOpenKey("KeenSoftwareHouse").TryOpenKey("MedievalEngineersDedicatedServer");

                    if (key != null && key.GetValue("SendLogToKeen") != null)
                        return Convert.ToBoolean(key.GetValue("SendLogToKeen"));
                    else
                        return true;
                }
                catch
                {
                    return true;
                }
            }
            set
            {
                using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
                using (var is64b = (Environment.Is64BitProcess) ? key.OpenSubKey("Wow6432Node", true) : key)
                using (var subKey = is64b.CreateSubKey("KeenSoftwareHouse", RegistryKeyPermissionCheck.ReadWriteSubTree))
                using (var subSubKey = subKey.CreateSubKey("MedievalEngineersDedicatedServer", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    subSubKey.SetValue("SendLogToKeen", value);
                }
            }
        }
    }
}
