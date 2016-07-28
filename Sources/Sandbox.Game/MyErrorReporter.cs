using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Game;
using VRage.Utils;
using System.Diagnostics;

namespace Sandbox
{
    public class MyErrorReporter
    {
        static bool AllowSendDialog(string gameName, string logfile, string errorMessage)
        {
            string text = String.Format(errorMessage, gameName, logfile);
            return Sandbox.MyMessageBox.Show(IntPtr.Zero, text, gameName, MessageBoxOptions.YesNo | MessageBoxOptions.SystemModal | MessageBoxOptions.IconExclamation | MessageBoxOptions.SetForeground) == MessageBoxResult.Yes;
        }

        public static void ReportRendererCrash(string logfile, string gameName, string minimumRequirementsPage, VRageRender.MyRenderExceptionEnum type)
        {
            string format;
            switch(type)
            {
                case VRageRender.MyRenderExceptionEnum.GpuNotSupported:
                    format = APP_ERROR_MESSAGE_LOW_GPU;
                    break;
                case VRageRender.MyRenderExceptionEnum.DriverNotInstalled:
                    format = APP_ERROR_MESSAGE_DRIVER_NOT_INSTALLED;
                    break;
                default:
                    format = APP_ERROR_MESSAGE_LOW_GPU;
                    break;
            }

            string text = String.Format(format, logfile, gameName, minimumRequirementsPage);
            Sandbox.MyMessageBox.Show(IntPtr.Zero, text, gameName, MessageBoxOptions.OkOnly | MessageBoxOptions.SystemModal | MessageBoxOptions.IconExclamation | MessageBoxOptions.SetForeground);
        }

        public static void ReportNotCompatibleGPU(string gameName, string logfile, string minimumRequirementsPage)
        {
            string text = String.Format(APP_WARNING_MESSAGE_UNSUPPORTED_GPU, logfile, gameName, minimumRequirementsPage);
            Sandbox.MyMessageBox.Show(IntPtr.Zero, text, gameName, MessageBoxOptions.OkOnly | MessageBoxOptions.SystemModal | MessageBoxOptions.IconExclamation | MessageBoxOptions.SetForeground);
        }

        public static void ReportNotDX11GPUCrash(string gameName, string logfile, string minimumRequirementsPage)
        {
            string text = String.Format(APP_ERROR_MESSAGE_NOT_DX11_GPU, logfile, gameName, minimumRequirementsPage);
            Sandbox.MyMessageBox.Show(IntPtr.Zero, text, gameName, MessageBoxOptions.OkOnly | MessageBoxOptions.SystemModal | MessageBoxOptions.IconExclamation | MessageBoxOptions.SetForeground);
        }

        public static void ReportGpuUnderMinimumCrash(string gameName, string logfile, string minimumRequirementsPage)
        {
            string text = String.Format(APP_ERROR_MESSAGE_LOW_GPU, logfile, gameName, minimumRequirementsPage);
            Sandbox.MyMessageBox.Show(IntPtr.Zero, text, gameName, MessageBoxOptions.OkOnly | MessageBoxOptions.SystemModal | MessageBoxOptions.IconExclamation | MessageBoxOptions.SetForeground);
        }

        public static void ReportOutOfMemory(string gameName, string logfile, string minimumRequirementsPage)
        {
            string text = String.Format(APP_ERROR_OUT_OF_MEMORY, logfile, gameName, minimumRequirementsPage);
            Sandbox.MyMessageBox.Show(IntPtr.Zero, text, gameName, MessageBoxOptions.OkOnly | MessageBoxOptions.SystemModal | MessageBoxOptions.IconExclamation | MessageBoxOptions.SetForeground);
        }

        public static void ReportOutOfVideoMemory(string gameName, string logfile, string minimumRequirementsPage)
        {
            string text = String.Format(APP_ERROR_OUT_OF_VIDEO_MEMORY, logfile, gameName, minimumRequirementsPage);
            Sandbox.MyMessageBox.Show(IntPtr.Zero, text, gameName, MessageBoxOptions.OkOnly | MessageBoxOptions.SystemModal | MessageBoxOptions.IconExclamation | MessageBoxOptions.SetForeground);
        }

        static void MessageBox(string caption, string text)
        {
            Sandbox.MyMessageBox.Show(IntPtr.Zero, text, caption, MessageBoxOptions.SetForeground);
        }

        static bool DisplayCommonError(string logContent)
        {
            foreach (var i in MyErrorTexts.Infos)
            {
                if (logContent.Contains(i.Match))
                {
                    MessageBox(i.Caption, i.Message);
                    return true;
                }
            }
            return false;
        }

