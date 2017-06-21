#if !XB1
// Type: System.ServiceProcess.ServiceBase
// Assembly: System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.ServiceProcess.dll

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Threading;

namespace VRage.Service
{
    [InstallerType(typeof(ServiceProcessInstaller))]
    public class MyServiceBase : Component
    {
        private NativeMethods.SERVICE_STATUS status = new NativeMethods.SERVICE_STATUS();
        public const int MaxNameLength = 80;
        private IntPtr statusHandle;
        private NativeMethods.ServiceControlCallback commandCallback;
        private NativeMethods.ServiceControlCallbackEx commandCallbackEx;
        private NativeMethods.ServiceMainCallback mainCallback;
        private IntPtr handleName;
        private ManualResetEvent startCompletedSignal;
        private int acceptedCommands;
        private bool autoLog;
        private string serviceName;
        private EventLog eventLog;
        private bool nameFrozen;
        private bool commandPropsFrozen;
        private bool disposed;
        private bool initialized;
        private bool isServiceHosted;

        public string UsedServiceName { get; private set; }

        [DefaultValue(true)]
        [ServiceProcessDescription("SBAutoLog")]
        public bool AutoLog
        {
            get
            {
                return this.autoLog;
            }
            set
            {
                this.autoLog = value;
            }
        }

        [ComVisible(false)]
        public int ExitCode
        {
            get
            {
                return this.status.win32ExitCode;
            }
            set
            {
                this.status.win32ExitCode = value;
            }
        }

        [DefaultValue(false)]
        public bool CanHandlePowerEvent
        {
            get
            {
                return (this.acceptedCommands & 64) != 0;
            }
            set
            {
                if (this.commandPropsFrozen)
                    throw new InvalidOperationException(GetString("CannotChangeProperties"));
                if (value)
                    this.acceptedCommands |= 64;
                else
                    this.acceptedCommands &= -65;
            }
        }

        [ComVisible(false)]
        [DefaultValue(false)]
        public bool CanHandleSessionChangeEvent
        {
            get
            {
                return (this.acceptedCommands & 128) != 0;
            }
            set
            {
                if (this.commandPropsFrozen)
                    throw new InvalidOperationException(GetString("CannotChangeProperties"));
                if (value)
                    this.acceptedCommands |= 128;
                else
                    this.acceptedCommands &= -129;
            }
        }

        [DefaultValue(false)]
        public bool CanPauseAndContinue
        {
            get
            {
                return (this.acceptedCommands & 2) != 0;
            }
            set
            {
                if (this.commandPropsFrozen)
                    throw new InvalidOperationException(GetString("CannotChangeProperties"));
                if (value)
                    this.acceptedCommands |= 2;
                else
                    this.acceptedCommands &= -3;
            }
        }

        [DefaultValue(false)]
        public bool CanShutdown
        {
            get
            {
                return (this.acceptedCommands & 4) != 0;
            }
            set
            {
                if (this.commandPropsFrozen)
                    throw new InvalidOperationException(GetString("CannotChangeProperties"));
                if (value)
                    this.acceptedCommands |= 4;
                else
                    this.acceptedCommands &= -5;
            }
        }

