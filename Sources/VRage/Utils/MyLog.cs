using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using VRage.Generics;
using VRage.Utils;
using System.Globalization;
using System.Reflection;
using SystemTrace = System.Diagnostics.Trace;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.Library;

namespace VRage.Utils
{
    [Flags]
    public enum LoggingOptions
    {
        NONE = 1 << 0,
        ENUM_CHECKING = 1 << 1,
        LOADING_MODELS = 1 << 2,
        LOADING_TEXTURES = 1 << 3,
        LOADING_CUSTOM_ASSETS = 1 << 4,
        LOADING_SPRITE_VIDEO = 1 << 5,
        VALIDATING_CUE_PARAMS = 1 << 6,
        CONFIG_ACCESS = 1 << 7,
        SIMPLE_NETWORKING = 1 << 8,
        VOXEL_MAPS = 1 << 9,
        MISC_RENDER_ASSETS = 1 << 10, // Decals, fonts, debug draw objects, simple draw objects
        AUDIO = 1 << 11,
        TRAILERS = 1 << 12,
        SESSION_SETTINGS = 1 << 13,

        ALL = (SESSION_SETTINGS << 1) - 1,
    }

    public enum MyLogSeverity
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    [Unsharper.UnsharperDisableReflection()]
    public class MyLog
    {
        public struct IndentToken : IDisposable
        {
            MyLog m_log;
            LoggingOptions m_options;

            internal IndentToken(MyLog log, LoggingOptions options)
            {
                Debug.Assert(log != null);
                m_log = log;
                m_options = options;
                m_log.IncreaseIndent(options);
            }

            public void Dispose()
            {
                Debug.Assert(m_log != null);
                if (m_log != null)
                {
                    m_log.DecreaseIndent(m_options);
                    m_log = null;
                }
            }

        }

        struct MyLogIndentKey
        {
            public int ThreadId;
            public int Indent;     //  Can be 0, 1, 2, 3, ...

            public MyLogIndentKey(int threadId, int indent)
            {
                ThreadId = threadId;
                Indent = indent;
            }
        }

        struct MyLogIndentValue
        {
            public long LastGcTotalMemory;
            public long LastWorkingSet;
            public DateTimeOffset LastDateTimeOffset;     //  DateTimeOffset.Now doesn't do garbage (DateTime.Now does internal allocations)

            public MyLogIndentValue(long lastGcTotalMemory, long lastWorkingSet, DateTimeOffset lastDateTimeOffset)
            {
                LastGcTotalMemory = lastGcTotalMemory;
                LastWorkingSet = lastWorkingSet;
                LastDateTimeOffset = lastDateTimeOffset;
            }
        }


        private bool m_alwaysFlush = false;
        public static MyLogSeverity AssertLevel = (MyLogSeverity)(byte.MaxValue);
        private bool LogForMemoryProfiler = false;
        private bool m_enabled = false;             //  Must be false, beuuase MW web site must not write into log file
        private Stream m_stream;                    //  Used for opening and closing the file
        private StreamWriter m_streamWriter;        //  Used for writing into the file
        private readonly Object m_lock = new Object();
        private Dictionary<int, int> m_indentsByThread;
        private Dictionary<MyLogIndentKey, MyLogIndentValue> m_indents;
        private string m_filepath;
        private StringBuilder m_stringBuilder = new StringBuilder(2048);
        private char[] m_tmpWrite = new char[2048];
        private LoggingOptions m_loggingOptions = (LoggingOptions.ALL & ~LoggingOptions.LOADING_MODELS); // Everything except loading models
        private Action<string> m_normalWriter;
        private Action<string> m_closedLogWriter;

        static MyLog m_default;
        public static MyLog Default
        {
            get { return m_default; }
            set { m_default = value; }
        }

        public LoggingOptions Options
        {
            get { return m_loggingOptions; }
            set { value = m_loggingOptions; }
        }

        public bool LogEnabled
        {
            get
            {
                return m_enabled;
            }
        }

        public MyLog(bool alwaysFlush = false)
        {
            m_alwaysFlush = alwaysFlush;
        }

