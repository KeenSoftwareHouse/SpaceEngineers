using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;

using Sandbox.Engine.Platform.VideoMode;
using System.Threading;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using VRage.Utils;
using System.Globalization;

using ParallelTasks;
using Sandbox.Definitions;
using System.Diagnostics;
using VRage.Trace;
#if !XB1
using LitJson;
#endif
using VRage;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using VRage.Game;

namespace Sandbox.Engine.Networking
{
    public struct MyStartSessionStatistics
    {
        public bool VerticalSync;
        public bool Fullscreen;
        public int VideoWidth;
        public int VideoHeight;

        public MyObjectBuilder_SessionSettings Settings;
    }

    public struct MyEndSessionStatistics
    {
        public int AverageFPS, MinFPS, MaxFPS;
        public int TotalPlaytimeInSeconds;
        public int FootTimeInSeconds, SmallShipTimeInSeconds, BigShipTimeInSeconds, JetpackTimeInSeconds;
        public Dictionary<string, MyFixedPoint> AmountMined;
        public float NegativeIntegrityTotal;
        public float PositiveIntegrityTotal;
    }

    public static class MyAnalyticsTracker
    {
        private static bool m_enabled = true;
        private static string[] m_oreTypes;
#if !XB1
        private static readonly CommonRequiredData m_requiredData;
#endif
        private static bool AnalyticsEnabled = (MyFinalBuildConstants.IS_OFFICIAL || MyFakes.ENABLE_INFINARIO) && !MyCompilationSymbols.PerformanceOrMemoryProfiling;

#if XB1
        public static void SendGameStart()
        {
            
        }
        public static void SendGameEnd(string method, int totalTimeInSeconds)
        {

        }
        public static void SendSessionStart(MyStartSessionStatistics sessionStatistics)
        {

        }
        public static void SendSessionEnd(MyEndSessionStatistics sessionStatistics)
        {

        }
        public static void ReportError(SeverityEnum severityEnum, Exception ex, bool async = true)
        {

        }
        public static void ReportError(SeverityEnum severityEnum, string messageText, bool async = true)
        {

        }


        /// <summary>
        /// Severity levels corresponding with Game analytics.
        /// </summary>
        public enum SeverityEnum
        {
            Critical,
            Error,
            Warning,
            Info,
            Debug
        }
#else
        static MyAnalyticsTracker()
        {
            var hashKey = new byte[64]; // SHA key, not used for any security, just hashing of user id
            string userId;
            using (HMACSHA1 shaCoder = new HMACSHA1(hashKey))
            {
                userId = BitConverter.ToString(shaCoder.ComputeHash(BitConverter.GetBytes(Sync.MyId)));
            }

            m_requiredData = new CommonRequiredData()
            {
                user_id = userId,
                session_id = Guid.NewGuid().ToString(),
                build = string.Format("{0}_{1}", MyFinalBuildConstants.APP_VERSION_STRING, MyFinalBuildConstants.IS_OFFICIAL ? BranchName : "VS"),
            };
        }

        private static bool IsPublic
        {
            get { return MyFinalBuildConstants.IS_OFFICIAL && MySteam.BranchName == null; }
        }

        private static bool IsDev
        {
            get { return MyFinalBuildConstants.IS_OFFICIAL && (MySteam.BranchName == "development" || MySteam.BranchName == "dev"); }
        }

        private static bool IsPirate
        {
            get { return MyFinalBuildConstants.IS_OFFICIAL && MySandboxGame.IsPirated; }
        }

        private static string BranchName
        {
            get
            {
                return String.IsNullOrEmpty(MySteam.BranchName) ? "def" : MySteam.BranchName;
            }
        }

        public static void SendGameStart()
        {
            if (AnalyticsEnabled)
                Parallel.Start(() => { SendGameStartInternal(); });
        }

        public static void SendGameEnd(string method, int totalTimeInSeconds)
        {
            if (AnalyticsEnabled)
                Parallel.Start(() => { SendGameEndInternal(method, totalTimeInSeconds); });
        }

