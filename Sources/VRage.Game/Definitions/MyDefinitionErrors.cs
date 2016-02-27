using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Utils;
using VRageMath;

namespace VRage.Game
{
    public enum TErrorSeverity
    {
        Notice = 0, //Definitions overriding
        Warning = 1, // Missing file extension
        Error = 2,   // Missing model
        Critical =3 ,// Broken definition, other things which cause mod not to load 
    }

    public static class MyDefinitionErrors
    {
        public class Error
        {
            public string ModName;
            public string ErrorFile;
            public string Message;
            public TErrorSeverity Severity;

            private static Color[] severityColors = { Color.Gray, Color.Gray, Color.White, new Color(1.0f, 0.25f, 0.1f) };

            private static string[] severityName = { "notice", "warning", "error", "critical error" };
            private static string[] severityNamePlural = { "notices", "warnings", "errors", "critical errors" };

            public string ErrorId
            {
                get { return ModName == null ? "definition_" : "mod_"; }
            }

            public string ErrorSeverity
            {
                get 
                {
                    string result = ErrorId;
                    switch (Severity)
                    {
                        case TErrorSeverity.Critical:
                            result = (result + "critical_error").ToUpperInvariant();
                            break;

                        case TErrorSeverity.Error:
                            result = (result + "error").ToUpperInvariant();
                            break;

                        case TErrorSeverity.Warning:
                            result = (result + "warning");
                            break;
                        case TErrorSeverity.Notice:
                            result = (result + "notice");
                            break;
                    }
                    return result;
                }
            }

            public override string ToString()
            {
                return String.Format("{0}: {1}, in file: {2}\n{3}", ErrorSeverity, ModName ?? String.Empty, ErrorFile, Message);
            }

            public static Color GetSeverityColor(TErrorSeverity severity)
            {
                try
                {
                    return severityColors[(int)severity];
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(String.Format("Error type does not have color assigned: message: {0}, stack:{1}", e.Message, e.StackTrace));
                    return Color.White;
                }
            }

            public static string GetSeverityName(TErrorSeverity severity, bool plural)
            {
                try
                {
                    if (plural)
                    {
                        return severityNamePlural[(int)severity];
                    }
                    {
                        return severityName[(int)severity];
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(String.Format("Error type does not have name assigned: message: {0}, stack:{1}", e.Message, e.StackTrace));
                    return plural ? "Errors" : "Error";
                }
            }

            public Color GetSeverityColor()
            {
                return GetSeverityColor(Severity);
            }
        }

        public class ErrorComparer : IComparer<Error>
        {
            public int Compare(Error x, Error y)
            {
                // Desc by severity
                return y.Severity - x.Severity;
            }
        }

        static readonly List<Error> m_errors = new List<Error>();
        static readonly ErrorComparer m_comparer = new ErrorComparer();

        public static bool ShouldShowModErrors { get; set; }

        public static void Clear()
        {
            m_errors.Clear();
        }

        public static void Add(MyModContext context, string message, TErrorSeverity severity, bool writeToLog = true)
        {
            var e = new Error()
            {
                ModName = context.ModName,
                ErrorFile = context.CurrentFile,
                Message = message,
                Severity = severity,
            };

            m_errors.Add(e);

            if (context.ModName == null)
                Debug.Fail(e.ToString());

            if (writeToLog)
                WriteError(e);

            if (severity == TErrorSeverity.Critical)
                ShouldShowModErrors = true;
        }   

        public static ListReader<Error> GetErrors()
        {
            m_errors.Sort(m_comparer);
            return new ListReader<Error>(m_errors);
        }

        public static void WriteError(Error e)
        {
            MyLog.Default.WriteLine(String.Format("{0}: {1}", e.ErrorSeverity, e.ModName ?? String.Empty));
            MyLog.Default.WriteLine("  in file: " + e.ErrorFile ?? String.Empty);
            MyLog.Default.WriteLine("  " + e.Message);
        }
    }
}