        public void Init(string logFileName, StringBuilder appVersionString)
        {
            lock (m_lock)
            {
                try
                {
                    m_filepath = Path.IsPathRooted(logFileName) ? logFileName : Path.Combine(MyFileSystem.UserDataPath, logFileName);
                    m_stream = MyFileSystem.OpenWrite(m_filepath);
                    m_streamWriter = new StreamWriter(m_stream);
                    m_normalWriter = new Action<string>(WriteLine);
                    m_closedLogWriter = new Action<string>((s) => File.AppendAllText(m_filepath, s + MyEnvironment.NewLine));
                    m_enabled = true;
                }
                catch (Exception e)
                {
                    SystemTrace.Fail("Cannot create log file: " + e.ToString());
                }

                m_indentsByThread = new Dictionary<int, int>();
                m_indents = new Dictionary<MyLogIndentKey, MyLogIndentValue>();

                int timezone = (int)Math.Round((DateTime.Now - DateTime.UtcNow).TotalHours);

                WriteLine("Log Started");
                WriteLine(String.Format("Timezone (local - UTC): {0}h", timezone));
                WriteLine("App Version: " + appVersionString);
            }
        }

        public string GetFilePath()
        {
            lock (m_lock)
            {
                return m_filepath;
            }
        }

        public IndentToken IndentUsing(LoggingOptions options = LoggingOptions.NONE)
        {
            return new IndentToken(this, options);
        }

        public void IncreaseIndent(LoggingOptions option)
        {
            if (LogFlag(option))
            {
                IncreaseIndent();
            }
        }

        public void IncreaseIndent()
        {
            if (m_enabled == false) return;

            lock (m_lock)
            {
                if (m_enabled)
                {
                    int threadId = GetThreadId();
                    m_indentsByThread[threadId] = GetIdentByThread(threadId) + 1;

                    MyLogIndentKey indentKey = new MyLogIndentKey(threadId, m_indentsByThread[threadId]);
                    m_indents[indentKey] = new MyLogIndentValue(GetManagedMemory(), GetSystemMemory(), DateTimeOffset.Now);

                    if (LogForMemoryProfiler)
                        MyMemoryLogs.StartEvent();
                }
            }
        }

        public bool IsIndentKeyIncreased()
        {
            if (m_enabled == false) return false;

            lock (m_lock)
            {
                if (m_enabled == false) return false;

                int threadId = GetThreadId();
                MyLogIndentKey indentKey = new MyLogIndentKey(threadId, GetIdentByThread(threadId));

                //  If this fails, then order of IncreaseIndent/DecreaseIndent was wrong, or duplicate, etc
                return m_indents.ContainsKey(indentKey);
            }
        }

        public void DecreaseIndent(LoggingOptions option)
        {
            if (LogFlag(option))
            {
                DecreaseIndent();
            }
        }

        public void DecreaseIndent()
        {
            if (m_enabled == false) return;

            MyLogIndentValue indentValue;

            lock (m_lock)
            {
                if (m_enabled == false) return;

                int threadId = GetThreadId();
                MyLogIndentKey indentKey = new MyLogIndentKey(threadId, GetIdentByThread(threadId));

                //  If this fails, then order of IncreaseIndent/DecreaseIndent was wrong, or duplicate, etc
                MyDebug.AssertDebug(m_indents.ContainsKey(indentKey));

                indentValue = m_indents[indentKey];

                if (LogForMemoryProfiler)
                {
                    MyMemoryLogs.MyMemoryEvent memEvent = new MyMemoryLogs.MyMemoryEvent();
                    memEvent.DeltaTime = (float)(DateTimeOffset.Now - indentValue.LastDateTimeOffset).TotalMilliseconds / 1000.0f;
                    memEvent.ManagedEndSize = GetManagedMemory();
                    memEvent.ProcessEndSize = GetSystemMemory();
                    memEvent.ManagedStartSize = indentValue.LastGcTotalMemory;
                    memEvent.ProcessStartSize = indentValue.LastWorkingSet;
                    MyMemoryLogs.EndEvent(memEvent);
                }

            }

            lock (m_lock)
            {
                int threadId = GetThreadId();
                m_indentsByThread[threadId] = GetIdentByThread(threadId) - 1;

            }
        }

        string GetFormatedMemorySize(long bytesCount)
        {
            return MyValueFormatter.GetFormatedFloat(bytesCount / 1024.0f / 1024.0f, 3) + " Mb (" +
                   MyValueFormatter.GetFormatedLong(bytesCount) + " bytes)";
        }

        long GetManagedMemory()
        {
            return GC.GetTotalMemory(false);
        }

        long GetSystemMemory()
        {
            return MyEnvironment.WorkingSetForMyLog;
        }

        //	Must be called before application ends
        public void Close()
        {
            //Debug.Close();

            if (m_enabled == false) return;

            lock (m_lock)
            {
                if (m_enabled == false) return;

                WriteLine("Log Closed");

                m_streamWriter.Close();
                m_stream.Close();

                //	Only for making sure that nobody will call WriteLine after Close
                m_stream = null;
                m_streamWriter = null;

                m_enabled = false;
            }
        }