        public static void SendSessionStart(MyStartSessionStatistics sessionStatistics)
        {
            if (AnalyticsEnabled)
                Parallel.Start(() => { SendSessionStartInternal(sessionStatistics); });
        }

        public static void SendSessionEnd(MyEndSessionStatistics sessionStatistics)
        {
            if (AnalyticsEnabled)
                Parallel.Start(() => { SendSessionEndInternal(sessionStatistics); });
        }

        public static void ReportError(SeverityEnum severityEnum, Exception ex, bool async = true)
        {
            if (AnalyticsEnabled)
            {
                var data = new ErrorEventData()
                {
                    severity = severityEnum,
                    message = (ex == null) ? "No exception specified." : ex.ToString(),
                };
                if (async)
                    Parallel.Start(() => { ReportErrorInternal(data); });
                else
                    ReportErrorInternal(data);
            }
        }

        public static void ReportError(SeverityEnum severityEnum, string messageText, bool async = true)
        {
            if (AnalyticsEnabled)
            {
                var data = new ErrorEventData()
                {
                    severity = severityEnum,
                    message = string.IsNullOrWhiteSpace(messageText) ? "No text specified." : messageText,
                };
                if (async)
                    Parallel.Start(() => { ReportErrorInternal(data); });
                else
                    ReportErrorInternal(data);
            }
        }

        private static void SendGameStartInternal()
        {
            ReportDesignInternal("Game:Start:Version", value: (float)MyBuildNumbers.GetBuildNumberWithoutMajor(MyFinalBuildConstants.APP_VERSION));
            ReportDesignInternal("Game:Start:Branch:" + BranchName);
            ReportDesignInternal("Game:Start:OS:" + Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor);

            UserEventData user;
            user.platform = string.Format("{0} ({1})", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "x64" : "x86");
            user.os_major = Environment.OSVersion.Version.Major.ToString();
            user.os_minor = Environment.OSVersion.Version.Minor.ToString();
            user.device = "PC";
            ReportUserInternal(user);
        }

        private static void SendGameEndInternal(string method, int totalTimeInSeconds)
        {
            ReportDesignInternal("Game:Quit:" + method);
            ReportDesignInternal("Game:Total time", value: totalTimeInSeconds);
        }

        private static void SendSessionStartInternal(MyStartSessionStatistics sessionStatistics)
        {
            ReportDesignInternal("Session:Video Settings:Vertical Sync", value: sessionStatistics.VerticalSync ? 1 : 0);
            ReportDesignInternal("Session:Video Settings:Fullscreen", value: sessionStatistics.Fullscreen ? 1 : 0);

            if (sessionStatistics.VideoHeight != 0)
            {
                ReportDesignInternal("Session:Video Settings:Aspect Ratio", value: (float)sessionStatistics.VideoWidth / (float)sessionStatistics.VideoHeight);
            }

            ReportDesignInternal("Session:World Settings:Auto Healing", value: sessionStatistics.Settings.AutoHealing ? 1 : 0);
            ReportDesignInternal("Session:World Settings:Auto Save In Minutes", value: sessionStatistics.Settings.AutoSaveInMinutes);
        }

