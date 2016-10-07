#if !XB1
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace VRage.Service
{
    internal static class NativeMethods
    {

#if !UNSHARPER

        public static readonly string DATABASE_ACTIVE = "ServicesActive";
        public static readonly string DATABASE_FAILED = "ServicesFailed";
        public const int MAX_COMPUTERNAME_LENGTH = 31;
        public const int WM_POWERBROADCAST = 536;
        public const int NO_ERROR = 0;
        public const int BROADCAST_QUERY_DENY = 1112363332;
        public const int PBT_APMBATTERYLOW = 9;
        public const int PBT_APMOEMEVENT = 11;
        public const int PBT_APMPOWERSTATUSCHANGE = 10;
        public const int PBT_APMQUERYSUSPEND = 0;
        public const int PBT_APMQUERYSUSPENDFAILED = 2;
        public const int PBT_APMRESUMEAUTOMATIC = 18;
        public const int PBT_APMRESUMECRITICAL = 6;
        public const int PBT_APMRESUMESUSPEND = 7;
        public const int PBT_APMSUSPEND = 4;
        public const int ERROR_MORE_DATA = 234;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int MB_OK = 0;
        public const int MB_OKCANCEL = 1;
        public const int MB_ABORTRETRYIGNORE = 2;
        public const int MB_YESNOCANCEL = 3;
        public const int MB_YESNO = 4;
        public const int MB_RETRYCANCEL = 5;
        public const int MB_ICONHAND = 16;
        public const int MB_ICONQUESTION = 32;
        public const int MB_ICONEXCLAMATION = 48;
        public const int MB_ICONASTERISK = 64;
        public const int MB_USERICON = 128;
        public const int MB_ICONWARNING = 48;
        public const int MB_ICONERROR = 16;
        public const int MB_ICONINFORMATION = 64;
        public const int MB_DEFBUTTON1 = 0;
        public const int MB_DEFBUTTON2 = 256;
        public const int MB_DEFBUTTON3 = 512;
        public const int MB_DEFBUTTON4 = 768;
        public const int MB_APPLMODAL = 0;
        public const int MB_SYSTEMMODAL = 4096;
        public const int MB_TASKMODAL = 8192;
        public const int MB_HELP = 16384;
        public const int MB_NOFOCUS = 32768;
        public const int MB_SETFOREGROUND = 65536;
        public const int MB_DEFAULT_DESKTOP_ONLY = 131072;
        public const int MB_TOPMOST = 262144;
        public const int MB_RIGHT = 524288;
        public const int MB_RTLREADING = 1048576;
        public const int MB_SERVICE_NOTIFICATION = 2097152;
        public const int MB_SERVICE_NOTIFICATION_NT3X = 262144;
        public const int MB_TYPEMASK = 15;
        public const int MB_ICONMASK = 240;
        public const int MB_DEFMASK = 3840;
        public const int MB_MODEMASK = 12288;
        public const int MB_MISCMASK = 49152;
        public const int STANDARD_RIGHTS_DELETE = 65536;
        public const int STANDARD_RIGHTS_REQUIRED = 983040;
        public const int SERVICE_NO_CHANGE = -1;
        public const int ACCESS_TYPE_CHANGE_CONFIG = 2;
        public const int ACCESS_TYPE_ENUMERATE_DEPENDENTS = 8;
        public const int ACCESS_TYPE_INTERROGATE = 128;
        public const int ACCESS_TYPE_PAUSE_CONTINUE = 64;
        public const int ACCESS_TYPE_QUERY_CONFIG = 1;
        public const int ACCESS_TYPE_QUERY_STATUS = 4;
        public const int ACCESS_TYPE_START = 16;
        public const int ACCESS_TYPE_STOP = 32;
        public const int ACCESS_TYPE_USER_DEFINED_CONTROL = 256;
        public const int ACCESS_TYPE_ALL = 983551;
        public const int ACCEPT_NETBINDCHANGE = 16;
        public const int ACCEPT_PAUSE_CONTINUE = 2;
        public const int ACCEPT_PARAMCHANGE = 8;
        public const int ACCEPT_POWEREVENT = 64;
        public const int ACCEPT_SHUTDOWN = 4;
        public const int ACCEPT_STOP = 1;
        public const int ACCEPT_SESSIONCHANGE = 128;
        public const int CONTROL_CONTINUE = 3;
        public const int CONTROL_INTERROGATE = 4;
        public const int CONTROL_NETBINDADD = 7;
        public const int CONTROL_NETBINDDISABLE = 10;
        public const int CONTROL_NETBINDENABLE = 9;
        public const int CONTROL_NETBINDREMOVE = 8;
        public const int CONTROL_PARAMCHANGE = 6;
        public const int CONTROL_PAUSE = 2;
        public const int CONTROL_POWEREVENT = 13;
        public const int CONTROL_SHUTDOWN = 5;
        public const int CONTROL_STOP = 1;
        public const int CONTROL_DEVICEEVENT = 11;
        public const int CONTROL_SESSIONCHANGE = 14;
        public const int SERVICE_CONFIG_DESCRIPTION = 1;
        public const int SERVICE_CONFIG_FAILURE_ACTIONS = 2;
        public const int SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;
        public const int ERROR_CONTROL_CRITICAL = 3;
        public const int ERROR_CONTROL_IGNORE = 0;
        public const int ERROR_CONTROL_NORMAL = 1;
        public const int ERROR_CONTROL_SEVERE = 2;
        public const int SC_MANAGER_CONNECT = 1;
        public const int SC_MANAGER_CREATE_SERVICE = 2;
        public const int SC_MANAGER_ENUMERATE_SERVICE = 4;
        public const int SC_MANAGER_LOCK = 8;
        public const int SC_MANAGER_MODIFY_BOOT_CONFIG = 32;
        public const int SC_MANAGER_QUERY_LOCK_STATUS = 16;
        public const int SC_MANAGER_ALL = 983103;
        public const int SC_ENUM_PROCESS_INFO = 0;
        public const int SERVICE_QUERY_CONFIG = 1;
        public const int SERVICE_CHANGE_CONFIG = 2;
        public const int SERVICE_QUERY_STATUS = 4;
        public const int SERVICE_ENUMERATE_DEPENDENTS = 8;
        public const int SERVICE_START = 16;
        public const int SERVICE_STOP = 32;
        public const int SERVICE_PAUSE_CONTINUE = 64;
        public const int SERVICE_INTERROGATE = 128;
        public const int SERVICE_USER_DEFINED_CONTROL = 256;
        public const int SERVICE_ALL_ACCESS = 983551;
        public const int SERVICE_TYPE_ADAPTER = 4;
        public const int SERVICE_TYPE_FILE_SYSTEM_DRIVER = 2;
        public const int SERVICE_TYPE_INTERACTIVE_PROCESS = 256;
        public const int SERVICE_TYPE_KERNEL_DRIVER = 1;
        public const int SERVICE_TYPE_RECOGNIZER_DRIVER = 8;
        public const int SERVICE_TYPE_WIN32_OWN_PROCESS = 16;
        public const int SERVICE_TYPE_WIN32_SHARE_PROCESS = 32;
        public const int SERVICE_TYPE_WIN32 = 48;
        public const int SERVICE_TYPE_DRIVER = 11;
        public const int SERVICE_TYPE_ALL = 319;
        public const int START_TYPE_AUTO = 2;
        public const int START_TYPE_BOOT = 0;
        public const int START_TYPE_DEMAND = 3;
        public const int START_TYPE_DISABLED = 4;
        public const int START_TYPE_SYSTEM = 1;
        public const int SERVICE_ACTIVE = 1;
        public const int SERVICE_INACTIVE = 2;
        public const int SERVICE_STATE_ALL = 3;
        public const int STATE_CONTINUE_PENDING = 5;
        public const int STATE_PAUSED = 7;
        public const int STATE_PAUSE_PENDING = 6;
        public const int STATE_RUNNING = 4;
        public const int STATE_START_PENDING = 2;
        public const int STATE_STOPPED = 1;
        public const int STATE_STOP_PENDING = 3;
        public const int STATUS_ACTIVE = 1;
        public const int STATUS_INACTIVE = 2;
        public const int STATUS_ALL = 3;
        public const int POLICY_VIEW_LOCAL_INFORMATION = 1;
        public const int POLICY_VIEW_AUDIT_INFORMATION = 2;
        public const int POLICY_GET_PRIVATE_INFORMATION = 4;
        public const int POLICY_TRUST_ADMIN = 8;
        public const int POLICY_CREATE_ACCOUNT = 16;
        public const int POLICY_CREATE_SECRET = 32;
        public const int POLICY_CREATE_PRIVILEGE = 64;
        public const int POLICY_SET_DEFAULT_QUOTA_LIMITS = 128;
        public const int POLICY_SET_AUDIT_REQUIREMENTS = 256;
        public const int POLICY_AUDIT_LOG_ADMIN = 512;
        public const int POLICY_SERVER_ADMIN = 1024;
        public const int POLICY_LOOKUP_NAMES = 2048;
        public const int POLICY_ALL_ACCESS = 985087;
        public const int STATUS_OBJECT_NAME_NOT_FOUND = -1073741772;
        public const int WTS_CONSOLE_CONNECT = 1;
        public const int WTS_CONSOLE_DISCONNECT = 2;
        public const int WTS_REMOTE_CONNECT = 3;
        public const int WTS_REMOTE_DISCONNECT = 4;
        public const int WTS_SESSION_LOGON = 5;
        public const int WTS_SESSION_LOGOFF = 6;
        public const int WTS_SESSION_LOCK = 7;
        public const int WTS_SESSION_UNLOCK = 8;
        public const int WTS_SESSION_REMOTE_CONTROL = 9;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenService(IntPtr databaseHandle, string serviceName, int access);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandler(string serviceName, Delegate callback);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(string serviceName, Delegate callback, IntPtr userData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool SetServiceStatus(IntPtr serviceStatusHandle, NativeMethods.SERVICE_STATUS* status);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool StartServiceCtrlDispatcher(IntPtr entry);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateService(IntPtr databaseHandle, string serviceName, string displayName, int access, int serviceType, int startType, int errorControl, string binaryPath, string loadOrderGroup, IntPtr pTagId, string dependencies, string servicesStartName, string password);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteService(IntPtr serviceHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaOpenPolicy(NativeMethods.LSA_UNICODE_STRING systemName, IntPtr pointerObjectAttributes, int desiredAccess, out IntPtr pointerPolicyHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaAddAccountRights(IntPtr policyHandle, byte[] accountSid, NativeMethods.LSA_UNICODE_STRING userRights, int countOfRights);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaRemoveAccountRights(IntPtr policyHandle, byte[] accountSid, bool allRights, NativeMethods.LSA_UNICODE_STRING userRights, int countOfRights);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int LsaEnumerateAccountRights(IntPtr policyHandle, byte[] accountSid, out IntPtr pLsaUnicodeStringUserRights, out int RightsCount);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool LookupAccountName(string systemName, string accountName, byte[] sid, int[] sidLen, char[] refDomainName, int[] domNameLen, [In, Out] int[] sidNameUse);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetComputerName(StringBuilder lpBuffer, ref int nSize);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ChangeServiceConfig2(IntPtr serviceHandle, uint infoLevel, ref NativeMethods.SERVICE_DESCRIPTION serviceDesc);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ChangeServiceConfig2(IntPtr serviceHandle, uint infoLevel, ref NativeMethods.SERVICE_DELAYED_AUTOSTART_INFO serviceDesc);

        public struct SERVICE_STATUS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        public delegate void ServiceMainCallback(int argCount, IntPtr argPointer);

        public delegate void ServiceControlCallback(int control);

        public delegate int ServiceControlCallbackEx(int control, int eventType, IntPtr eventData, IntPtr eventContext);

        public static SessionChangeDescription CreateSessionChangeDescription(SessionChangeReason reason, int id)
        {
            SessionChangeDescriptionUnion u;
            u.Desc = new SessionChangeDescription();
            u.DescCopy = new SessionChangeDescriptionCopy() { _id = id, _reason = reason };
            return u.Desc;
        }

        private struct SessionChangeDescriptionCopy
        {
            public SessionChangeReason _reason;
            public int _id;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct SessionChangeDescriptionUnion
        {
            [FieldOffset(0)]
            public SessionChangeDescription Desc;

            [FieldOffset(0)]
            public SessionChangeDescriptionCopy DescCopy;
        }

        [ComVisible(false)]
        public enum StructFormat
        {
            Ansi = 1,
            Unicode = 2,
            Auto = 3,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class ENUM_SERVICE_STATUS
        {
            public string serviceName;
            public string displayName;
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class ENUM_SERVICE_STATUS_PROCESS
        {
            public string serviceName;
            public string displayName;
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
            public int processID;
            public int serviceFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
		[Unsharper.UnsharperDisableReflection()]
        public class QUERY_SERVICE_CONFIG
        {
            public int dwServiceType;
            public int dwStartType;
            public int dwErrorControl;
            public unsafe char* lpBinaryPathName;
            public unsafe char* lpLoadOrderGroup;
            public int dwTagId;
            public unsafe char* lpDependencies;
            public unsafe char* lpServiceStartName;
            public unsafe char* lpDisplayName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SERVICE_TABLE_ENTRY
        {
            public IntPtr name;
            public Delegate callback;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class LSA_UNICODE_STRING
        {
            public short length;
            public short maximumLength;
            public string buffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class LSA_UNICODE_STRING_withPointer
        {
            public IntPtr pwstr = (IntPtr)0;
            public short length;
            public short maximumLength;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class LSA_OBJECT_ATTRIBUTES
        {
            public IntPtr rootDirectory = (IntPtr)0;
            public IntPtr pointerLsaString = (IntPtr)0;
            public IntPtr pointerSecurityDescriptor = (IntPtr)0;
            public IntPtr pointerSecurityQualityOfService = (IntPtr)0;
            public int length;
            public int attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_DESCRIPTION
        {
            public IntPtr description;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_DELAYED_AUTOSTART_INFO
        {
            public bool fDelayedAutostart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		[Unsharper.UnsharperDisableReflection()]
        public struct SERVICE_FAILURE_ACTIONS
        {
            public uint dwResetPeriod;
            public IntPtr rebootMsg;
            public IntPtr command;
            public uint numActions;
            public unsafe NativeMethods.SC_ACTION* actions;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SC_ACTION
        {
            public int type;
            public uint delay;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class WTSSESSION_NOTIFICATION
        {
            public int size;
            public int sessionId;
        }

#endif

    }
}
#endif // !XB1