        public void AppendToClosedLog(string text)
        {
            if (m_enabled)
            {
                WriteLine(text);
            }
            else if (m_filepath != null)
            {
                File.AppendAllText(m_filepath, text + MyEnvironment.NewLine);
            }
        }

        public void AppendToClosedLog(Exception e)
        {
            if (m_enabled)
            {
                WriteLine(e);
            }
            else if (m_filepath != null)
            {
                WriteLine(m_closedLogWriter, e);
            }
        }

        public bool LogFlag(LoggingOptions option)
        {
            return (m_loggingOptions & option) != 0;
        }

        public void WriteLine(string message, LoggingOptions option)
        {
            if (LogFlag(option))
            {
                WriteLine(message);
            }
        }

        private static void WriteLine(Action<string> writer, Exception ex)
        {
            writer("Exception occured: " + ((ex == null) ? "null" : ex.ToString()));
            if (ex != null && ex is ReflectionTypeLoadException)
            {
                writer("LoaderExceptions: ");
                foreach (var e in ((ReflectionTypeLoadException)ex).LoaderExceptions)
                {
                    WriteLine(writer, e);
                }
            }
            if (ex != null && ex.InnerException != null)
            {
                writer("InnerException: ");
                WriteLine(writer, ex.InnerException);
            }
        }

        //  Write an exception on new line
        public void WriteLine(Exception ex)
        {
            if (m_enabled == false) return;

            WriteLine(m_normalWriter, ex);

            m_streamWriter.Flush();
        }

        StringBuilder m_consoleStringBuilder = new StringBuilder();

        public void WriteLineAndConsole(string msg)
        {
            WriteLine(msg);

            m_consoleStringBuilder.Clear();
            AppendDateAndTime(m_consoleStringBuilder);
            m_consoleStringBuilder.Append(": ");
            m_consoleStringBuilder.Append(msg);
            Console.WriteLine(m_consoleStringBuilder.ToString());
        }

        //  Write a string on new line
        public void WriteLine(string msg)
        {
            if (m_enabled)
            {
                lock (m_lock)
                {
                    if (m_enabled)
                    {
                        WriteDateTimeAndThreadId();
                        WriteString(msg);
                        m_streamWriter.WriteLine();

                        if (m_alwaysFlush)
                            m_streamWriter.Flush();
                    }
                }
            }

            if (LogForMemoryProfiler)
            {
                MyMemoryLogs.AddConsoleLine(msg);
            }

            //Debug.WriteLine(msg);
        }

		//Crash object builder logging
		//TODO: remove or make sure it uses lock,enabled, etc...
        public TextWriter GetTextWriter()
        {
            return m_streamWriter;
        }

        private string GetGCMemoryString(string prependText = "")
        {
            return String.Format("{0}: GC Memory: {1} B", prependText, GetManagedMemory().ToString("##,#"));
        }

        public void WriteMemoryUsage(string prefixText)
        {
            WriteLine(GetGCMemoryString(prefixText));
        }

        //  Log info about ThreadPool
        public void LogThreadPoolInfo()
        {
#if XB1
			Debug.Assert(false);
#else
            if (m_enabled == false) return;

            WriteLine("LogThreadPoolInfo - START");
            IncreaseIndent();

            int workerThreads;
            int completionPortThreads;

            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            WriteLine("GetMaxThreads.WorkerThreads: " + workerThreads);
            WriteLine("GetMaxThreads.CompletionPortThreads: " + completionPortThreads);

            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            WriteLine("GetMinThreads.WorkerThreads: " + workerThreads);
            WriteLine("GetMinThreads.CompletionPortThreads: " + completionPortThreads);

            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
            WriteLine("GetAvailableThreads.WorkerThreads: " + workerThreads);
            WriteLine("GetAvailableThreads.WompletionPortThreads: " + completionPortThreads);

            DecreaseIndent();
            WriteLine("LogThreadPoolInfo - END");
#endif
        }

        //	Return message with included datetime information. We are using when logging.
        void WriteDateTimeAndThreadId()
        {
            m_stringBuilder.Clear();
            AppendDateAndTime(m_stringBuilder);
            m_stringBuilder.Append(" - ");
            m_stringBuilder.Append("Thread: ");
            m_stringBuilder.Concat(GetThreadId(), 3, ' ');
            m_stringBuilder.Append(" ->  ");
            m_stringBuilder.Append(' ', GetIdentByThread(GetThreadId()) * 3);

            WriteStringBuilder(m_stringBuilder);
        }