        private static void SendSessionEndInternal(MyEndSessionStatistics sessionStatistics)
        {
            if (m_oreTypes == null)
                MyDefinitionManager.Static.GetOreTypeNames(out m_oreTypes);

            ReportDesignInternal("Session:Play Time:Total", value: sessionStatistics.TotalPlaytimeInSeconds);
            ReportDesignInternal("Session:FPS:Minimum FPS", value: sessionStatistics.MinFPS);
            ReportDesignInternal("Session:FPS:Maximum FPS", value: sessionStatistics.MaxFPS);
            ReportDesignInternal("Session:FPS:Average FPS", value: sessionStatistics.AverageFPS);
            ReportDesignInternal("Session:Play Time:Jetpack Time", value: sessionStatistics.JetpackTimeInSeconds);
            ReportDesignInternal("Session:Play Time:Small Ship Time", value: sessionStatistics.SmallShipTimeInSeconds);
            ReportDesignInternal("Session:Play Time:Big Ship Time", value: sessionStatistics.BigShipTimeInSeconds);
            ReportDesignInternal("Session:Play Time:Foot Time", value: sessionStatistics.FootTimeInSeconds);
            ReportDesignInternal("Session:Resources:Total Amount Mined", value: (float)sessionStatistics.AmountMined.Values.Sum(s => (float)s));

            for (int i = 0; i < m_oreTypes.Length; i++)
            {
                MyFixedPoint mined = 0;
                sessionStatistics.AmountMined.TryGetValue(m_oreTypes[i], out mined);
                ReportDesignInternal("Session:Resources:Amount " + m_oreTypes[i] + " mined", value: (float)mined);
            }
            ReportDesignInternal("Session:Resources:Positive Integrity Total", value: sessionStatistics.PositiveIntegrityTotal);
            ReportDesignInternal("Session:Resources:Negative Integrity Total", value: sessionStatistics.NegativeIntegrityTotal);
        }

        #region Internal reporting

        private static void ReportDesignInternal(
            string eventId,
            float? value = null,
            LocationData? location = null,
            bool logFailure = true)
        {
            if (!m_enabled)
                return;

            Debug.Assert(!string.IsNullOrWhiteSpace(eventId));
            var json = CreateWriter();
            json.AppendProperty("event_id", eventId);
            if (location.HasValue)
                location.Value.Write(json);
            if (value.HasValue)
                json.AppendProperty("value", value.Value);
            Report("design", json, logFailure);
        }

        private static void ReportUserInternal(
            UserEventData user,
            bool logFailure = true)
        {
            if (!m_enabled)
                return;

            var json = CreateWriter();
            user.Write(json);
            Report("user", json, logFailure);
        }

        private static void ReportErrorInternal(
            ErrorEventData error,
            LocationData? location = null,
            bool logFailure = false)
        {
            if (!m_enabled)
                return;

            var json = CreateWriter();
            error.Write(json);
            if (location.HasValue)
                location.Value.Write(json);
            Report("error", json, logFailure);
        }

        private static void Report(string category, JsonWriter json, bool logFailure)
        {
            json.WriteObjectEnd();
            try
            {
                string gameKey, secretKey;
                GetKeys(out gameKey, out secretKey);
                string jsonMessage = json.ToString();
                string url = GetApiUrl(gameKey, category);
                string auth = GetAuthorization(jsonMessage, secretKey);
                string result = Post(url, jsonMessage, auth);
                MyTrace.Send(TraceWindow.Analytics, jsonMessage);
            }
            catch (Exception ex)
            {
                m_enabled = false;

                if (logFailure)
                {
                    // Do not write it to log as classic exception (it would also make false reports for error reporter)
                    MySandboxGame.Log.WriteLine("Sending analytics failed: " + ex.Message);
                }
            }
        }

        #endregion

        #region Helpers

        private static string GetSeverityString(SeverityEnum severity)
        {
            switch (severity)
            {
                case SeverityEnum.Critical: return "critical";
                case SeverityEnum.Debug: return "debug";
                case SeverityEnum.Error: return "error";
                case SeverityEnum.Info: return "info";
                case SeverityEnum.Warning: return "warning";

                default:
                    Debug.Fail("Invalid branch.");
                    return "critical";
            }
        }

        private static JsonWriter CreateWriter()
        {
            var json = new JsonWriter();
            json.PrettyPrint = false;
            json.WriteObjectStart();
            m_requiredData.Write(json);
            return json;
        }

        private static JsonWriter AppendProperty(this JsonWriter self, string propertyName, string propertyValue)
        {
            self.WritePropertyName(propertyName);
            self.Write(propertyValue);
            return self;
        }

        private static JsonWriter AppendProperty(this JsonWriter self, string propertyName, float propertyValue)
        {
            self.WritePropertyName(propertyName);
            self.Write(propertyValue);
            return self;
        }