        static bool LoadAndDisplayCommonError(string logName)
        {
            try
            {
                if (logName != null && File.Exists(logName))
                {
                    using (var file = File.Open(logName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(file))
                    {
                        return DisplayCommonError(reader.ReadToEnd() ?? String.Empty);
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public static void ReportNotInteractive(string logName, string id)
        {
            if (logName == null)
                return;

            string log;
            HttpStatusCode code;
            SendReport(logName, id, out log, out code);
        }

        public static void Report(string logName, string gameName, string id, string errorMessage)
        {
            if (LoadAndDisplayCommonError(logName))
                return;

            if (!AllowSendDialog(gameName, logName, errorMessage) || logName == null)
                return;

            string log;
            HttpStatusCode code;
            SendReport(logName, id, out log, out code);

            if (log == String.Empty || code == HttpStatusCode.OK)
            {
                MessageBox(gameName, APP_LOG_REPORT_THANK_YOU);
            }
            else
            {
                MessageBox(
                    String.Format(APP_ERROR_CAPTION, gameName),
                    String.Format(APP_LOG_REPORT_FAILED, gameName, logName, SUPPORT_EMAIL));
            }
        }

        public static void ReportAppAlreadyRunning(string gameName)
        {
#if !XB1
            System.Windows.Forms.MessageBox.Show(
                String.Format(APP_ALREADY_RUNNING, gameName),
                String.Format(MESSAGE_BOX_CAPTION, gameName));
#else
            System.Diagnostics.Debug.Assert(false, "Report support on XB1!");
#endif
        }

        private static void SendReport(string logName, string id, out string log, out HttpStatusCode code)
        {
            log = null;
            string additionalInfo = String.Empty;
            code = HttpStatusCode.MethodNotAllowed;

            try
            {
                if (logName != null && File.Exists(logName))
                {
                    using (var file = File.Open(logName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(file))
                    {
                        log = reader.ReadToEnd() ?? String.Empty;
                    }
                }

#if XB1
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
                try
                {
                    foreach (var renderLog in Directory.GetFiles(Path.GetDirectoryName(logName), "VRageRender*.log", SearchOption.TopDirectoryOnly))
                    {
                        if (renderLog != null && File.Exists(renderLog))
                        {
                            using (var file = File.Open(renderLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var reader = new StreamReader(file))
                            {
                                additionalInfo += reader.ReadToEnd() ?? String.Empty;
                            }
                        }
                    }
                }
                catch { }
#endif // !XB1

                if (!String.IsNullOrEmpty(log))
                {
                    string uri = "http://www.minerwars.com/SubmitLog.aspx?id=" + id;
                    //string uri = "http://localhost/SubmitLog.aspx";
                    HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(uri);

                    httpWReq.Method = "POST";
                    httpWReq.ContentType = "application/octet-stream";

                    using (Stream stream = httpWReq.GetRequestStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(log);
                        writer.Write(additionalInfo);
                    }

                    using (var response = (HttpWebResponse)httpWReq.GetResponse())
                    {
                        code = response.StatusCode;
                    }
                }
            }
            catch
            {
            }
        }

        public static string SUPPORT_EMAIL = "support@keenswh.com";

        public static string MESSAGE_BOX_CAPTION = "{0} Launcher";
        public static string APP_ALREADY_RUNNING = "Sorry, but {0} is already running on your computer. Only one instance allowed.";
        public static string APP_ERROR_CAPTION = "{0} - Application Error";

        public static string APP_LOG_REPORT_FAILED = (
            "{0} log upload failed\n" +
            "{1}\n\n" +
            "If you want to help us make {0} a better game, please send the application log to {2}\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_LOG_REPORT_THANK_YOU = (
            "We make sure the issue will be fixed as soon as possible!\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_ERROR_MESSAGE = (
            "{0} - application error occured. For more information please see application log at {1}\n\n" +
            "If you want to help us make {0} a better game, you can send us the log file. " +
            "No personal data or any sensitive information will be submitted.\n\n" +
            "Do you want to submit this log to developers?\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_ERROR_MESSAGE_DX11_NOT_AVAILABLE = (
            "{1} - application error occured. For more information please see application log at {0}\n\n" +
            "This problem may be caused by your graphics card, because it does not meet minimum requirements. " +
            "DirectX 11 GPU is required. " +
            "Please see minimum requirements at {2}\n\n" +
            "Do you want to submit your configuration to developers?\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_ERROR_MESSAGE_LOW_GPU = (
            "{1} - application error occured. For more information please see application log at {0}\n\n" +
            "This problem may be caused by your graphics card, because it does not meet minimum requirements. " +
            "Please see minimum requirements at {2}\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_ERROR_MESSAGE_NOT_DX11_GPU = (
            "{1} - application error occured. For more information please see application log at {0}\n\n" +
            "This problem may be caused by your graphics card, because it does not meet minimum requirements. " +
            "DirectX 11 GPU is required. " +
            "Please see minimum requirements at {2}\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_ERROR_MESSAGE_DRIVER_NOT_INSTALLED = (
            "{1} - application error occured. For more information please see application log at {0}\n\n" +
            "It seems that your graphics card driver is not installed or your graphics card does not meet minimum requirements. " +
            "Please install driver and see minimum requirements at {2}\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_WARNING_MESSAGE_UNSUPPORTED_GPU = (
            "{1} - Warning!\n\n" +
            "It seems that your graphics card is currently unsupported because it does not meet minimum requirements. For more information please see application log at {0}\n" +
            "Please see minimum requirements at {2}\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_ERROR_OUT_OF_MEMORY = (
            "{1} - application error occured. For more information please see application log at {0}\n\n" +
            "This problem is caused by limited memory on your system. In case you're still using 32-bit operating system, upgrade is strongly recommended.\n\n" +
            "In case you're using 64-bit operating system, please close other applications (especially internet browser) and try again or install additional system memory.\n\n" +
            "Please see minimum requirements at {2}\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);

        public static string APP_ERROR_OUT_OF_VIDEO_MEMORY = (
            "{1} - application error occured. For more information please see application log at {0}\n\n" +
            "This problem is caused by limited video memory on your system. Please make sure you Graphics card meets minimum requirements: at least 512 MB of video memory" +
            "If you have enabled MODs or playing on server with MODs, this may be the cause. Some mods feature high quality textures which consumes great amount of video memory.\n\n" +
            "Please see minimum requirements at {2}\n\n" +
            "Thank You!\n" +
            "Keen Software House"
            ).Replace("\n", MyUtils.C_CRLF);


    }
}