        void AppendDateAndTime(StringBuilder sb)
        {
            var d = DateTimeOffset.Now;
            // 2014-01-25 14:26:26.524
            //m_stringBuilder.Append(MyValueFormatter.GetFormatedDateTimeOffset(DateTimeOffset.Now));
            sb.Concat(d.Year, 4, '0', 10, false).Append('-');
            sb.Concat(d.Month, 2).Append('-');
            sb.Concat(d.Day, 2).Append(' ');
            sb.Concat(d.Hour, 2).Append(':');
            sb.Concat(d.Minute, 2).Append(':');
            sb.Concat(d.Second, 2).Append('.');
            sb.Concat(d.Millisecond, 3);
        }

        void WriteString(String text)
        {
            if (text == null ||
    m_tmpWrite == null ||
    m_streamWriter == null)
                return;


            if (text == null)
            {
                Debug.Fail("text shouldn't be null!");
                text = "UNKNOWN ERROR: text shouldn't be null!";
            }
            if (m_tmpWrite.Length < text.Length)
            {
                Array.Resize(ref m_tmpWrite, Math.Max(m_tmpWrite.Length * 2, text.Length));
            }
            text.CopyTo(0, m_tmpWrite, 0, text.Length);
            m_streamWriter.Write(m_tmpWrite, 0, text.Length);
        }

        void WriteStringBuilder(StringBuilder sb)
        {
            //JC: fix for a NullReferenceException, when the game is closed
            if (sb == null || 
                m_tmpWrite == null ||
                m_streamWriter == null)
                return;

            if (m_tmpWrite.Length < sb.Length)
            {
                Array.Resize(ref m_tmpWrite, Math.Max(m_tmpWrite.Length * 2, sb.Length));
            }
            sb.CopyTo(0, m_tmpWrite, 0, sb.Length);
            m_streamWriter.Write(m_tmpWrite, 0, sb.Length);
        }

        int GetThreadId()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }

        //  Get actual ident for specified thread. If not specified yet, we assume it's zero.
        int GetIdentByThread(int threadId)
        {
            int retVal;
            if (m_indentsByThread.TryGetValue(threadId, out retVal) == false)
            {
                retVal = 0;
            }

            //  If retVal is negative, then someone used wrong order of increase ident and decrease ident
            //  E.g. used MyMwcLog.Default.DecreaseIndent(); at the start of a method whereas there should be MyMwcLog.Default.IncreaseIndent();
            MyDebug.AssertDebug(retVal >= 0);

            return retVal;
        }

        public void Log(MyLogSeverity severity, string format, params object[] args)
        {
            if (m_enabled)
            {
                lock (m_lock)
                {
                    if (m_enabled)
                    {
                        WriteDateTimeAndThreadId();

                        StringBuilder sb = m_stringBuilder;
                        sb.Clear();

                        sb.AppendFormat("{0}: ", severity);
                        sb.AppendFormat(format, args);
                        sb.Append('\n');

                        WriteStringBuilder(sb);

                        if ((int)severity >= (int)AssertLevel)
                            SystemTrace.Fail(sb.ToString());
                    }
                }
            }
        }

        public void Log(MyLogSeverity severity, StringBuilder builder)
        {
            if (m_enabled)
            {
                lock (m_lock)
                {
                    if (m_enabled)
                    {
                        WriteDateTimeAndThreadId();

                        StringBuilder sb = m_stringBuilder;
                        sb.Clear();

                        sb.AppendFormat("{0}: ", severity);
                        sb.AppendStringBuilder(builder);
                        sb.Append('\n');

                        WriteStringBuilder(sb);

                        if ((int)severity >= (int)AssertLevel)
                            SystemTrace.Fail(sb.ToString());
                    }
                }
            }
        }

        public void Flush()
        {
            m_streamWriter.Flush();
        }
    }

    public static class MyLogExtensions
    {
        [Conditional("DEBUG")]
        public static void Debug(this MyLog self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Debug, message, args);
        }

        [Conditional("DEBUG")]
        public static void Debug(this MyLog self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Debug, buillder);
        }

        public static void Info(this MyLog self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Info, message, args);
        }

        public static void Info(this MyLog self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Info, buillder);
        }

        public static void Warning(this MyLog self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Warning, message, args);
        }

        public static void Warning(this MyLog self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Warning, buillder);
        }

        public static void Error(this MyLog self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Error, message, args);
        }

        public static void Error(this MyLog self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Error, buillder);
        }

        public static void Critical(this MyLog self, string message, params object[] args)
        {
            self.Log(MyLogSeverity.Critical, message, args);
        }

        public static void Critical(this MyLog self, StringBuilder buillder)
        {
            self.Log(MyLogSeverity.Critical, buillder);
        }

    }
}