        private static string GetApiUrl(string gameKey, string category)
        {
            const int API_VERSION = 1;
            return string.Format("http://api.gameanalytics.com/{0}/{1}/{2}", API_VERSION, gameKey, category);
        }

        private static void GetKeys(out string gameKey, out string secretKey)
        {
            if (IsPublic)
            {
                // Public branch on Steam (IsOfficial is tested too)
                gameKey = MyPerGameSettings.GA_Public_GameKey;
                secretKey = MyPerGameSettings.GA_Public_SecretKey;
            }
            else if (IsDev)
            {
                // Development branch on Steam (IsOfficial is tested too)
                gameKey = MyPerGameSettings.GA_Dev_GameKey;
                secretKey = MyPerGameSettings.GA_Dev_SecretKey;
            }
            else if (IsPirate)
            {
                // Possibly pirate copies (initial tests check whether path is in steam)
                gameKey = MyPerGameSettings.GA_Pirate_GameKey;
                secretKey = MyPerGameSettings.GA_Pirate_SecretKey;
            }
            else
            {
                // VS builds, Automatic
                gameKey = MyPerGameSettings.GA_Other_GameKey;
                secretKey = MyPerGameSettings.GA_Other_SecretKey;
            }
        }

        private static string GetAuthorization(string jsonMessage, string secretKey)
        {
            byte[] authData = Encoding.Default.GetBytes(jsonMessage + secretKey);
            // Transforms as hexa
            return VRage.Security.Md5.ComputeHash(authData).ToLowerString();
        }

        private static string Post(string uri, string body, string authorization = null)
        {
            var request = WebRequest.Create(uri);
            if (authorization != null)
                request.Headers.Add("Authorization", authorization);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            using (var reqStream = request.GetRequestStream())
            {
                reqStream.Write(bodyBytes, 0, bodyBytes.Length);
            }
            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                    return null;

                using (var reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion

        #region Nested types

        /// <summary>
        /// Severity levels corresponding with Game analytics.
        /// </summary>
        public enum SeverityEnum
        {
            Critical,
            Error,
            Warning,
            Info,
            Debug
        }

        struct CommonRequiredData
        {
            public string user_id;
            public string build;
            public string session_id;

            internal void Write(JsonWriter json)
            {
                Debug.Assert(user_id != null && session_id != null && build != null);
                json.AppendProperty("user_id", user_id)
                    .AppendProperty("build", build)
                    .AppendProperty("session_id", session_id);
            }
        }

        struct LocationData
        {
            public string area;
            public float? x, y, z;

            internal void Write(JsonWriter json)
            {
                if (!string.IsNullOrWhiteSpace(area)) json.AppendProperty("area", area);
                if (x.HasValue) json.AppendProperty("x", x.Value);
                if (y.HasValue) json.AppendProperty("y", y.Value);
                if (z.HasValue) json.AppendProperty("z", z.Value);
            }
        }

        struct UserEventData
        {
            //public CommonRequiredData Required;
            public string platform;
            public string device;
            public string os_major;
            public string os_minor;

            // Add other fields if needed.

            internal void Write(JsonWriter json)
            {
                if (!string.IsNullOrWhiteSpace(platform)) json.AppendProperty("platform", platform);
                if (!string.IsNullOrWhiteSpace(device)) json.AppendProperty("device", device);
                if (!string.IsNullOrWhiteSpace(os_major)) json.AppendProperty("os_major", os_major);
                if (!string.IsNullOrWhiteSpace(os_minor)) json.AppendProperty("os_minor", os_minor);
            }
        }

        struct DesignEventData
        {
            //public CommonRequiredData Required;
            //public string event_id;
            //public LocationData Location;
            public float? value;
        }

        struct ErrorEventData
        {
            //public CommonRequiredData Required;
            //public LocationData Location;
            public string message;
            public SeverityEnum severity;

            internal void Write(JsonWriter json)
            {
                Debug.Assert(!string.IsNullOrWhiteSpace(message));
                json.AppendProperty("severity", GetSeverityString(severity))
                    .AppendProperty("message", message);
            }
		}
		#endregion
#endif //!XB1

	}
}