        [DefaultValue(true)]
        public bool CanStop
        {
            get
            {
                return (this.acceptedCommands & 1) != 0;
            }
            set
            {
                if (this.commandPropsFrozen)
                    throw new InvalidOperationException(GetString("CannotChangeProperties"));
                if (value)
                    this.acceptedCommands |= 1;
                else
                    this.acceptedCommands &= -2;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public virtual EventLog EventLog
        {
            get
            {
                if (this.eventLog == null)
                {
                    this.eventLog = new EventLog();
                    this.eventLog.Source = this.ServiceName;
                    this.eventLog.Log = "Application";
                }
                return this.eventLog;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected IntPtr ServiceHandle
        {
            get
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                return this.statusHandle;
            }
        }

        [ServiceProcessDescription("SBServiceName")]
        [TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string ServiceName
        {
            get
            {
                return this.serviceName;
            }
            set
            {
                if (this.nameFrozen)
                    throw new InvalidOperationException(GetString("CannotChangeName"));
                if (value != "" && !ValidServiceName(value))
                    throw new ArgumentException(GetString("ServiceName", (object)value, (object)80.ToString((IFormatProvider)CultureInfo.CurrentCulture)));
                else
                    this.serviceName = value;
            }
        }

        static bool ValidServiceName(string serviceName)
        {
            if (serviceName == null || serviceName.Length > 80 || serviceName.Length == 0)
                return false;
            foreach (char ch in serviceName.ToCharArray())
            {
                switch (ch)
                {
                    case '\\':
                    case '/':
                        return false;
                    default:
                        break;
                }
            }
            return true;
        }

        private static bool IsRTLResources
        {
            get
            {
                return false;
            }
        }

        public MyServiceBase()
        {
            this.acceptedCommands = 1;
            this.AutoLog = true;
            this.ServiceName = "";
        }

        [ComVisible(false)]
        public unsafe void RequestAdditionalTime(int milliseconds)
        {
            fixed (NativeMethods.SERVICE_STATUS* status = &this.status)
            {
                if (this.status.currentState != 5 && this.status.currentState != 2 && (this.status.currentState != 3 && this.status.currentState != 6))
                    throw new InvalidOperationException(GetString("NotInPendingState"));
                this.status.waitHint = milliseconds;
                ++this.status.checkPoint;
                NativeMethods.SetServiceStatus(this.statusHandle, status);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (this.handleName != (IntPtr)0)
            {
                Marshal.FreeHGlobal(this.handleName);
                this.handleName = (IntPtr)0;
            }
            this.nameFrozen = false;
            this.commandPropsFrozen = false;
            this.disposed = true;
            base.Dispose(disposing);
        }

        protected virtual void OnContinue()
        {
        }

        protected virtual void OnPause()
        {
        }

        protected virtual bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return true;
        }

        protected virtual void OnSessionChange(SessionChangeDescription changeDescription)
        {
        }

        protected virtual void OnShutdown()
        {
        }

        protected virtual void OnStart(string[] args)
        {
        }

        protected virtual void OnStop()
        {
        }

        private unsafe void DeferredStop()
        {
            fixed (NativeMethods.SERVICE_STATUS* status = &this.status)
            {
                int num = this.status.currentState;
                this.status.checkPoint = 0;
                this.status.waitHint = 0;
                this.status.currentState = 3;
                NativeMethods.SetServiceStatus(this.statusHandle, status);
                try
                {
                    this.OnStop();
                    this.WriteEventLogEntry(GetString("StopSuccessful"));
                    this.status.currentState = 1;
                    NativeMethods.SetServiceStatus(this.statusHandle, status);
                    if (this.isServiceHosted)
                    {
                        try
                        {
                            AppDomain.Unload(AppDomain.CurrentDomain);
                        }
                        catch (CannotUnloadAppDomainException ex)
                        {
                            this.WriteEventLogEntry(GetString("FailedToUnloadAppDomain", (object)AppDomain.CurrentDomain.FriendlyName, (object)ex.Message), EventLogEntryType.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.status.currentState = num;
                    NativeMethods.SetServiceStatus(this.statusHandle, status);
                    this.WriteEventLogEntry(GetString("StopFailed", new object[1]
          {
            (object) ((object) ex).ToString()
          }), EventLogEntryType.Error);
                    throw;
                }
            }
        }

        protected virtual void OnCustomCommand(int command)
        {
        }

        public static void Run(MyServiceBase[] services)
        {
            if (services == null || services.Length == 0)
                throw new ArgumentException(GetString("NoServices"));
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                MyServiceBase.LateBoundMessageBoxShow(GetString("CantRunOnWin9x"), GetString("CantRunOnWin9xTitle"));
            }
            else
            {
                IntPtr entry = Marshal.AllocHGlobal((IntPtr)((services.Length + 1) * Marshal.SizeOf(typeof(NativeMethods.SERVICE_TABLE_ENTRY))));
                NativeMethods.SERVICE_TABLE_ENTRY[] serviceTableEntryArray = new NativeMethods.SERVICE_TABLE_ENTRY[services.Length];
                bool multipleServices = services.Length > 1;
                IntPtr num = (IntPtr)0;
                for (int index = 0; index < services.Length; ++index)
                {
                    services[index].Initialize(multipleServices);
                    serviceTableEntryArray[index] = services[index].GetEntry();
                    IntPtr ptr = (IntPtr)((long)entry + (long)(Marshal.SizeOf(typeof(NativeMethods.SERVICE_TABLE_ENTRY)) * index));
                    Marshal.StructureToPtr((object)serviceTableEntryArray[index], ptr, true);
                }
                NativeMethods.SERVICE_TABLE_ENTRY serviceTableEntry = new NativeMethods.SERVICE_TABLE_ENTRY();
                serviceTableEntry.callback = (Delegate)null;
                serviceTableEntry.name = (IntPtr)0;
                IntPtr ptr1 = (IntPtr)((long)entry + (long)(Marshal.SizeOf(typeof(NativeMethods.SERVICE_TABLE_ENTRY)) * services.Length));
                Marshal.StructureToPtr((object)serviceTableEntry, ptr1, true);
                bool flag = NativeMethods.StartServiceCtrlDispatcher(entry);
                string str = "";
                if (!flag)
                {
                    str = new Win32Exception().Message;
                    string string1 = GetString("CantStartFromCommandLine");
                    if (Environment.UserInteractive)
                    {
                        string string2 = GetString("CantStartFromCommandLineTitle");
                        MyServiceBase.LateBoundMessageBoxShow(string1, string2);
                    }
                    else
                        Console.WriteLine(string1);
                }
                foreach (MyServiceBase serviceBase in services)
                {
                    serviceBase.Dispose();
                    if (!flag && serviceBase.EventLog.Source.Length != 0)
                        serviceBase.WriteEventLogEntry(GetString("StartFailed", new object[1]
            {
              (object) str
            }), EventLogEntryType.Error);
                }
            }
        }

        public static void Run(MyServiceBase service)
        {
            if (service == null)
                throw new ArgumentException(GetString("NoServices"));
            MyServiceBase.Run(new MyServiceBase[1]
      {
        service
      });
        }

        public void Stop()
        {
            this.DeferredStop();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [ComVisible(false)]
        public unsafe void ServiceMainCallback(int argCount, IntPtr argPointer)
        {
            fixed (NativeMethods.SERVICE_STATUS* status = &this.status)
            {
                string[] strArray = (string[])null;
                if (argCount > 0)
                {
                    char** chPtr = (char**)argPointer.ToPointer();
                    UsedServiceName = Marshal.PtrToStringUni((IntPtr)((void*)*chPtr));

                    strArray = new string[argCount - 1];
                    for (int index = 0; index < strArray.Length; ++index)
                    {
                        ++chPtr;
                        strArray[index] = Marshal.PtrToStringUni((IntPtr)((void*)*chPtr));
                    }
                }
                if (!this.initialized)
                {
                    this.isServiceHosted = true;
                    this.Initialize(true);
                }
                this.statusHandle = Environment.OSVersion.Version.Major < 5 ? NativeMethods.RegisterServiceCtrlHandler(this.ServiceName, (Delegate)this.commandCallback) : NativeMethods.RegisterServiceCtrlHandlerEx(this.ServiceName, (Delegate)this.commandCallbackEx, (IntPtr)0);
                this.nameFrozen = true;
                if (this.statusHandle == (IntPtr)0)
                    this.WriteEventLogEntry(GetString("StartFailed", new object[1]
          {
            (object) new Win32Exception().Message
          }), EventLogEntryType.Error);
                this.status.controlsAccepted = this.acceptedCommands;
                this.commandPropsFrozen = true;
                if ((this.status.controlsAccepted & 1) != 0)
                    this.status.controlsAccepted = this.status.controlsAccepted | 4;
                if (Environment.OSVersion.Version.Major < 5)
                    this.status.controlsAccepted &= -65;
                this.status.currentState = 2;
                if (!NativeMethods.SetServiceStatus(this.statusHandle, status))
                    return;
                this.startCompletedSignal = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.ServiceQueuedMainCallback), (object)strArray);
                this.startCompletedSignal.WaitOne();
                if (!NativeMethods.SetServiceStatus(this.statusHandle, status))
                {
                    this.WriteEventLogEntry(GetString("StartFailed", new object[1]
          {
            (object) new Win32Exception().Message
          }), EventLogEntryType.Error);
                    this.status.currentState = 1;
                    NativeMethods.SetServiceStatus(this.statusHandle, status);
                }
            }
        }

        private unsafe void DeferredContinue()
        {
            fixed (NativeMethods.SERVICE_STATUS* status = &this.status)
            {
                try
                {
                    this.OnContinue();
                    this.WriteEventLogEntry(GetString("ContinueSuccessful"));
                    this.status.currentState = 4;
                }
                catch (Exception ex)
                {
                    this.status.currentState = 7;
                    this.WriteEventLogEntry(GetString("ContinueFailed", new object[1]
          {
            (object) ((object) ex).ToString()
          }), EventLogEntryType.Error);
                    throw;
                }
                finally
                {
                    NativeMethods.SetServiceStatus(this.statusHandle, status);
                }
            }
        }

        private void DeferredCustomCommand(int command)
        {
            try
            {
                this.OnCustomCommand(command);
                this.WriteEventLogEntry(GetString("CommandSuccessful"));
            }
            catch (Exception ex)
            {
                this.WriteEventLogEntry(GetString("CommandFailed", new object[1]
        {
          (object) ((object) ex).ToString()
        }), EventLogEntryType.Error);
                throw;
            }
        }

        private unsafe void DeferredPause()
        {
            fixed (NativeMethods.SERVICE_STATUS* status = &this.status)
            {
                try
                {
                    this.OnPause();
                    this.WriteEventLogEntry(GetString("PauseSuccessful"));
                    this.status.currentState = 7;
                }
                catch (Exception ex)
                {
                    this.status.currentState = 4;
                    this.WriteEventLogEntry(GetString("PauseFailed", new object[1]
          {
            (object) ((object) ex).ToString()
          }), EventLogEntryType.Error);
                    throw;
                }
                finally
                {
                    NativeMethods.SetServiceStatus(this.statusHandle, status);
                }
            }
        }

        private void DeferredPowerEvent(int eventType, IntPtr eventData)
        {
            try
            {
                this.OnPowerEvent((PowerBroadcastStatus)eventType);
                this.WriteEventLogEntry(GetString("PowerEventOK"));
            }
            catch (Exception ex)
            {
                this.WriteEventLogEntry(GetString("PowerEventFailed", new object[1]
                {
                  (object) ((object) ex).ToString()
                }), EventLogEntryType.Error);
                throw;
            }
        }

        private void DeferredSessionChange(int eventType, int sessionId)
        {
            try
            {
                this.OnSessionChange(NativeMethods.CreateSessionChangeDescription((SessionChangeReason)eventType, sessionId));
            }
            catch (Exception ex)
            {
                this.WriteEventLogEntry(GetString("SessionChangeFailed", new object[1]
        {
          (object) ((object) ex).ToString()
        }), EventLogEntryType.Error);
                throw;
            }
        }

        private unsafe void DeferredShutdown()
        {
            try
            {
                this.OnShutdown();
                this.WriteEventLogEntry(GetString("ShutdownOK"));
                if (this.status.currentState != 7)
                {
                    if (this.status.currentState != 4)
                        return;
                }
                fixed (NativeMethods.SERVICE_STATUS* status = &this.status)
                {
                    this.status.checkPoint = 0;
                    this.status.waitHint = 0;
                    this.status.currentState = 1;
                    NativeMethods.SetServiceStatus(this.statusHandle, status);
                    if (!this.isServiceHosted)
                        return;
                    try
                    {
                        AppDomain.Unload(AppDomain.CurrentDomain);
                    }
                    catch (CannotUnloadAppDomainException ex)
                    {
                        this.WriteEventLogEntry(GetString("FailedToUnloadAppDomain", (object)AppDomain.CurrentDomain.FriendlyName, (object)ex.Message), EventLogEntryType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                this.WriteEventLogEntry(GetString("ShutdownFailed", new object[1]
        {
          (object) ((object) ex).ToString()
        }), EventLogEntryType.Error);
                throw;
            }
        }

        private void Initialize(bool multipleServices)
        {
            if (this.initialized)
                return;
            if (this.disposed)
                throw new ObjectDisposedException(this.GetType().Name);
            this.status.serviceType = multipleServices ? 32 : 16;
            this.status.currentState = 2;
            this.status.controlsAccepted = 0;
            this.status.win32ExitCode = 0;
            this.status.serviceSpecificExitCode = 0;
            this.status.checkPoint = 0;
            this.status.waitHint = 0;
            this.mainCallback = new NativeMethods.ServiceMainCallback(this.ServiceMainCallback);
            this.commandCallback = new NativeMethods.ServiceControlCallback(this.ServiceCommandCallback);
            this.commandCallbackEx = new NativeMethods.ServiceControlCallbackEx(this.ServiceCommandCallbackEx);
            this.handleName = Marshal.StringToHGlobalUni(this.ServiceName);
            this.initialized = true;
        }

        private NativeMethods.SERVICE_TABLE_ENTRY GetEntry()
        {
            NativeMethods.SERVICE_TABLE_ENTRY serviceTableEntry = new NativeMethods.SERVICE_TABLE_ENTRY();
            this.nameFrozen = true;
            serviceTableEntry.callback = (Delegate)this.mainCallback;
            serviceTableEntry.name = this.handleName;
            return serviceTableEntry;
        }

        private static void LateBoundMessageBoxShow(string message, string title)
        {
            int num = 0;
            if (MyServiceBase.IsRTLResources)
                num |= 1572864;
            Type type1 = Type.GetType("System.Windows.Forms.MessageBox, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Type type2 = Type.GetType("System.Windows.Forms.MessageBoxButtons, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Type type3 = Type.GetType("System.Windows.Forms.MessageBoxIcon, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Type type4 = Type.GetType("System.Windows.Forms.MessageBoxDefaultButton, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Type type5 = Type.GetType("System.Windows.Forms.MessageBoxOptions, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            type1.InvokeMember("Show", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, (Binder)null, (object)null, new object[6]
      {
        (object) message,
        (object) title,
        Enum.ToObject(type2, 0),
        Enum.ToObject(type3, 0),
        Enum.ToObject(type4, 0),
        Enum.ToObject(type5, num)
      }, CultureInfo.InvariantCulture);
        }

        private int ServiceCommandCallbackEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
        {
            int num = 0;
            switch (command)
            {
                case 13:
                    new MyServiceBase.DeferredHandlerDelegateAdvanced(this.DeferredPowerEvent).BeginInvoke(eventType, eventData, (AsyncCallback)null, (object)null);
                    break;
                case 14:
                    MyServiceBase.DeferredHandlerDelegateAdvancedSession delegateAdvancedSession = new MyServiceBase.DeferredHandlerDelegateAdvancedSession(this.DeferredSessionChange);
                    NativeMethods.WTSSESSION_NOTIFICATION wtssessionNotification = new NativeMethods.WTSSESSION_NOTIFICATION();
                    Marshal.PtrToStructure(eventData, (object)wtssessionNotification);
                    delegateAdvancedSession.BeginInvoke(eventType, wtssessionNotification.sessionId, (AsyncCallback)null, (object)null);
                    break;
                default:
                    this.ServiceCommandCallback(command);
                    break;
            }
            return num;
        }

        private unsafe void ServiceCommandCallback(int command)
        {
            fixed (NativeMethods.SERVICE_STATUS* status = &this.status)
            {
                if (command == 4)
                    NativeMethods.SetServiceStatus(this.statusHandle, status);
                else if (this.status.currentState != 5 && this.status.currentState != 2 && (this.status.currentState != 3 && this.status.currentState != 6))
                {
                    switch (command)
                    {
                        case 1:
                            int num = this.status.currentState;
                            if (this.status.currentState == 7 || this.status.currentState == 4)
                            {
                                this.status.currentState = 3;
                                NativeMethods.SetServiceStatus(this.statusHandle, status);
                                this.status.currentState = num;
                                new MyServiceBase.DeferredHandlerDelegate(this.DeferredStop).BeginInvoke((AsyncCallback)null, (object)null);
                                break;
                            }
                            else
                                break;
                        case 2:
                            if (this.status.currentState == 4)
                            {
                                this.status.currentState = 6;
                                NativeMethods.SetServiceStatus(this.statusHandle, status);
                                new MyServiceBase.DeferredHandlerDelegate(this.DeferredPause).BeginInvoke((AsyncCallback)null, (object)null);
                                break;
                            }
                            else
                                break;
                        case 3:
                            if (this.status.currentState == 7)
                            {
                                this.status.currentState = 5;
                                NativeMethods.SetServiceStatus(this.statusHandle, status);
                                new MyServiceBase.DeferredHandlerDelegate(this.DeferredContinue).BeginInvoke((AsyncCallback)null, (object)null);
                                break;
                            }
                            else
                                break;
                        case 5:
                            new MyServiceBase.DeferredHandlerDelegate(this.DeferredShutdown).BeginInvoke((AsyncCallback)null, (object)null);
                            break;
                        default:
                            new MyServiceBase.DeferredHandlerDelegateCommand(this.DeferredCustomCommand).BeginInvoke(command, (AsyncCallback)null, (object)null);
                            break;
                    }
                }
            }
        }

        private void ServiceQueuedMainCallback(object state)
        {
            string[] args = (string[])state;

            try
            {
                this.OnStart(args);
                this.WriteEventLogEntry(GetString("StartSuccessful"));
                this.status.checkPoint = 0;
                this.status.waitHint = 0;
                this.status.currentState = 4;
            }
            catch (Exception ex)
            {
                this.WriteEventLogEntry(GetString("StartFailed", new object[1]
        {
          (object) ((object) ex).ToString()
        }), EventLogEntryType.Error);
                this.status.currentState = 1;
            }
            this.startCompletedSignal.Set();
        }

        private void WriteEventLogEntry(string message)
        {
            try
            {
                if (!this.AutoLog)
                    return;
                this.EventLog.WriteEntry(message);
            }
            catch (StackOverflowException)
            {
                throw;
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch
            {
            }
        }

        private void WriteEventLogEntry(string message, EventLogEntryType errorType)
        {
            try
            {
                if (!this.AutoLog)
                    return;
                this.EventLog.WriteEntry(message, errorType);
            }
            catch (StackOverflowException)
            {
                throw;
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Don't want to include resources, just return key
        /// </summary>
        private static string GetString(string str)
        {
            return str;
        }

        /// <summary>
        /// Don't want to include resources, just return key + args
        /// </summary>
        private static string GetString(string str, params object[] args)
        {
            foreach (var a in args)
            {
                str += ", " + a;
            }
            return str;
        }

        private delegate void DeferredHandlerDelegate();

        private delegate void DeferredHandlerDelegateCommand(int command);

        private delegate void DeferredHandlerDelegateAdvanced(int eventType, IntPtr eventData);

        private delegate void DeferredHandlerDelegateAdvancedSession(int eventType, int sessionId);
    }
}
#endif // !XB1
